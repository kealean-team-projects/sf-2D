using UnityEngine;

namespace _02._Script._01_Players.Interface {
    public interface IPlayerMoveContext {
        IMover Mover { get; }

        bool CanSJ { get; }
        bool IsGrounded { get; }
        bool IsClimb { get; }

        float MoveInput { get; }
        float ClimbInput { get; }
        float ClimbSpeed { get; }
        float SpeedMultiplier { get; }
        float CrouchSpeedMultiplier { get; }
        float PushSpeed { get; }

        Vector2 JumpSpeed { get; }
        float JumpDuration { get; }
        float Impulse { get; }
        float ClimbStaminaCostPerSecond { get; }

        void Jump();
        bool TryWallJump();
        bool TryWallDash();
        void CancelClimb();
        void EndWallDash();
    }
}