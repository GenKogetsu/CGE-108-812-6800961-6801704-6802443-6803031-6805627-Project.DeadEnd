using Genoverrei.Library.Core;
using Genoverrei.Library.DesignPatternCore;
using UnityEngine;

public class UseGun : MonoBehaviour
{
    public bool Inrange;
    public BasicMovementInputObserverSO InputObserverSO;

    public AnimationController AnimationController;
    public StatsController StatsController;

    public GameObject PlayerB;
    public GameObject PlayerC;
     private void OnEnable()
    {
        InputObserverSO.OnInteractionChannel += OnInterac;
    }
    private void OnDisable()
    {
        InputObserverSO.OnInteractionChannel -= OnInterac;
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Inrange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Inrange = false;
        }
    }

    private void OnInterac()
    {
        Debug.Log(Inrange);
        if (Inrange)
        {
            PlayerB.SetActive(false);
            AnimationController.HasWeapon = true;
            StatsController.ResetSprite();
            PlayerC.SetActive(true);
            this.gameObject.SetActive(false);
        }

    }
}
