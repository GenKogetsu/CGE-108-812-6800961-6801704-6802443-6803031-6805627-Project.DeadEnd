using UnityEngine;

/// <summary>
/// ใส่บน Bullet Prefab
/// เคลื่อนที่ตามทิศที่ถูก set ตอน spawn
/// ชนกับ tag "Enemy" หรือหมดเวลา → Destroy
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    [Tooltip("ความเร็วกระสุน")]
    [SerializeField] private float _speed    = 12f;

    [Tooltip("กระสุนอยู่ได้กี่วินาทีก่อนหาย")]
    [SerializeField] private float _lifeTime = 3f;

    [Tooltip("tag ที่ทำให้กระสุนหาย")]
    [SerializeField] private string _enemyTag = "Enemy";

    [Tooltip("Damage ที่ทำกับ Enemy (ถ้า Enemy มี BossController)")]
    [SerializeField] private float _damage = 20f;

    // ── Runtime ───────────────────────────────────────────────────────
    private Rigidbody2D _rb;
    private Vector2     _direction;

    // ─────────────────────────────────────────────────────────────────
    private void Awake()
    {
        _rb              = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
    }

    private void Start()
    {
        // ทำลายตัวเองหลัง lifeTime วินาที
        Destroy(gameObject, _lifeTime);
    }

    private void FixedUpdate()
    {
        _rb.linearVelocity = _direction * _speed;
    }

    // ─────────────────────────────────────────────────────────────────
    /// <summary>เรียกจาก BulletShooter หลัง Instantiate เพื่อกำหนดทิศ</summary>
    public void SetDirection(Vector2 dir)
    {
        _direction = dir.normalized;

        // คำนวณ angle จากทิศ แล้วบวก offset 90° (เพราะ sprite หันขึ้นเป็น default)
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + 90f);
    }

    // ─────────────────────────────────────────────────────────────────
    // ชนกับอะไรก็ตาม (Trigger)
    // ─────────────────────────────────────────────────────────────────
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(_enemyTag)) return;
        DealDamage(other.gameObject);
        Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag(_enemyTag)) return;
        DealDamage(collision.gameObject);
        Destroy(gameObject);
    }

    private void DealDamage(GameObject target)
    {
        // ค้นหาทั้ง object เดียวกันและ parent (รองรับ Collider บน child)
        var monster = target.GetComponentInParent<MonsterAI.MonsterController>();
        if (monster != null) { monster.TakeDamage(_damage); return; }

        var boss = target.GetComponentInParent<BossController>();
        boss?.TakeDamage(_damage);
    }
}
