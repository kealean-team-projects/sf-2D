using System;

namespace _01.Script.Player.Interface {
    public interface IInputReader {
        float MoveInput { get; }
        event Action OnInteractPressed;
        event Action OnJumpPressed;
        event Action OnSprintPressed;
        event Action OnCrouchPressed;
    }
}