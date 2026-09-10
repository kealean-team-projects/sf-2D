using _02._Script.Players.Components;

namespace _02._Script.Players.Interface {
    public interface IMover {
        public MotionType MotionT { get; }
        bool IsGround { get; }
        void SetMoveInput(float moveInput);
        void ClimbInput(float climbInput);
        void Climb(ICheckClimbWall check);
        void CalculateAirTime(ICheckClimbWall checkClimbWall);
        void Jump(float multiplier = 1);
        void WallJump(float dir);
        void WallDash();
        void CancelClimb();
        void SpeedControl(float newSpeed);
    }
}