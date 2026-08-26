namespace _01.Script.Player.Interface {
    public interface IMover
    {
        bool IsGround {get;}
        void SetMoveInput(float moveInput);
        void Jump(float multiplier = 1);
    }
}