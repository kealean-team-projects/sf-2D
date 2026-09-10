namespace _02._Script.Players.MoveState {
    public interface IPlayerMoveContext {
        float MoveInput { get; }

        void ApplyMoveInput(float input);
    }
}