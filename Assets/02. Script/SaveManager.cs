using System;
using _02._Script.Core;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
public class UserData {
    public float positionX;
    public float positionY;
    public float stamina;
}

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

        private void Update() {
            if(Keyboard.current.oKey.wasPressedThisFrame)
                SaveProgress();
        }

        public static event Action OnCaptureRequested;
        public static event Action OnRestoreRequested;

        public void SaveProgress() {
            // 게임을 껐다가 다시 시작할 때 이어지는 영구 저장은 별도 단계
            var data = new UserData() { positionX = transform.position.x, positionY = transform.position.y, stamina = 100 };
            SaveSystem.Save("Save.sf2d", data);
            OnCaptureRequested?.Invoke();
        }

        // 플레이 중 사망/체크포인트 복구용 런타임 이벤트 방식
        public void RestoreProgress() {
            OnRestoreRequested?.Invoke();
        }
    }
}