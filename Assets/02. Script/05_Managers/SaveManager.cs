using System;
using _02._Script._02_Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _02._Script._05_Managers
{
    [Serializable]
    public class UserData {
        public int stage;
        public float positionX;
        public float positionY;
        public float stamina;
    }

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
                OnCaptureRequested?.Invoke();
            if(Keyboard.current.oKey.wasPressedThisFrame)
                RestoreProgress();
        }

        public static event Action OnCaptureRequested;
        public static event Action<UserData> OnRestoreRequested;

        public void SaveProgress(Transform trm, float stamina, int stage) {
            UserData data = new UserData();

            data.stage = stage;
            data.positionX = trm.position.x;
            data.positionY = trm.position.y;
            data.stamina = stamina;

            SaveSystem.Save("Save.sf2d", data);
        }

        // 플레이 중 사망/체크포인트 복구용 런타임 이벤트 방식
        public void RestoreProgress() {
            var data = SaveSystem.Load<UserData>("Save.sf2d");
            if (data == null)
            {
                Debug.LogWarning("Save.sf2d not found");
                return;
            }
            OnRestoreRequested?.Invoke(data);
        }
    }
}