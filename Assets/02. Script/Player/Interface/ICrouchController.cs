namespace _02._Script.Player.Interface {
    public interface ICrouchController {
        float MoveSpeedMultiplier { get; }

        void Crouch();
        void Stand();
    }
}