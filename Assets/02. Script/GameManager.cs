using System.Collections;
using UnityEngine;

namespace _02._Script {
    namespace _00_Scripts._07_Managers {
        public class GameManager : MonoBehaviour {
            public static GameManager Instance;

            public Transform player;
            public ScreenFade fader;

            private float _defaultFixedDeltaTime;

            public bool IsRestarting { get; private set; }

            private void Awake() {
                if (Instance == null) Instance = this;

                _defaultFixedDeltaTime = Time.fixedDeltaTime;
            }

            public void SlowMotion(float slowTime) {
                Time.timeScale = slowTime;
                Time.fixedDeltaTime = _defaultFixedDeltaTime * slowTime;
            }

            public void NormalTime() {
                Time.timeScale = 1;
                Time.fixedDeltaTime = _defaultFixedDeltaTime;
            }

            public void Restart() {
                if (IsRestarting) return;
                StartCoroutine(RestartCoroutine());
            }

            private IEnumerator RestartCoroutine() {
                IsRestarting = true;

                yield return fader?.FadeOut();

                Time.timeScale = 0f;

                SaveManager.Instance?.RestoreProgress();

                yield return new WaitForSecondsRealtime(2f);

                Time.timeScale = 1f;
                yield return fader?.FadeIn();
                IsRestarting = false;
            }
        }
    }
}