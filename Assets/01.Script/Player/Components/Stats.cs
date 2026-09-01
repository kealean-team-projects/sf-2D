using System;
using _01.Script.Player.Interface;
using UnityEngine;

namespace _01.Script.Player.Components {
    public class Stats : MonoBehaviour, IAgentModule, IStats {
        [SerializeField] private float staminaHealSlow;
        [SerializeField] private float staminaHealBoost;
        [SerializeField] private float maxStamina;
        
        public float Stamina { get; private set; }

        public void Initialize(Agent owner) {
            Stamina = maxStamina;
        }
        
        public Type Type => typeof(IStats);

        public void StaminaUpdate(bool isGrounded, bool isWalking) {
            if(!isGrounded) return;
            float healRate = isWalking ? staminaHealSlow : staminaHealBoost;
            Stamina = Mathf.Clamp(Stamina + healRate * Time.deltaTime, 0, maxStamina);
        }

        public void UseStamina(float usedStamina, bool immediate) {
            if(Stamina == 0f) return;
            if(!immediate) Stamina = Mathf.Clamp(Stamina - usedStamina * Time.deltaTime, 0, maxStamina);
            else Stamina = Mathf.Clamp(Stamina - usedStamina, 0, maxStamina);
        }
    }
}