using System;
using UnityEngine;

namespace MonsterAI
{
    // ─────────────────────────────────────────────────────────────────
    // IAttackStrategy — interface สำหรับท่าโจมตีแต่ละท่า
    // ─────────────────────────────────────────────────────────────────
    public interface IAttackStrategy
    {
        float             MaxCooldown     { get; }
        float             CurrentCooldown { get; }
        bool              IsReady         { get; }
        AttackRangeConfig Range           { get; }

        /// <summary>เริ่มเล่น animation + เตรียมสถานะ (เรียกโดย AttackState.OnEnter)</summary>
        void Execute(Vector2 startPos, int facingDir);

        /// <summary>นับ CD ลงทุก frame (เรียกจาก MonsterController.Update เสมอ)</summary>
        void Tick(float dt);

        /// <summary>Reset CD — เรียกหลัง Execute เสร็จ</summary>
        void StartCooldown();
    }

    // ─────────────────────────────────────────────────────────────────
    // AttackStrategyData — ข้อมูลท่าโจมตีที่ set ใน Inspector
    //   MonsterController จะ new concrete strategy จาก data นี้
    // ─────────────────────────────────────────────────────────────────
    [Serializable]
    public class AttackStrategyData
    {
        [Tooltip("ชื่อ trigger ใน Animator")]
        public string AnimatorTrigger = "Attack";

        [Tooltip("CD (วินาที) ของท่านี้")]
        public float MaxCooldown = 1f;

        [Tooltip("กล่องตรวจจับระยะโจมตี")]
        public AttackRangeConfig Range = new AttackRangeConfig();

        [Tooltip("true = ท่าพิเศษ (ไม้ตาย)")]
        public bool IsSpecial = false;
    }

    // ─────────────────────────────────────────────────────────────────
    // AttackStrategy — concrete strategy สร้างจาก AttackStrategyData
    // ─────────────────────────────────────────────────────────────────
    public class AttackStrategy : IAttackStrategy
    {
        readonly AttackStrategyData _data;
        readonly Animator           _animator;

        float _currentCooldown;

        public float             MaxCooldown     => _data.MaxCooldown;
        public float             CurrentCooldown => _currentCooldown;
        public bool              IsReady         => _currentCooldown <= 0f;
        public AttackRangeConfig Range           => _data.Range;
        public bool              IsSpecial       => _data.IsSpecial;

        public AttackStrategy(AttackStrategyData data, Animator animator)
        {
            _data      = data;
            _animator  = animator;
        }

        public void Execute(Vector2 startPos, int facingDir)
        {
            _animator.SetTrigger(_data.AnimatorTrigger);
        }

        public void Tick(float dt)
        {
            if (_currentCooldown > 0f)
                _currentCooldown -= dt;
        }

        public void StartCooldown()
        {
            _currentCooldown = _data.MaxCooldown;
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // AttackHitbox — MonoBehaviour ที่ติดกับ GameObject ลูก
    //   Enable/Disable ผ่าน Animation Event ใน MonsterController
    // ─────────────────────────────────────────────────────────────────
    [RequireComponent(typeof(Collider2D))]
    public class AttackHitbox : MonoBehaviour
    {
        Collider2D _col;

        void Awake() => _col = GetComponent<Collider2D>();

        public void Enable()
        {
            _col.enabled = true;
        }

        public void Disable()
        {
            _col.enabled = false;
        }

        /// <summary>ปรับขนาด/ตำแหน่ง hitbox ก่อน Enable (optional)</summary>
        public void SetShape(Vector2 localOffset, Vector2 size)
        {
            if (_col is BoxCollider2D box)
            {
                box.offset = localOffset;
                box.size   = size;
            }
        }
    }
}
