namespace _01.Script.Player.Components {
    public interface IMover {
        void SetMoveInput(float moveInput);
        void Jump(float multiplier = 1);
    }
}