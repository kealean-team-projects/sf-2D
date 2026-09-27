namespace _02._Script._01_Players.Interface {
    public interface ICrouchController {
        bool IsCrouching { get; }
        float MoveSpeedMultiplier { get; }

        void SetCrouchSpeedMultiplier(float multiplier);
        void Crouch();
        void Stand();
    }
}
