using Genoverrei.Library.Core;
using Genoverrei.Library.DesignPatternCore;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// กด R → โหลด Scene ปัจจุบันใหม่
/// ใช้ BasicMovementInputObserverSO + BasicSceneEffectController
/// </summary>
public class SceneRestarter : MonoBehaviour
{
    [SerializeField] private BasicMovementInputObserverSO _input;

    private void OnEnable()
    {
        if (_input != null)
            _input.OnRestartChannel += Restart;
    }

    private void OnDisable()
    {
        if (_input != null)
            _input.OnRestartChannel -= Restart;
    }

    private void Restart()
    {
        // ล้าง subscriber ทั้งหมดก่อนโหลด Scene เพื่อป้องกัน MissingReferenceException
        _input?.ClearAllChannels();

        BasicSceneEffectController.Instance.LoadScene(SceneManager.GetActiveScene().name);
    }
}
