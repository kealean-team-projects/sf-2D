namespace _02._Script.Players.Interface {
    public interface ICrouchController {
        float MoveSpeedMultiplier { get; }

        void Crouch();
        void Stand();
    }
}