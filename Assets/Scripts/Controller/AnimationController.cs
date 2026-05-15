using Genoverrei.Library.Core;
using Genoverrei.Library.DesignPatternCore;
using UnityEngine;

public class AnimationController : MonoBehaviour
{
    [SerializeField] private BasicMovementInputObserverSO _inputObserverSO;

    [SerializeField] private MoveController2D _moveController2D;
    
    [SerializeField] private Animator _animatorB, _animatorC;

    [SerializeField] private GameObject _characterB, _characterC;

    [SerializeField] private Rigidbody2D _body2D;

    public bool HasWeapon;


    private void OnEnable()
    {
        _inputObserverSO.OnLeftClickChannel += OnLeftClick;
        _inputObserverSO.OnRollChannel += OnRoll;
        _inputObserverSO.OnJumpChannel += OnJump;

    }

    private void Update()
    {
        if (_body2D.linearVelocity.magnitude > 0)
        {
            if (HasWeapon)
            {
                _animatorC.SetBool("IsMoving", true);
            }
            else
            {
                _animatorB.SetBool("IsMoving", true);
            }
        }
        else
        {
            if (HasWeapon)
            {
                _animatorC.SetBool("IsMoving", false);
            }
            else
            {
                _animatorB.SetBool("IsMoving", false);
            }
        }
    }

    private void OnLeftClick(ClickData clickData)
    {
        if (HasWeapon)
        {
            _animatorC.SetTrigger("Shoot");
        }
    }

    private void OnRoll()
    {
        if (HasWeapon)
        {
            _animatorC.SetTrigger("Roll");
        }
        else
        {
            _animatorB.SetTrigger("Roll");
        }
    }

    private void OnJump()
    {
        if (!_moveController2D.IsGrounded) return;

        if (HasWeapon)
        {
            _animatorC.SetTrigger("Jump");
        }
        else 
        {
            _animatorB.SetTrigger("Jump");
        }
    }
}
