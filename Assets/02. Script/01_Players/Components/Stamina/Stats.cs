using System;
using _02._Script._01_Players.Components.DamageCompo;
using _02._Script._01_Players.Interface;
using _02._Script._05_Managers;
using UnityEngine;

namespace _02._Script._01_Players.Components.Stamina {
    public class Stats : MonoBehaviour, IAgentModule, IStats {
        [SerializeField] private float staminaHealSlow;
        [SerializeField] private float staminaHealBoost;
        [SerializeField] private float maxStamina;
        [SerializeField] private float staminaHealWaitTime;
        [SerializeField] private float heatStroke;

        private float _recoveryEligibleSince = -1f;

        private float stamina;

        public void Initialize(Agent owner) {
            RestoreStamina(maxStamina);
            heatStroke = 0;
        }

        public Type Type => typeof(IStats);
        public event Action<float> OnStaminaChanged;
        public float StaminaRatio => maxStamina > 0f ? Mathf.Clamp01(Stamina / maxStamina) : 0f;

        public void HeatStrokeUpdate(int value, DamageModule damage) {
            heatStroke = Mathf.Clamp(heatStroke += value * Time.deltaTime, 0, 100);
            switch (heatStroke) {
                case >= 100:
                    damage.TakeDamage();
                    break;
                case > 50:
                    EffectManager.Instance.setFilter(true);
                    break;
                default:
                    EffectManager.Instance.setFilter(false);
                    break;
            }
        }

        public float Stamina {
            get => stamina;
            private set {
                if (Mathf.Approximately(stamina, value)) return;

                stamina = value;
                OnStaminaChanged?.Invoke(StaminaRatio);
            }
        }

        public void StaminaUpdate(bool isGrounded, bool isWalking, bool isClimb, bool isSprinting) {
            if (!isGrounded || isClimb || isSprinting) {
                _recoveryEligibleSince = -1f;
                return;
            }

            if (_recoveryEligibleSince < 0f)
                _recoveryEligibleSince = Time.time;
            if (Time.time - _recoveryEligibleSince < staminaHealWaitTime) return;

            var healRate = isWalking ? staminaHealSlow : staminaHealBoost;
            Stamina = Mathf.Clamp(Stamina + healRate * Time.deltaTime, 0, maxStamina);
        }

        public void UseStamina(float usedStamina, bool immediate) {
            if (Stamina == 0f) return;
            Stamina = !immediate
                ? Mathf.Clamp(Stamina - usedStamina * Time.deltaTime, 0, maxStamina)
                : Mathf.Clamp(Stamina - usedStamina, 0, maxStamina);
        }

        public void RestoreStamina(float savedStamina) {
            Stamina = Mathf.Clamp(savedStamina, 0f, maxStamina);
            _recoveryEligibleSince = -1f;
        }
    }
}