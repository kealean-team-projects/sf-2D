using System;
using _02._Script._01_Players.Components.DamageCompo;
using _02._Script._01_Players.Interface;
using _02._Script._05_Managers;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _02._Script._01_Players.Components.Stamina {
    public class Stats : MonoBehaviour, IAgentModule, IStats {
        [SerializeField] private float staminaHealSlow;
        [SerializeField] private float staminaHealBoost;
        [SerializeField] private float maxStamina;
        [SerializeField] private float staminaHealWaitTime;
        [SerializeField] private float heatStroke;

        private bool _canCharge = true;
        private bool _uniTaskIsRunning;
        private int _chargeVersion;

        public void Initialize(Agent owner) {
            RestoreStamina(maxStamina);
            heatStroke = 0;
        }

        public Type Type => typeof(IStats);

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

        public float Stamina { get; private set; }

        public void StaminaUpdate(bool isGrounded, bool isWalking, bool isClimb) {
            if (!_canCharge) {
                if (!_uniTaskIsRunning) WaitCharge().Forget();
                return;
            }

            if (!isGrounded) return;
            if (isClimb) return;
            var healRate = isWalking ? staminaHealSlow : staminaHealBoost;
            Stamina = Mathf.Clamp(Stamina + healRate * Time.deltaTime, 0, maxStamina);
        }

        public void UseStamina(float usedStamina, bool immediate) {
            if (Stamina == 0f) return;
            _canCharge = false;
            Stamina = !immediate
                ? Mathf.Clamp(Stamina - usedStamina * Time.deltaTime, 0, maxStamina)
                : Mathf.Clamp(Stamina - usedStamina, 0, maxStamina);
        }

        public void RestoreStamina(float savedStamina) {
            Stamina = Mathf.Clamp(savedStamina, 0f, maxStamina);
            // A pending delay belongs to the previous life, not the restored state.
            _chargeVersion++;
            _canCharge = true;
            _uniTaskIsRunning = false;
        }

        private async UniTask WaitCharge() {
            _uniTaskIsRunning = true;
            int version = _chargeVersion;
            await UniTask.Delay(TimeSpan.FromSeconds(staminaHealWaitTime));
            if (version != _chargeVersion) return;
            _canCharge = true;
            _uniTaskIsRunning = false;
        }
    }
}
