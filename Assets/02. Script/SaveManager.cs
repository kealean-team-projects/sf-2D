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
            if(Keyboard.current.iKey.wasPressedThisFrame)
                SaveProgress(transform, 100f);
            if(Keyboard.current.oKey.wasPressedThisFrame)
                SaveProgress(transform, 100f);
        }

        public static event Action OnCaptureRequested;
        public static event Action OnRestoreRequested;

        public void SaveProgress(Transform trm, float stamina) {
            // 게임을 껐다가 다시 시작할 때 이어지는 영구 저장은 별도 단계
            var data = new UserData { positionX = trm.position.x, positionY = trm.position.y, stamina = stamina };
            SaveSystem.Save("Save.sf2d", data);
            OnCaptureRequested?.Invoke();
        }

        // 플레이 중 사망/체크포인트 복구용 런타임 이벤트 방식
        public void RestoreProgress() {
            SaveSystem.Load<UserData>("Save.sf2d");
            OnRestoreRequested?.Invoke();
        }
    }
}