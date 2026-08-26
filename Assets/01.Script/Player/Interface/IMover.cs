namespace _01.Script.Player.Interface {
    public interface IMover {
        void SetMoveInput(float moveInput);
        void Jump(float multiplier = 1);
    }
}