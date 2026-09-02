namespace _02._Script.Player.Interface {
    public interface IStats {
        float Stamina { get; }
        void StaminaUpdate(bool isGrounded, bool isWalking);
        void UseStamina(float usedStamina, bool immediate);
    }
}