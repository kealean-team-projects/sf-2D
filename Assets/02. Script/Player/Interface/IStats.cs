namespace _01.Script.Player.Components {
    public interface IStats {
        float Stamina { get; }
        void StaminaUpdate(bool isGrounded, bool isWalking);
        void UseStamina(float usedStamina, bool immediate);
    }
}