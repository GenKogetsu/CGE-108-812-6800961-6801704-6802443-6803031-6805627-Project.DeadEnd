using UnityEngine;

namespace MonsterAI
{
    // ─────────────────────────────────────────────────────────────────
    // IMonsterState — interface ที่ทุก State ต้อง implement
    // ─────────────────────────────────────────────────────────────────
    public interface IMonsterState
    {
        void OnEnter();
        void OnUpdate(float dt);
        void OnExit();
    }

    // ─────────────────────────────────────────────────────────────────
    // MonsterStateMachine — เก็บ state ปัจจุบัน + สลับ state
    // ─────────────────────────────────────────────────────────────────
    public class MonsterStateMachine
    {
        public IMonsterState Current { get; private set; }

        public void ChangeState(IMonsterState next)
        {
            Current?.OnExit();
            Current = next;
            Current?.OnEnter();
        }

        public void Update(float dt)
        {
            Current?.OnUpdate(dt);
        }
    }
}
