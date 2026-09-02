using _01.Script.Player.Components;

namespace _01.Script.Player.Interface {
    public interface IMover {
        bool IsGround { get; }
        void SetMoveInput(float moveInput);
        void ClimbInput(float climbInput);
        void Climb(ICheckClimbWall check);
        void CalculateAirTime(ICheckClimbWall checkClimbWall);
        void Jump(float multiplier = 1);
        void WallJump(float dir);
        void WallDash();
        void CancelClimb();
    }
}