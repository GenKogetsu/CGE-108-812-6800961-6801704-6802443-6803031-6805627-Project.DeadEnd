using UnityEngine;

/// <summary>
/// Boss HP Bar — 2 หลอด (Phase 1 / Phase 2)
/// ทำงานเหมือน HpBarUI ของ Player เปะ แค่มี 2 แถบ
/// Phase 1 หลอดจะลดก่อน พอบอสเข้าเฟส 2 หลอดที่ 1 เป็น 0 แล้วหลอดที่ 2 เริ่มลด
/// </summary>
public class BossHpBarUI : MonoBehaviour
{
    [Header("Boss Reference")]
    [SerializeField] private BossController _boss;

    [Header("Phase 1 Bar")]
    [Tooltip("RectTransform ของ fill Phase 1")]
    [SerializeField] private RectTransform _phase1Fill;

    [Header("Phase 2 Bar")]
    [Tooltip("RectTransform ของ fill Phase 2")]
    [SerializeField] private RectTransform _phase2Fill;

    private float _phase1MaxWidth;
    private float _phase2MaxWidth;

    void Start()
    {
        if (_phase1Fill != null) _phase1MaxWidth = _phase1Fill.sizeDelta.x;
        if (_phase2Fill != null) _phase2MaxWidth = _phase2Fill.sizeDelta.x;

        // Phase 2 bar เริ่มเป็น 0 ก่อน
        SetWidth(_phase2Fill, _phase2MaxWidth, 0f);
    }

    void Update()
    {
        if (_boss == null) return;

        if (!_boss.IsPhase2)
        {
            // ── Phase 1 กำลังลด ──────────────────────────────────────
            float ratio1 = _boss.MaxHp > 0f ? _boss.CurrentHp / _boss.MaxHp : 0f;
            SetWidth(_phase1Fill, _phase1MaxWidth, Mathf.Clamp01(ratio1));
            SetWidth(_phase2Fill, _phase2MaxWidth, 1f);   // Phase 2 เต็มรอ
        }
        else
        {
            // ── Phase 2 กำลังลด ──────────────────────────────────────
            SetWidth(_phase1Fill, _phase1MaxWidth, 0f);   // Phase 1 หมดแล้ว
            float ratio2 = _boss.MaxHp > 0f ? _boss.CurrentHp / _boss.MaxHp : 0f;
            SetWidth(_phase2Fill, _phase2MaxWidth, Mathf.Clamp01(ratio2));
        }
    }

    void SetWidth(RectTransform rt, float maxWidth, float ratio)
    {
        if (rt == null) return;
        rt.sizeDelta = new Vector2(maxWidth * ratio, rt.sizeDelta.y);
    }
}
