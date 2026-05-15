using UnityEngine;
using Genoverrei.Library.Core;


public class NextSceneTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            BasicSceneEffectController.Instance.LoadNextScene(1);
        }
    }
}