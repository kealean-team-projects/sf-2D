using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputReader : MonoBehaviour, Control.IPlayerActions
{
    private Control _control;

    private void Awake()
    {
        _control = new Control();
        _control.Player.Enable();
        _control.Player.SetCallbacks(this);
    }

    private void OnDestroy()
    {
        _control.Disable();
        _control.Dispose();
        
    }

    public event Action OnInteractPressed;
    public event Action OnJumpPressed;
    public event Action OnSprintPressed;
    public event Action OnCrouchPressed;
    public event Action<float> OnMovePressed;
    
    
    public void OnMove(InputAction.CallbackContext context)
    {
        OnMovePressed?.Invoke(context.ReadValue<float>());
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if(context.performed)
            OnInteractPressed?.Invoke();
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        if(context.performed)
            OnCrouchPressed?.Invoke();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if(context.performed)
            OnJumpPressed?.Invoke();
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if(context.performed)
            OnSprintPressed?.Invoke();
    }
}
