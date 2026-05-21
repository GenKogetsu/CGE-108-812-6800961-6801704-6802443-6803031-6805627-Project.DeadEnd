using System.Collections.Generic;
using Kogetsu.Library.Core;
using UnityEngine;

namespace MonsterAI
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
    public class MonsterController : MonoBehaviour
    {
        [Header("Vision")]
        public VisionConfig VisionCfg = new VisionConfig();

        [Header("Patrol")]
        public PatrolConfig PatrolCfg = new PatrolConfig();

        [Header("Chase")]
        public float ChaseSpeed = 3.5f;

        [Header("Search")]
        public float SearchSpeed = 2.5f;

        [Header("Animation")]
        [Tooltip("ชื่อ Bool parameter ใน Animator สำหรับ animation วิ่ง (ว่าง = ไม่ใช้)")]
        [SerializeField] private string _isMovingParam = "IsMoving";

        [Header("Attack Strategies")]
        public AttackStrategyData[] AttackData = new AttackStrategyData[1];

        [Header("HP")]
        [SerializeField] private float _maxHp = 100f;

        [Header("Contact Damage")]
        [Tooltip("Damage ที่ทำกับ Player เมื่อชน")]
        [SerializeField] private float _contactDamage  = 10f;
        [Tooltip("ป้องกัน damage ถี่เกินเมื่อ overlap")]
        [SerializeField] private float _damageCooldown = 0.8f;
        [Tooltip("Trigger ชื่อนี้จะ Set ใน Animator Player เมื่อโดนชน")]
        [SerializeField] private string _playerHitTrigger = "Hit";

        [Header("Facing")]
        [Tooltip("เปิดถ้าโมเดล default หันซ้าย → invert ทิศ flip ของ localScale.x")]
        [SerializeField] private bool _invertFacing = false;
        [Tooltip("เปิดถ้าการหันหน้าตามทิศเดินยังไปคนละทาง → กลับทิศที่ใช้ตัดสิน facing จาก movement")]
        [SerializeField] private bool _invertMoveFacing = false;

        [Header("Hit Flash")]
        [Tooltip("Root ที่มี SpriteRenderer ทั้งหมด (รวม inactive)")]
        [SerializeField] private Transform _hitFlashRoot;
        [SerializeField] private Color     _hitStartColor = Color.white;
        [SerializeField] private Color     _hitEndColor   = Color.red;
        [SerializeField] private float     _hitLerpSpeed  = 10f;
        [SerializeField] private int       _hitLerpCount  = 2;

        [Header("References")]
        public Transform    PlayerTransform;
        public AttackHitbox Hitbox;

        public Rigidbody2D Rb2            { get; private set; }
        public Animator    Animator       { get; private set; }
        public int         FacingDir      { get; private set; } = 1;
        public Vector2     PivotPoint     { get; private set; }
        public Vector2     LastKnownPlayerPos { get; set; }

        public IReadOnlyList<AttackStrategy> Strategies => _strategies;

        MonsterStateMachine  _sm;
        List<AttackStrategy> _strategies;
        float                _cachedInitAnimSpeed;
        bool                 _attackFinished;
        float                _currentHp;
        bool                 _isDead;
        float                _lastDamageTime = -99f;
        SpriteRenderer[]     _cachedRenderers;
        Coroutine            _flashCoroutine;

        PatrolState  _patrol;
        ChaseState   _chase;
        SearchState  _search;
        AttackState  _attackState;

        void Awake()
        {
            Rb2      = GetComponent<Rigidbody2D>();
            Animator = GetComponent<Animator>();

            _cachedInitAnimSpeed = Animator.speed;

            Transform flashRoot = _hitFlashRoot != null ? _hitFlashRoot : transform;
            _cachedRenderers = flashRoot.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
            PivotPoint = Rb2.position;
            _currentHp = _maxHp;

            _strategies = new List<AttackStrategy>();
            foreach (var d in AttackData)
                if (d != null) _strategies.Add(new AttackStrategy(d, Animator));

            _patrol      = new PatrolState(this);
            _chase       = new ChaseState(this);
            _search      = new SearchState(this);
            _attackState = new AttackState(this);

            _sm = new MonsterStateMachine();
            _sm.ChangeState(_patrol);
        }

        void Update()
        {
            if (_isDead) return;

            float dt = Time.deltaTime;
            foreach (var s in _strategies) s.Tick(dt);
            _sm.Update(dt);
        }

        void OnAnimatorMove()
        {
            if (_sm.Current is AttackState)
                Rb2.MovePosition(Rb2.position + (Vector2)Animator.deltaPosition);
        }

        public void ChangeState(IMonsterState next) => _sm.ChangeState(next);
        public PatrolState  GetPatrolState()  => _patrol;
        public ChaseState   GetChaseState()   => _chase;
        public SearchState  GetSearchState()  => _search;
        public AttackState  GetAttackState()  => _attackState;

        public bool CanSeePlayer()
        {
            if (PlayerTransform == null) return false;
            return VisionCfg.Contains(Rb2.position, PlayerTransform.position, FacingDir);
        }

        public void MoveToward(Vector2 target, float speed)
        {
            Vector2 dir = target - Rb2.position;
            if (dir.sqrMagnitude > 0.001f)
                SetFacing(dir.x > 0f ? 1 : -1);

            Rb2.MovePosition(Vector2.MoveTowards(Rb2.position, target, speed * Time.deltaTime));
            SyncAnimatorSpeed(speed);
            SetIsMoving(true);
        }

        public void StopMove()
        {
            Rb2.linearVelocity = Vector2.zero;
            SyncAnimatorSpeed(0f);
            SetIsMoving(false);
        }

        void SetIsMoving(bool value)
        {
            if (!string.IsNullOrEmpty(_isMovingParam))
                Animator.SetBool(_isMovingParam, value);
        }

        public void SetFacing(int dir)
        {
            if (FacingDir == dir) return;
            FacingDir = dir;

            // XOR: ถ้า invert อย่างใดอย่างหนึ่ง (ไม่ใช่ทั้งคู่) ให้ flip scale
            bool invert = _invertFacing ^ _invertMoveFacing;
            int scaleDir = invert ? -dir : dir;
            Vector3 s = transform.localScale;
            s.x = Mathf.Abs(s.x) * scaleDir;
            transform.localScale = s;
        }

        public void SyncAnimatorSpeed(float speed)
        {
            float baseSpeed = PatrolCfg.Speed > 0f ? PatrolCfg.Speed : 1f;
            Animator.speed  = _cachedInitAnimSpeed * (speed / baseSpeed);
        }

        public void ResetAnimatorSpeed() => Animator.speed = _cachedInitAnimSpeed;

        public void SetAttackFinished() => _attackFinished = true;
        public bool ConsumeAttackFinished()
        {
            if (!_attackFinished) return false;
            _attackFinished = false;
            return true;
        }

        public void TakeDamage(float damage)
        {
            if (_isDead) return;
            _currentHp = Mathf.Max(0f, _currentHp - damage);
            TriggerHitFlash();
            if (_currentHp <= 0f) Die();
        }

        void TriggerHitFlash()
        {
            if (_cachedRenderers == null || _cachedRenderers.Length == 0) return;
            if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
            _flashCoroutine = StartCoroutine(HitFlashRoutine());
        }

        System.Collections.IEnumerator HitFlashRoutine()
        {
            for (int i = 0; i < _hitLerpCount; i++)
            {
                for (float t = 0f; t < 1f; t += Time.deltaTime * _hitLerpSpeed)
                {
                    SetAllColors(Color.Lerp(_hitStartColor, _hitEndColor, t));
                    yield return null;
                }
                for (float t = 0f; t < 1f; t += Time.deltaTime * _hitLerpSpeed)
                {
                    SetAllColors(Color.Lerp(_hitEndColor, _hitStartColor, t));
                    yield return null;
                }
            }
            SetAllColors(_hitStartColor);
        }

        void SetAllColors(Color color)
        {
            foreach (var sr in _cachedRenderers)
                if (sr != null) sr.color = color;
        }

        private void Die()
        {
            _isDead = true;
            StopMove();
            _sm.ChangeState(null);
            gameObject.SetActive(false);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            HitPlayer(other);
        }

        void OnTriggerStay2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            if (Time.time - _lastDamageTime < _damageCooldown) return;
            HitPlayer(other);
        }

        void HitPlayer(Collider2D playerCol)
        {
            _lastDamageTime = Time.time;
            playerCol.GetComponent<StatsController>()?.TakeDamage(_contactDamage);
        }

        public void EnableHitbox()     => Hitbox?.Enable();
        public void DisableHitbox()    => Hitbox?.Disable();
        public void OnAttackFinished() => SetAttackFinished();

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Vector2 pos = Application.isPlaying ? Rb2.position : (Vector2)transform.position;

            Color col = _sm?.Current switch
            {
                PatrolState  => Color.green,
                ChaseState   => Color.yellow,
                SearchState  => new Color(1f, 0.5f, 0f),
                AttackState  => Color.red,
                _            => Color.green
            };

            VisionCfg.DrawGizmos(pos, FacingDir, col);

            foreach (var d in AttackData)
                d?.Range?.DrawGizmos(pos, FacingDir, Color.red);
        }
#endif
    }
}
