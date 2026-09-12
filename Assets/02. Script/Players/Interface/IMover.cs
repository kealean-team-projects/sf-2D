using _02._Script.Players.Components;

namespace _02._Script.Players.Interface {
    public interface IMover {
        public MotionType MotionT { get; }
        bool IsGround { get; }
        bool CanClimb { get; }
        void Climb(ICheckClimbWall check);
        void CalculateAirTime(ICheckClimbWall checkClimbWall);
        void Jump(float multiplier = 1);
        void WallJump(float dir);
        void WallDash();
        void CancelClimb();
        void SpeedControl(float newSpeed);
        
        void ApplyManualMove(float moveSpeed);
        void ApplyClimb(float climbSpeed);
        void ApplyWallDash(float impulse);
        void ApplyWallJump(float xSpeed, float ySpeed);
        
        void EndWallDash();
    }
}