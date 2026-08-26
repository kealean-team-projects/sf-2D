using System;

namespace _01.Script.Player.Interface {
    public interface IInputReader {
        event Action OnInteractPressed;
        event Action OnJumpPressed;
        event Action OnSprintPressed;
        event Action OnCrouchPressed;
        float MoveInput { get; }
    }
}