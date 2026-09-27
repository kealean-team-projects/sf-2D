using System;
using _02._Script._05_Managers;
using UnityEngine;
using UnityEngine.UI;

namespace _00._Member.LHS.Script {
    public class MainMenuButtons : MonoBehaviour
    {
        [SerializeField] private Button startBtn;
        [SerializeField] private Button settingBtn;
        [SerializeField] private Button exitBtn;

        private void Start()
        {
            startBtn.onClick.AddListener(OnClickStartGame);
            settingBtn.onClick.AddListener(OnClickSettings);
            exitBtn.onClick.AddListener(OnExitGame);
        }

        public void OnClickStartGame() {
            if (ChapterLoader.Instance == null) {
                Debug.LogError("ChapterLoader.Instance가 아직 준비되지 않았습니다.");
                return;
            }

            ChapterLoader.Instance.LoadChapter(1).Forget();
        }

        public void OnClickForC2() {
            if (ChapterLoader.Instance == null) {
                Debug.LogError("ChapterLoader.Instance가 아직 준비되지 않았습니다.");
                return;
            }

            ChapterLoader.Instance.LoadChapter(2).Forget();
        }
        
        public void OnClickSettings() => UIManager.Instance.OpenSettings();
        public void OnExitGame() => Application.Quit();
    }
}