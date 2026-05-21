using Kogetsu.Library.Core;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BossController : MonoBehaviour
{
    [Header("Phase Models")]
    [SerializeField] private GameObject _phase1Model;
    [SerializeField] private GameObject _phase2Model;
    [SerializeField] private Animator   _phase1Animator;
    [SerializeField] private Animator   _phase2Animator;

    [Header("Animation Clip Names")]
    [SerializeField] private string _phase1ClipName = "Phase_1_All";
    [SerializeField] private string _phase2ClipName = "Phase_2_All";

    [Header("HP")]
    [SerializeField] private float _phase1MaxHp = 500f;
    [SerializeField] private float _phase2MaxHp = 500f;

    [Header("Hit Flash")]
    [Tooltip("Root ที่มี SpriteRenderer ทั้งหมด (รวม inactive) — ถ้าไม่ใส่ใช้ transform ตัวเอง")]
    [SerializeField] private Transform _hitFlashRoot;
    [SerializeField] private Color     _hitStartColor = Color.white;
    [SerializeField] private Color     _hitEndColor   = Color.red;
    [SerializeField] private float     _hitLerpSpeed  = 10f;
    [SerializeField] private int       _hitLerpCount  = 2;

    [Header("Movement")]
    [SerializeField] private float _moveSpeed    = 3f;
    [SerializeField] private float _detectRange  = 20f;
    [SerializeField] private float _stopRange    = 2.5f;
    [SerializeField] private bool  _autoFlip     = false;

    [Header("Attack")]
    [SerializeField] private float _contactDamage    = 20f;
    [SerializeField] private float _damageCooldown   = 0.8f;
    [SerializeField] private float _attackIntervalMin = 1.5f;
    [SerializeField] private float _attackIntervalMax = 3f;

    [Header("Game Over")]
    [Tooltip("Canvas/Panel ที่จะ SetActive(true) ตอนบอสตาย")]
    [SerializeField] private GameObject _gameOverCanvas;

    private Rigidbody2D      _rb;
    private Animator         _anim;
    private Transform        _player;
    private SpriteRenderer[] _cachedRenderers;
    private Coroutine        _flashCoroutine;

    [Header("Runtime (ReadOnly)")]
    [ReadOnly] [SerializeField] private float _currentHp;
    [ReadOnly] [SerializeField] private bool  _isPhase2;
    private bool  _isDead;
    private float _lastDamageTime = -99f;
    private float _nextAttackTime = 0f;

    public float CurrentHp => _currentHp;
    public float MaxHp     => _isPhase2 ? _phase2MaxHp : _phase1MaxHp;
    public bool  IsPhase2  => _isPhase2;

    private void Awake()
    {
        _rb              = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.constraints  = RigidbodyConstraints2D.FreezeRotation;

        if (_phase1Model != null) _phase1Model.SetActive(true);
        if (_phase2Model != null) _phase2Model.SetActive(false);

        if (_phase1Animator == null)
            _phase1Animator = _phase1Model != null
                ? _phase1Model.GetComponentInChildren<Animator>()
                : GetComponentInChildren<Animator>();

        Transform flashRoot = _hitFlashRoot != null ? _hitFlashRoot : transform;
        _cachedRenderers = flashRoot.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
    }

    private void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        _player = playerObj != null ? playerObj.transform : null;
        if (_player == null)
            Debug.LogWarning("[Boss] No GameObject with tag 'Player' found!");

        _isPhase2  = false;
        _currentHp = _phase1MaxHp;
        _anim      = _phase1Animator;

        if (_phase1Model != null) _phase1Model.SetActive(!_isPhase2);
        if (_phase2Model != null) _phase2Model.SetActive(_isPhase2);

        _nextAttackTime = Time.time + Random.Range(_attackIntervalMin, _attackIntervalMax);
        PlayClip(_phase1ClipName);
        BossHPUIController.Instance?.Refresh(this);
    }

    private void Update()
    {
        if (_isDead) return;

        if (_player == null)
        {
            var obj = GameObject.FindGameObjectWithTag("Player");
            if (obj != null) _player = obj.transform;
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        float dist = Vector2.Distance(transform.position, _player.position);

        if (dist <= _detectRange)
        {
            Vector2 dir = ((Vector2)_player.position - (Vector2)transform.position).normalized;
            if (_autoFlip) FlipToward(dir.x);

            _rb.linearVelocity = dist > _stopRange ? dir * _moveSpeed : Vector2.zero;

            if (Time.time >= _nextAttackTime)
            {
                _nextAttackTime = Time.time + Random.Range(_attackIntervalMin, _attackIntervalMax);
                PlayClip(_isPhase2 ? _phase2ClipName : _phase1ClipName);
            }
        }
        else
        {
            _rb.linearVelocity = Vector2.zero;
        }
    }

    public void TakeDamage(float damage)
    {
        if (_isDead) return;

        _currentHp = Mathf.Max(0f, _currentHp - damage);
        TriggerHitFlash();
        BossHPUIController.Instance?.Refresh(this);

        if (_currentHp <= 0f)
        {
            if (!_isPhase2) SwitchToPhase2();
            else            GameOver();
        }
    }

    private void TriggerHitFlash()
    {
        if (_cachedRenderers == null || _cachedRenderers.Length == 0) return;
        if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
        _flashCoroutine = StartCoroutine(HitFlashRoutine());
    }

    private System.Collections.IEnumerator HitFlashRoutine()
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

    private void SetAllColors(Color color)
    {
        foreach (var sr in _cachedRenderers)
            if (sr != null) sr.color = color;
    }

    private void SwitchToPhase2()
    {
        _isPhase2  = true;
        _currentHp = _phase2MaxHp;

        if (_phase1Model != null) _phase1Model.SetActive(false);
        if (_phase2Model != null) _phase2Model.SetActive(true);

        if (_phase2Animator != null) _anim = _phase2Animator;

        Transform flashRoot = _hitFlashRoot != null ? _hitFlashRoot : transform;
        _cachedRenderers = flashRoot.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);

        _nextAttackTime = Time.time;
        BossHPUIController.Instance?.Refresh(this);
        PlayClip(_phase2ClipName);
    }

    private void GameOver()
    {
        _isDead            = true;
        _rb.linearVelocity = Vector2.zero;
        gameObject.SetActive(false);

        if (_gameOverCanvas != null)
            _gameOverCanvas.SetActive(true);
    }

    private void FlipToward(float dirX)
    {
        if (Mathf.Abs(dirX) < 0.01f) return;
        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * (dirX > 0f ? 1f : -1f);
        transform.localScale = s;
    }

    private void PlayClip(string stateName)
    {
        if (_anim == null || string.IsNullOrEmpty(stateName)) return;
        _anim.Play(stateName, 0, 0f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        DamagePlayer(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (Time.time - _lastDamageTime < _damageCooldown) return;
        DamagePlayer(other);
    }

    private void DamagePlayer(Collider2D playerCol)
    {
        _lastDamageTime = Time.time;
        playerCol.GetComponentInParent<StatsController>()?.TakeDamage(_contactDamage);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _detectRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _stopRange);
    }
#endif
}
