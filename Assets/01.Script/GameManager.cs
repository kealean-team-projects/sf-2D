using UnityEngine;

namespace _01.Script
{
    using System.Collections;
    using UnityEngine;

    namespace _00_Scripts._07_Managers
    {
        public class GameManager : MonoBehaviour
        {
            public static GameManager Instance;

            public Transform player;
            public ScreenFade fader;
        
            private float defaultFixedDeltaTime;
        
            public bool IsRestarting { get; private set; }

            private void Awake()
            {
                if (Instance == null)
                {
                    Instance = this;
                }

                defaultFixedDeltaTime = Time.fixedDeltaTime;
            }

            public void SlowMotion(float slowTime)
            {
                Time.timeScale = slowTime;
                Time.fixedDeltaTime = defaultFixedDeltaTime * slowTime;
            }

            public void NormalTime()
            {
                Time.timeScale = 1;
                Time.fixedDeltaTime = defaultFixedDeltaTime;
            }

            public void Restart()
            {
                if (IsRestarting) return;
                StartCoroutine(RestartCoroutine());
            }

            private IEnumerator RestartCoroutine()
            {
                IsRestarting = true;
            
                yield return fader?.FadeOut();
            
                Time.timeScale = 0f;

                SaveManager.instance?.RestoreProgress();
            
                yield return new WaitForSecondsRealtime(2f);
            
                Time.timeScale = 1f;
                yield return fader?.FadeIn();
                IsRestarting = false;
            }
        }
    }
}