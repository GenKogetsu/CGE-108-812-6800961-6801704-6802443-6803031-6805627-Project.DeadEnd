using Genoverrei.Library.Core;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Stamina UI แบบ 10 ก้อน — ทุก 10% = 1 ก้อน
/// ลาก Image 10 อันใส่ Blocks แล้วระบุ StatsController
/// </summary>
public class StaminaBlockUI : MonoBehaviour
{
    [SerializeField] private StatsController _stats;
    [Tooltip("Image 10 อัน เรียงจากซ้ายไปขวา (index 0 = ซ้ายสุด)")]
    [SerializeField] private Image[] _blocks = new Image[10];

    void Update()
    {
        if (_stats == null || _blocks == null) return;

        float ratio      = _stats.MaxStamina > 0f
            ? Mathf.Clamp01(_stats.GetCurrentStamina() / _stats.MaxStamina)
            : 0f;
        int activeBlocks = Mathf.RoundToInt(ratio * _blocks.Length);  // ทุก 10% = 1 ก้อน

        for (int i = 0; i < _blocks.Length; i++)
        {
            if (_blocks[i] == null) continue;
            _blocks[i].enabled = i < activeBlocks;
        }
    }
}
