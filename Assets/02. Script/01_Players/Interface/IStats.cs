using System;
using _02._Script._01_Players.Components.DamageCompo;

namespace _02._Script._01_Players.Interface {
    public interface IStats {
        event Action<float> OnStaminaChanged;
        float StaminaRatio { get; }
        float Stamina { get; }

        void StaminaUpdate(bool isGrounded, bool isWalking, bool isClimb);
        void UseStamina(float usedStamina, bool immediate);
        void RestoreStamina(float savedStamina);
        void HeatStrokeUpdate(int value, DamageModule damage);
    }
}