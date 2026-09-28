using UnityEngine;

namespace _02._Script._01_Players.Interface {
    public interface IMover {
        bool IsGround { get; }
        float VerticalSpeed { get; }
        bool CanClimb { get; }
        void CalculateAirTime(bool isClimbing);
        void Jump(float multiplier = 1);
        void WallDash();
        void CancelClimb();
        void ApplyManualMove(float moveSpeed);
        void ApplyManualMoveY(float moveSpeed);
        void ApplyClimb(float climbSpeed);
        void ApplyWallDash(float impulse);
        void ApplyWallJump(Vector2 jumpSpeed);

        void EndWallDash();
        void RestorePosition(Vector2 position);
    }
}