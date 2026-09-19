using System;

namespace _02._Script._01_Players.Interface {
    public interface IInputReader {
        float MoveInput { get; }
        float ClimbInput { get; }
        event Action OnInteractPressed;
        event Action OnJumpPressed;
        event Action OnSprintPressed;
        event Action OnSprintReleased;
        event Action OnCrouchPressed;
        event Action OnCrouchReleased;
    }
}