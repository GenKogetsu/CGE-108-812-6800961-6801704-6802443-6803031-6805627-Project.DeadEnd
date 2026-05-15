using System.Linq;
using UnityEngine;

namespace MonsterAI
{
    // ─────────────────────────────────────────────────────────────────
    // PatrolState — เดินไป-มาแบบ random ใน Range จาก pivot
    // ─────────────────────────────────────────────────────────────────
    public class PatrolState : IMonsterState
    {
        readonly MonsterController _ctx;

        Vector2 _targetPoint;
        float   _waitTimer;
        bool    _waiting;

        public PatrolState(MonsterController ctx) => _ctx = ctx;

        public void OnEnter()
        {
            _waiting   = false;
            _waitTimer = 0f;
            PickNewTarget();
        }

        public void OnUpdate(float dt)
        {
            // เห็น player → Chase
            if (_ctx.CanSeePlayer())
            {
                _ctx.ChangeState(_ctx.GetChaseState());
                return;
            }

            if (_waiting)
            {
                _waitTimer -= dt;
                if (_waitTimer <= 0f)
                {
                    _waiting = false;
                    PickNewTarget();
                }
                return;
            }

            // เดินไปยัง target
            _ctx.MoveToward(_targetPoint, _ctx.PatrolCfg.Speed);

            // ถึงแล้ว → รอ
            if (Vector2.Distance(_ctx.Rb2.position, _targetPoint) < 0.15f)
            {
                _ctx.StopMove();
                _waiting   = true;
                _waitTimer = _ctx.PatrolCfg.WaitTime;
            }
        }

        public void OnExit()
        {
            _ctx.StopMove();
        }

