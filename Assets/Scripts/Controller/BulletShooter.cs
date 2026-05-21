using Kogetsu.Library.Core;
using Kogetsu.Library.DesignPatternCore;
using UnityEngine;

public class BulletShooter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BasicMovementInputObserverSO _inputObserver;

    [Tooltip("Bullet Prefab ที่มี Bullet component")]
    [SerializeField] private GameObject _bulletPrefab;

    [Tooltip("จุดกำเนิดกระสุน — ถ้าว่างจะใช้ตำแหน่ง GameObject นี้")]
    [SerializeField] private Transform _spawnPoint;

    [Tooltip("Transform ที่ใช้อ่านทิศหัน (localScale.x) — ถ้าว่างจะใช้ GameObject นี้")]
    [SerializeField] private Transform _facingTarget;

    [Header("Shoot Settings")]
    [Tooltip("หน่วงระหว่างยิงแต่ละครั้ง (วินาที)")]
    [SerializeField] private float _fireRate = 0.3f;

    private float _nextFireTime;

    public enum FlipMode { ScaleX, FlipX }

    private Vector2 FacingDir
    {
        get
        {
            return new Vector2(_facingTarget.localScale.x >= 0f ? 1f : -1f, 0f);
        }
    }

    private void Awake()
    {
        if (_spawnPoint   == null) _spawnPoint   = transform;
        if (_facingTarget == null) _facingTarget = transform;
    }

    private void OnEnable()
    {
        if (_inputObserver != null)
            _inputObserver.OnLeftClickChannel += OnLeftClick;
        else
            Debug.LogWarning("[BulletShooter] Input Observer ไม่ได้ assign!");
    }

    private void OnDisable()
    {
        if (_inputObserver != null)
            _inputObserver.OnLeftClickChannel -= OnLeftClick;
    }

    private void OnLeftClick(ClickData _)
    {
        if (Time.time < _nextFireTime) return;

        Transform t = _facingTarget != null ? _facingTarget : transform;
        Debug.Log($"[BulletShooter] FacingDir={FacingDir}  scaleX={t.localScale.x:F2}");

        Shoot();
        _nextFireTime = Time.time + _fireRate;
    }

    private void Shoot()
    {
        if (_bulletPrefab == null)
        {
            Debug.LogWarning("[BulletShooter] Bullet Prefab ไม่ได้ assign!");
            return;
        }

        var bulletObj = Instantiate(_bulletPrefab, _spawnPoint.position, Quaternion.identity);

        if (bulletObj.TryGetComponent<Bullet>(out var bullet))
            bullet.SetDirection(FacingDir);
        else
            Debug.LogWarning("[BulletShooter] Bullet Prefab ไม่มี Bullet component!");
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_spawnPoint == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(_spawnPoint.position, 0.15f);
        Gizmos.DrawRay(_spawnPoint.position, (Vector3)FacingDir * 1.5f);
    }
#endif
}
