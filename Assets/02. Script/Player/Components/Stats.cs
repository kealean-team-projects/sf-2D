using System;
using System.Threading;
using _02._Script.Player.Interface;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _02._Script.Player.Components {
    public class Stats : MonoBehaviour, IAgentModule, IStats {
        [SerializeField] private float staminaHealSlow;
        [SerializeField] private float staminaHealBoost;
        [SerializeField] private float maxStamina;
        [SerializeField] private float staminaHealWaitTime;

        private bool _canCharge = true;
        private UniTask _task;
        private CancellationTokenSource _cancellationTokenSource = new();
        
        public void Initialize(Agent owner) {
            Stamina = maxStamina;
        }

        public Type Type => typeof(IStats);

        public float Stamina { get; private set; }

        public void StaminaUpdate(bool isGrounded, bool isWalking, bool isClimb) {
            if (!_canCharge) {
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
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            
            _cancellationTokenSource = new CancellationTokenSource();
            
            WaitCharge().Forget();
        }

        private async UniTaskVoid WaitCharge() {
            await UniTask.Delay(TimeSpan.FromSeconds(staminaHealWaitTime), cancellationToken: _cancellationTokenSource.Token);
            _canCharge = true;
        }
    }
}