        void PickNewTarget()
        {
            PatrolConfig cfg = _ctx.PatrolCfg;
            Vector2 pivot    = _ctx.PivotPoint;
            _targetPoint = new Vector2(
                pivot.x + Random.Range(-cfg.Range.x, cfg.Range.x),
                pivot.y + Random.Range(-cfg.Range.y, cfg.Range.y));
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // ChaseState — ไล่ตาม player จนถึงระยะโจมตี
    // ─────────────────────────────────────────────────────────────────
    public class ChaseState : IMonsterState
    {
        readonly MonsterController _ctx;

        public ChaseState(MonsterController ctx) => _ctx = ctx;

        public void OnEnter() { }

        public void OnUpdate(float dt)
        {
            if (_ctx.PlayerTransform == null) { _ctx.ChangeState(_ctx.GetPatrolState()); return; }

            Vector2 playerPos = _ctx.PlayerTransform.position;

            // ไม่เห็น player → Search
            if (!_ctx.CanSeePlayer())
            {
                _ctx.LastKnownPlayerPos = playerPos;
                _ctx.GetSearchState().Setup(_ctx.LastKnownPlayerPos);
                _ctx.ChangeState(_ctx.GetSearchState());
                return;
            }

            _ctx.LastKnownPlayerPos = playerPos;

            // ตรวจว่ามีท่าไหนพร้อม + อยู่ในระยะ
            AttackStrategy best = SelectBestAttack(playerPos);
            if (best != null)
            {
                _ctx.GetAttackState().Setup(best);
                _ctx.ChangeState(_ctx.GetAttackState());
                return;
            }

            // ยังไม่ได้ระยะ → เดินเข้าหา
            _ctx.MoveToward(playerPos, _ctx.ChaseSpeed);
        }

        public void OnExit()
        {
            _ctx.StopMove();
        }

        AttackStrategy SelectBestAttack(Vector2 playerPos)
        {
            // เลือกท่าที่ ready + player อยู่ในระยะ แล้วเอาอันที่ MaxCooldown สูงสุด
            return _ctx.Strategies
                       .Where(s => s.IsReady &&
                                   s.Range.Contains(_ctx.Rb2.position, playerPos, _ctx.FacingDir))
                       .OrderByDescending(s => s.MaxCooldown)
                       .FirstOrDefault();
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // SearchState — เดินไปยัง lastKnownPos แล้วมองซ้าย-ขวา 3 รอบ
    // ─────────────────────────────────────────────────────────────────
    public class SearchState : IMonsterState
    {
        const int   LOOK_ROUNDS   = 3;
        const float LOOK_DURATION = 0.33f;

        readonly MonsterController _ctx;

        Vector2 _lastKnownPos;
        bool    _arrived;
        int     _lookRound;
        int     _lookSide;       // 1 or -1
        float   _lookTimer;

        public SearchState(MonsterController ctx) => _ctx = ctx;

        /// <summary>ต้องเรียกก่อน ChangeState เข้า SearchState</summary>
        public void Setup(Vector2 lastKnown) => _lastKnownPos = lastKnown;

        public void OnEnter()
        {
            _arrived   = false;
            _lookRound = 0;
            _lookSide  = 1;
            _lookTimer = LOOK_DURATION;
        }

        public void OnUpdate(float dt)
        {
            // เห็น player ระหว่างทาง → Chase
            if (_ctx.CanSeePlayer())
            {
                _ctx.ChangeState(_ctx.GetChaseState());
                return;
            }

            if (!_arrived)
            {
                // เดินไปยัง lastKnownPos
                _ctx.MoveToward(_lastKnownPos, _ctx.SearchSpeed);

                if (Vector2.Distance(_ctx.Rb2.position, _lastKnownPos) < 0.2f)
                {
                    _ctx.StopMove();
                    _arrived = true;
                    _ctx.SetFacing(_lookSide);
                }
                return;
            }

            // มองซ้าย-ขวา
            _lookTimer -= dt;
            if (_lookTimer <= 0f)
            {
                _lookSide  = -_lookSide;          // สลับทิศ
                _lookRound++;
                _lookTimer = LOOK_DURATION;
                _ctx.SetFacing(_lookSide);

                if (_lookRound >= LOOK_ROUNDS * 2)    // ครบทุก half-turn
                {
                    // ไม่เจอ → กลับ Patrol
                    _ctx.ChangeState(_ctx.GetPatrolState());
                }
            }
        }

        public void OnExit() { }
    }

    // ─────────────────────────────────────────────────────────────────
    // AttackState — เล่น animation โจมตี (root motion)
    //   รอ Animation Event "OnAttackFinished" หรือ timeout → กลับ Chase
    // ─────────────────────────────────────────────────────────────────
    public class AttackState : IMonsterState
    {
        readonly MonsterController _ctx;

        AttackStrategy _strategy;
        Vector2        _attackStartPos;
        float          _elapsed;
        float          _timeout;   // fallback ถ้าไม่มี Animation Event

        public AttackState(MonsterController ctx) => _ctx = ctx;

        /// <summary>ต้องเรียกก่อน ChangeState เข้า AttackState</summary>
        public void Setup(AttackStrategy strategy) => _strategy = strategy;

        public void OnEnter()
        {
            _attackStartPos = _ctx.Rb2.position;
            _elapsed        = 0f;

            // timeout = MaxCooldown + 0.5s buffer
            // ถ้าไม่มี strategy ให้ fallback 2 วินาที
            _timeout = _strategy != null ? _strategy.MaxCooldown + 0.5f : 2f;

            _ctx.ResetAnimatorSpeed();
            _strategy?.Execute(_attackStartPos, _ctx.FacingDir);
        }

        public void OnUpdate(float dt)
        {
            _elapsed += dt;

            // ออกเมื่อ: Animation Event ยิง หรือ timeout
            bool done = _ctx.ConsumeAttackFinished() || _elapsed >= _timeout;
            if (!done) return;

            ExitAttack();
        }

        public void OnExit()
        {
            // เริ่ม CD หลังออกจาก AttackState → CD จะไม่หมดระหว่างเล่น animation
            _strategy?.StartCooldown();
            _ctx.Hitbox?.Disable();
        }

        void ExitAttack()
        {
            if (_ctx.CanSeePlayer())
                _ctx.ChangeState(_ctx.GetChaseState());
            else
            {
                _ctx.LastKnownPlayerPos = _ctx.PlayerTransform != null
                    ? (Vector2)_ctx.PlayerTransform.position
                    : _ctx.LastKnownPlayerPos;
                _ctx.GetSearchState().Setup(_ctx.LastKnownPlayerPos);
                _ctx.ChangeState(_ctx.GetSearchState());
            }
        }
    }
}
