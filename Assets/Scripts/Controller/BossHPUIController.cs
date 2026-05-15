using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI Controller สำหรับแสดง HP ของ Boss
///
/// Setup:
///   1. ใส่ Script นี้บน Canvas GameObject ที่แสดง Boss HP
///   2. ผูก FrontBar (Slider) = HP ปัจจุบัน
///   3. ผูก BackBar (Slider) = แถบ lag (optional, สวยงาม)
///   4. ผูก Phase1Label / Phase2Label (optional)
///   5. GameObject ที่มี BossController จะ Refresh อัตโนมัติ
/// </summary>
public class BossHPUIController : MonoBehaviour
{
    public static BossHPUIController Instance { get; private set; }

    // ── UI References ─────────────────────────────────────────────────
    [Header("HP Bar")]
    [Tooltip("Slider หลัก — แสดง HP ปัจจุบัน (ลดทันที)")]
    [SerializeField] private Slider _frontBar;

    [Tooltip("Slider ด้านหลัง — Lerp ตาม (optional, ทำให้ดูนุ่มนวล)")]
    [SerializeField] private Slider _backBar;

    [Tooltip("ความเร็ว Lerp ของ BackBar")]
    [SerializeField] private float  _backBarSpeed = 4f;

    // ── Text (optional) ───────────────────────────────────────────────
    [Header("Text (optional)")]
    [Tooltip("แสดง HP เป็นตัวเลข เช่น  500 / 500")]
    [SerializeField] private TMP_Text _hpText;

    // ── Phase Labels (optional) ───────────────────────────────────────
    [Header("Phase Labels (optional)")]
    [SerializeField] private GameObject _phase1Label;
    [SerializeField] private GameObject _phase2Label;

    // ── Runtime ───────────────────────────────────────────────────────
    private float _targetFill = 1f;

    // ─────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (_backBar == null) return;
        _backBar.value = Mathf.Lerp(_backBar.value, _targetFill, Time.deltaTime * _backBarSpeed);
    }

    // ─────────────────────────────────────────────────────────────────
    /// <summary>เรียกจาก BossController ทุกครั้งที่ HP เปลี่ยน</summary>
    public void Refresh(BossController boss)
    {
        if (boss == null) return;

        float fill = boss.MaxHp > 0f ? Mathf.Clamp01(boss.CurrentHp / boss.MaxHp) : 0f;
        _targetFill = fill;

        if (_frontBar != null)
            _frontBar.value = fill;

        if (_hpText != null)
            _hpText.text = $"{Mathf.CeilToInt(boss.CurrentHp)} / {Mathf.CeilToInt(boss.MaxHp)}";

        if (_phase1Label != null) _phase1Label.SetActive(!boss.IsPhase2);
        if (_phase2Label != null) _phase2Label.SetActive(boss.IsPhase2);
    }
}
