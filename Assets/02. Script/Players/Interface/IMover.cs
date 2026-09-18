using UnityEngine;

namespace _02._Script.Players.Interface {
    public interface IMover {
        bool IsGround { get; }
        bool CanClimb { get; }
        void CalculateAirTime(bool isClimbing);
        void Jump(float multiplier = 1);
        void WallDash();
        void CancelClimb();
        void ApplyManualMove(float moveSpeed);
        void ApplyClimb(float climbSpeed);
        void ApplyWallDash(float impulse);
        void ApplyWallJump(Vector2 jumpSpeed);

        void EndWallDash();
    }
}