namespace _02._Script.Players.Interface {
    public interface ICrouchController {
        float MoveSpeedMultiplier { get; }

        void SetCrouchSpeedMultiplier(float multiplier);
        void Crouch();
        void Stand();
    }
}