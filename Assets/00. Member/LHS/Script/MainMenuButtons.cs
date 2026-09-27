using System;
using _02._Script._05_Managers;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

namespace _00._Member.LHS.Script {
    public class MainMenuButtons : MonoBehaviour
    {
        [SerializeField] private Button startBtn;
        [SerializeField] private Button settingBtn;
        [SerializeField] private Button exitBtn;
        [SerializeField] private CanvasGroup menuGroup;

        private IEnumerator Start()
        {
            // UI layout and Button.onClick bindings are authored in MainMenu.
            // Allow Play directly in MainMenu without duplicating it when CoreScene starts.
            if (ChapterLoader.Instance == null) {
                const string coreScene = "Assets/00. Member/LHS/Scene/CoreScene.unity";
                startBtn.interactable = settingBtn.interactable = false;
                if (Application.CanStreamedLevelBeLoaded(coreScene))
                    yield return SceneManager.LoadSceneAsync(coreScene, LoadSceneMode.Additive);
                else
                    Debug.LogError("CoreScene을 Build Profiles의 Scene List에 등록하세요.", this);
            }
            startBtn.interactable = ChapterLoader.Instance != null;
            settingBtn.interactable = UIManager.Instance != null;
        }

        public void OnClickStartGame() {
            if (ChapterLoader.Instance == null) {
                Debug.LogError("ChapterLoader.Instance가 아직 준비되지 않았습니다.");
                return;
            }

            UIManager.Instance?.CloseMainMenuSettings();
            if (ChapterLoader.Instance.TryLoadChapter(1)) startBtn.interactable = false;
        }

        public void OnClickForC2() {
            if (ChapterLoader.Instance == null) {
                Debug.LogError("ChapterLoader.Instance가 아직 준비되지 않았습니다.");
                return;
            }

            ChapterLoader.Instance.LoadChapter(2).Forget();
        }
        
        public void OnClickSettings() => UIManager.Instance.OpenMainMenuSettings(menuGroup);
        public void OnExitGame() {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
