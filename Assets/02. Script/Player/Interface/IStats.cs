namespace _02._Script.Player.Interface {
    public interface IStats {
        float Stamina { get; }
        void StaminaUpdate(bool isGrounded, bool isWalking, bool isClimb);
        void UseStamina(float usedStamina, bool immediate);
    }
}