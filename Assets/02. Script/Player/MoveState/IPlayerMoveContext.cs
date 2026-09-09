namespace _02._Script.Player.MoveState
{
    public interface IPlayerMoveContext
    {
        float MoveInput { get; }

        void ApplyMoveInput(float input);
    }
}