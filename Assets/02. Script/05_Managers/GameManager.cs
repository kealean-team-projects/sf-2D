using System;
using _02._Script._01_Players;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _02._Script._05_Managers {
    public class GameManager : MonoBehaviour {
        public static GameManager Instance;

        public Transform player;

        [SerializeField] private float respawnDelay = 0.2f;

        private float _defaultFixedDeltaTime;

        public bool IsRestarting { get; private set; }
        public static event Func<UniTask> OnRespawnReset;

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _defaultFixedDeltaTime = Time.fixedDeltaTime;
        }

        private void OnDestroy() {
            if (Instance == this) Instance = null;
        }

        public void SlowMotion(float slowTime) {
            Time.timeScale = slowTime;
            Time.fixedDeltaTime = _defaultFixedDeltaTime * slowTime;
        }

        public void NormalTime() {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = _defaultFixedDeltaTime;
        }

        public void Restart(Player target) {
            if (IsRestarting || target == null) return;

            IsRestarting = true;
            RestartCoroutine(target).Forget();
        }

        private async UniTaskVoid RestartCoroutine(Player target) {
            Time.timeScale = 0f;

            try {
                var effect = EffectManager.Instance;

                await effect.ShowDeathEffect();
                if (OnRespawnReset != null)
                    foreach (Func<UniTask> reset in OnRespawnReset.GetInvocationList())
                        await reset();
                target?.RestoreAfterDeath();

                // 화면을 걷기 전에 게임 시간을 재개
                NormalTime();
                // 위치 복원 후 잠깐 대기
                await UniTask.Delay(TimeSpan.FromSeconds(respawnDelay), true);
                await effect.HideDeathEffect();
            }
            finally {
                NormalTime();
                IsRestarting = false;
                target?.FinishRespawn();
            }
        }
    }
}
