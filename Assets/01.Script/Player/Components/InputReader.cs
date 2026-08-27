using System;
using _01.Script.Player;
using _01.Script.Player.Interface;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputReader : MonoBehaviour, Control.IPlayerActions, IAgentModule, IDisposable, IInputReader {
    private Control _control;

    public Type Type => typeof(IInputReader);

    public void Initialize(Agent owner) {
        _control = new Control();
        _control.Player.Enable();
        _control.Player.SetCallbacks(this);
    }

    public void Dispose() {
        _control.Disable();
        _control?.Dispose();
        _control = null;
    }

    public event Action OnInteractPressed;
    public event Action OnJumpPressed;
    public event Action OnSprintPressed;
    public event Action OnSprintReleased;
    public event Action OnCrouchPressed;
    public event Action OnCrouchReleased;
    public float MoveInput { get; private set; }
    public float ClimbInput { get; private set; }


    public void OnMove(InputAction.CallbackContext context) {
        MoveInput = context.ReadValue<float>();
    }

    public void OnInteract(InputAction.CallbackContext context) {
        if (context.performed)
            OnInteractPressed?.Invoke();
    }

    public void OnCrouch(InputAction.CallbackContext context) {
        if (context.performed)
            OnCrouchPressed?.Invoke();
        if(context.canceled)
            OnCrouchReleased?.Invoke();
    }

    public void OnJump(InputAction.CallbackContext context) {
        if (context.performed)
            OnJumpPressed?.Invoke();
    }

    public void OnSprint(InputAction.CallbackContext context) {
        if (context.performed)
            OnSprintPressed?.Invoke();
        if(context.canceled)
            OnSprintReleased?.Invoke();
    }

    public void OnClimb(InputAction.CallbackContext context)
    {
        ClimbInput = context.ReadValue<float>();
    }
}