using Genoverrei.Library.Core;
using UnityEngine;

/// <summary>
/// Simple 2-Phase Boss Controller
///   Phase 1 : plays "Phase_1_All" clip, moves toward player
///   Phase 2 : switches model, plays "Phase_2_All" clip, continues
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BossController : MonoBehaviour
{
    // ── Phase Models ──────────────────────────────────────────────────
    [Header("Phase Models")]
    [Tooltip("Drag 'บอส_แก้_' GameObject here")]
    [SerializeField] private GameObject _phase1Model;

    [Tooltip("Drag 'บอสเฟส2' GameObject here")]
    [SerializeField] private GameObject _phase2Model;

    [Tooltip("Animator on Phase 1 model")]
    [SerializeField] private Animator _phase1Animator;

    [Tooltip("Animator on Phase 2 model")]
    [SerializeField] private Animator _phase2Animator;

    // ── Animation Clip Names ──────────────────────────────────────────
    [Header("Animation Clip Names")]
    [Tooltip("Must match the State name in Animator exactly")]
    [SerializeField] private string _phase1ClipName = "Phase_1_All";
    [SerializeField] private string _phase2ClipName = "Phase_2_All";

    // ── Stats ─────────────────────────────────────────────────────────
    [Header("Boss Stats")]
    [SerializeField] private float _phase1MaxHp = 500f;
    [SerializeField] private float _phase2MaxHp = 500f;

    // ── Movement ──────────────────────────────────────────────────────
    [Header("Movement")]
    [Tooltip("How fast the boss moves toward the player")]
    [SerializeField] private float _moveSpeed   = 3f;

    [Tooltip("Boss starts chasing player within this distance")]
    [SerializeField] private float _detectRange = 20f;

    [Tooltip("Boss stops moving and attacks within this distance")]
    [SerializeField] private float _stopRange   = 2.5f;

    [Tooltip("Uncheck this if your animation clip already controls the facing direction")]
    [SerializeField] private bool _autoFlip = false;

    // ── Attack / Damage ───────────────────────────────────────────────
    [Header("Attack")]
    [Tooltip("Damage dealt to player on contact")]
    [SerializeField] private float _contactDamage  = 20f;

    [Tooltip("Seconds between each damage tick when overlapping player")]
    [SerializeField] private float _damageCooldown = 0.8f;

    [Tooltip("Min seconds between attack animation plays")]
    [SerializeField] private float _attackIntervalMin = 1.5f;

    [Tooltip("Max seconds between attack animation plays")]
    [SerializeField] private float _attackIntervalMax = 3f;

    // ── Game Over ─────────────────────────────────────────────────────
    [Header("Game Over")]
    [Tooltip("CanvasGroup on a full-screen black panel (set alpha = 0 in Inspector)")]
    [SerializeField] private CanvasGroup _fadeCanvas;
    [SerializeField] private float       _fadeDuration = 2f;

    // ── Private Runtime ───────────────────────────────────────────────
    private Rigidbody2D _rb;
    private Animator    _anim;          // currently active animator
    private Transform   _player;

    private float _currentHp;
    private bool  _isPhase2;
    private bool  _isDead;
    private float _lastDamageTime  = -99f;
    private float _nextAttackTime  = 0f;

    // ── Public (used by BossHPUIController) ───────────────────────────
    public float CurrentHp => _currentHp;
    public float MaxHp     => _isPhase2 ? _phase2MaxHp : _phase1MaxHp;
    public bool  IsPhase2  => _isPhase2;

    // ─────────────────────────────────────────────────────────────────
    // Awake — setup physics, auto-find components
    // ─────────────────────────────────────────────────────────────────
    private void Awake()
    {
        _rb              = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.constraints  = RigidbodyConstraints2D.FreezeRotation;

        // Auto-find animator on this object if Phase1Animator not assigned
        if (_phase1Animator == null)
            _phase1Animator = GetComponentInChildren<Animator>();
    }

    // ─────────────────────────────────────────────────────────────────
    // Start
    // ─────────────────────────────────────────────────────────────────
    private void Start()
    {
        // Find player
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            _player = playerObj.transform;
        else
            Debug.LogWarning("[Boss] No GameObject with tag 'Player' found!");

        // Activate Phase 1 model, hide Phase 2
        if (_phase1Model != null) _phase1Model.SetActive(true);
        if (_phase2Model != null) _phase2Model.SetActive(false);

        // Set active animator
        _anim      = _phase1Animator;
        _currentHp = _phase1MaxHp;

        // Schedule first attack immediately
        _nextAttackTime = Time.time + Random.Range(_attackIntervalMin, _attackIntervalMax);

        // Play Phase 1 animation right away
        PlayClip(_phase1ClipName);

        BossHPUIController.Instance?.Refresh(this);

        Debug.Log($"[Boss] Ready — HP:{_currentHp}  Player:{(_player != null ? _player.name : "MISSING")}");
    }

    // ─────────────────────────────────────────────────────────────────
    // Update — runs every frame
    // ─────────────────────────────────────────────────────────────────
    private void Update()
    {
        if (_isDead) return;

        // Re-find player if lost
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

            if (dist > _stopRange)
            {
                // ── Move toward player ────────────────────────────────
                _rb.linearVelocity = dir * _moveSpeed;
            }
            else
            {
                // ── In attack range — stop and attack ─────────────────
                _rb.linearVelocity = Vector2.zero;
            }

            // ── Play attack animation on random interval ──────────────
            if (Time.time >= _nextAttackTime)
            {
                _nextAttackTime = Time.time + Random.Range(_attackIntervalMin, _attackIntervalMax);
                PlayClip(_isPhase2 ? _phase2ClipName : _phase1ClipName);
            }
        }
        else
        {
            // ── Out of range — stop ───────────────────────────────────
            _rb.linearVelocity = Vector2.zero;
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // Flip sprite to face direction
    // ─────────────────────────────────────────────────────────────────
    private void FlipToward(float dirX)
    {
        if (Mathf.Abs(dirX) < 0.01f) return;
        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * (dirX > 0f ? 1f : -1f);
        transform.localScale = s;
    }

    // ─────────────────────────────────────────────────────────────────
    // Play animation clip by state name
    // ─────────────────────────────────────────────────────────────────
    private void PlayClip(string stateName)
    {
        if (_anim == null || string.IsNullOrEmpty(stateName)) return;
        _anim.Play(stateName, 0, 0f);   // layer 0, restart from beginning
    }

    // ─────────────────────────────────────────────────────────────────
    // TakeDamage — called by player attack
    // ─────────────────────────────────────────────────────────────────
    public void TakeDamage(float damage)
    {
        if (_isDead) return;

        _currentHp = Mathf.Max(0f, _currentHp - damage);
        Debug.Log($"[Boss] Hit! HP: {_currentHp}/{MaxHp}");
        BossHPUIController.Instance?.Refresh(this);

        if (_currentHp <= 0f)
        {
            if (!_isPhase2) SwitchToPhase2();
            else            GameOver();
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // Switch to Phase 2
    // ─────────────────────────────────────────────────────────────────
    private void SwitchToPhase2()
    {
        Debug.Log("[Boss] Phase 1 defeated → Phase 2!");

        _isPhase2  = true;
        _currentHp = _phase2MaxHp;

        // Swap models
        if (_phase1Model != null) _phase1Model.SetActive(false);
        if (_phase2Model != null) _phase2Model.SetActive(true);

        // Swap animator (keep phase1 animator if phase2 not assigned)
        if (_phase2Animator != null)
            _anim = _phase2Animator;

        // Re-cache SpriteRenderers หลังเปิดโมเดลใหม่

        _nextAttackTime = Time.time; // attack immediately

        BossHPUIController.Instance?.Refresh(this);
        PlayClip(_phase2ClipName);
    }

    // ─────────────────────────────────────────────────────────────────
    // Game Over
    // ─────────────────────────────────────────────────────────────────
    private void GameOver()
    {
        Debug.Log("[Boss] Defeated! Game Over.");
        _isDead            = true;
        _rb.linearVelocity = Vector2.zero;
        StartCoroutine(FadeToBlack());
    }

    private IEnumerator FadeToBlack()
    {
        if (_fadeCanvas == null)
        {
            Debug.LogWarning("[Boss] FadeCanvas not assigned — skipping fade.");
            yield break;
        }

        _fadeCanvas.gameObject.SetActive(true);
        float elapsed = 0f;
        while (elapsed < _fadeDuration)
        {
            elapsed          += Time.deltaTime;
            _fadeCanvas.alpha = Mathf.Clamp01(elapsed / _fadeDuration);
            yield return null;
        }
        _fadeCanvas.alpha = 1f;
    }

    // ─────────────────────────────────────────────────────────────────
    // Circle Collider 2D — damage player on contact
    // ─────────────────────────────────────────────────────────────────
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

        // Damage via library StatsController
        playerCol.GetComponent<StatsController>()?.TakeDamage(_contactDamage);
    }

    // ─────────────────────────────────────────────────────────────────
    // Gizmos
    // ─────────────────────────────────────────────────────────────────
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
