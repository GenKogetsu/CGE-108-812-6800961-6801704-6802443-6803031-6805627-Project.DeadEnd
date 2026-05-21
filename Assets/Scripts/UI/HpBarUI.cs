using Kogetsu.Library.Core;
using UnityEngine;

public class HpBarUI : MonoBehaviour
{
    [SerializeField] private StatsController _stats;
    [Tooltip("RectTransform ของ fill (แถบสี) ที่จะปรับ width")]
    [SerializeField] private RectTransform   _fillRect;

    [Header("Game Over")]
    [Tooltip("Canvas/Panel ที่จะ SetActive(true) เมื่อ HP = 0")]
    [SerializeField] private GameObject _gameOverCanvas;

    private float _maxWidth;
    private bool  _gameOverShown;

    void Start()
    {
        if (_fillRect == null)
            _fillRect = GetComponent<RectTransform>();

        _maxWidth = _fillRect.sizeDelta.x;

        if (_gameOverCanvas != null)
            _gameOverCanvas.SetActive(false);
    }

    void Update()
    {
        if (_stats == null || _fillRect == null) return;

        float ratio = _stats.MaxHp > 0f ? _stats.GetCurrentHp() / _stats.MaxHp : 0f;
        ratio = Mathf.Clamp01(ratio);

        _fillRect.sizeDelta = new Vector2(_maxWidth * ratio, _fillRect.sizeDelta.y);

        if (!_gameOverShown && ratio <= 0f)
        {
            _gameOverShown = true;
            if (_gameOverCanvas != null)
                _gameOverCanvas.SetActive(true);
        }
    }
}
