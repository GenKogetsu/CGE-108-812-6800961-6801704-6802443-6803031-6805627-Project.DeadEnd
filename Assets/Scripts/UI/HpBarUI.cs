using Genoverrei.Library.Core;
using UnityEngine;

/// <summary>
/// แถบ HP แบบง่าย — ปรับ sizeDelta.x ตามสัดส่วน currentHp / maxHp
/// ใส่ script นี้บน GameObject ของ fill bar (RectTransform)
/// </summary>
public class HpBarUI : MonoBehaviour
{
    [SerializeField] private StatsController _stats;
    [Tooltip("RectTransform ของ fill (แถบสี) ที่จะปรับ width")]
    [SerializeField] private RectTransform   _fillRect;

    private float _maxWidth;

    void Start()
    {
        if (_fillRect == null)
            _fillRect = GetComponent<RectTransform>();

        _maxWidth = _fillRect.sizeDelta.x;
    }

    void Update()
    {
        if (_stats == null || _fillRect == null) return;

        float ratio = _stats.MaxHp > 0f ? _stats.GetCurrentHp() / _stats.MaxHp : 0f;
        ratio = Mathf.Clamp01(ratio);

        _fillRect.sizeDelta = new Vector2(_maxWidth * ratio, _fillRect.sizeDelta.y);
    }
}
