using System;
using UnityEngine;

namespace _02._Script {
    public class SaveManager : MonoBehaviour {
        public static SaveManager Instance;

        private void Awake() {
            if (Instance == null) {
                Instance = this;

                DontDestroyOnLoad(gameObject);
            }

            else {
                Destroy(gameObject);
            }
        }

        public static event Action<int> OnCaptureRequested;
        public static event Action OnRestoreRequested;

        public void SaveProgress(int num) {
            // 게임을 껐다가 다시 시작할 때 이어지는 영구 저장은 별도 단계
            PlayerPrefs.SetInt("ProgressData", num);
            OnCaptureRequested?.Invoke(num);
        }

        // 플레이 중 사망/체크포인트 복구용 런타임 이벤트 방식
        public void RestoreProgress() {
            OnRestoreRequested?.Invoke();
        }
    }
}