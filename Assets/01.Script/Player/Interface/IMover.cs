namespace _01.Script.Player.Interface {
    public interface IMover {
        bool IsGround { get; }
        bool IsClimbed { get; }
        void SetMoveInput(float moveInput);
        void ClimbInput(float climbInput);
        void Jump(float multiplier = 1);
        void WallJump(float dir);
        void WallDash();
        void CancelClimb();
    }
}