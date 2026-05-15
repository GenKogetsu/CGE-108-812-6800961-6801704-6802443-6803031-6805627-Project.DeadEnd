using UnityEngine;

[ExecuteAlways]
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform _target;

    [SerializeField] private Vector3 _offset = new Vector3(0f, 2f, -10f);

    [SerializeField] private float _smoothSpeed = 8f;

    void LateUpdate()
    {
        if (_target == null) return;

        Vector3 desired = _target.position + _offset;
        transform.position = Vector3.Lerp(transform.position, desired, _smoothSpeed * Time.deltaTime);
    }
}