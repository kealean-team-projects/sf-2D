using _02._Script._05_Managers;
using UnityEngine;

namespace _02._Script._01_Players {
    public class PlayerProgress : MonoBehaviour {
        [SerializeField] private int currentStage = 1;

        private Player _player;
        private UserData _respawnData;
        private bool _initialized;

        public void Initialize(Player player) {
            if (_initialized) return;

            _player = player;
            _respawnData = CreateSaveData();
            _initialized = true;

            if (isActiveAndEnabled) {
                Subscribe();
            }
        }

        private void OnEnable() {
            if (_initialized) {
                Subscribe();
            }
        }

        private void OnDisable() {
            Unsubscribe();
        }

        public void Shutdown() {
            Unsubscribe();
            _initialized = false;
        }

        private void Subscribe() {
            SaveManager.OnCaptureRequested += CaptureProgress;
            SaveManager.OnRestoreRequested += RestoreProgress;
        }

        private void Unsubscribe() {
            SaveManager.OnCaptureRequested -= CaptureProgress;
            SaveManager.OnRestoreRequested -= RestoreProgress;
        }

        private UserData CreateSaveData() {
            UserData data = new UserData();

            data.stage = currentStage;
            data.positionX = _player.transform.position.x;
            data.positionY = _player.transform.position.y;
            data.stamina = _player.CurrentStamina;

            return data;
        }

        private void CaptureProgress()
        {
            if (_player.IsDead) return;

            _respawnData = CreateSaveData();

            SaveManager.Instance?.SaveProgress(_player.transform, _player.CurrentStamina, currentStage);
        }

        private void RestoreProgress(UserData data) {
            if (data == null || _player.IsDead) return;

            if (data.stage != currentStage) {
                Debug.LogWarning("현재 스테이지와 다른 저장 데이터입니다.");
                return;
            }

            ApplyProgress(data);
            _respawnData = data;
        }

        public void RestoreAfterDeath() {
            ApplyProgress(_respawnData);
        }

        private void ApplyProgress(UserData data) {
            Vector2 position =
                new Vector2(data.positionX, data.positionY);

            _player.RestoreState(position, data.stamina);
        }
    }
}