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
        var currentScene = SceneManager.GetActiveScene();
        BasicSceneEffectController.Instance.LoadScene(currentScene.name);
    }
}
