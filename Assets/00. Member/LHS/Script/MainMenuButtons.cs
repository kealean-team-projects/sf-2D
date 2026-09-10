using UnityEngine;

namespace _00._Member.LHS.Script {
    public class MainMenuButtons : MonoBehaviour {
        public void OnClickStartGame() {
            if (ChapterLoader.Instance == null) {
                Debug.LogError("ChapterLoader.Instance가 아직 준비되지 않았습니다.");
                return;
            }

            ChapterLoader.Instance.StartCoroutine(ChapterLoader.Instance.LoadChapter(1));
        }

        public void OnClickForC2() {
            if (ChapterLoader.Instance == null) {
                Debug.LogError("ChapterLoader.Instance가 아직 준비되지 않았습니다.");
                return;
            }

            ChapterLoader.Instance.StartCoroutine(ChapterLoader.Instance.LoadChapter(2));
        }
    }
}