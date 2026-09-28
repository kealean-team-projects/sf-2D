using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LHS_CATest {
    public enum CATestStartMode {
        /// <summary>CoreScene 을 에디터에서 바로 Play: CATestBootstrap 의 startIndex(테스트 시작 지점) 사용</summary>
        Default,
        /// <summary>타이틀 [새로 시작]: 오프닝 컷신부터</summary>
        NewGame,
        /// <summary>타이틀 [이어하기]: Save.sf2d 의 마지막 세이브 지점부터</summary>
        Continue,
    }

    /// <summary>
    /// 타이틀 ↔ 게임(CoreScene) 전환.
    ///
    /// ■ 씬 로드 방법
    ///   - 에디터: EditorSceneManager.LoadSceneAsyncInPlayMode(경로) → Build Profiles 씬 목록에 없어도 로드됨
    ///   - 빌드: SceneManager.LoadSceneAsync(이름) → 빌드 씬 목록에 CATest_Title, CATest_CoreScene, 맵 씬들을 넣어야 함
    /// ■ 넘겨주는 값: 정적 변수 Mode (씬이 바뀌어도 static 은 유지된다) → CoreScene 의 CATestBootstrap 이 읽고 비운다.
    /// ■ 정리: 시간 정지(Time.timeScale)·컷신 잠금·페이드 색을 원래대로 돌린 뒤 넘어간다.
    /// </summary>
    public static class CATestSceneFlow {
        public const string TitleScenePath = "Assets/00. Member/LHS/LHS_CATest/Scenes/CATest_Title.unity";
        public const string CoreScenePath = "Assets/00. Member/LHS/LHS_CATest/Scenes/CATest_CoreScene.unity";

        public static CATestStartMode Mode { get; private set; } = CATestStartMode.Default;
        public static bool IsLoading { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() {
            Mode = CATestStartMode.Default;
            IsLoading = false;
        }

        /// <summary>CATestBootstrap 이 한 번 읽고 기본값으로 되돌린다.</summary>
        public static CATestStartMode ConsumeMode() {
            var m = Mode;
            Mode = CATestStartMode.Default;
            return m;
        }

        public static void StartGame(CATestStartMode mode) {
            Mode = mode;
            Load(CoreScenePath).Forget();
        }

        public static void GoToTitle() => Load(TitleScenePath).Forget();

        private static async UniTaskVoid Load(string path) {
            if (IsLoading) return;
            IsLoading = true;
            try {
                Time.timeScale = 1f;
                CATestCutscene.ResetState();
                if (path == TitleScenePath) {
                    // 원본 UIManager 는 DontDestroyOnLoad 라 씬을 바꿔도 남는다. 그런데 그것이 참조하는 기력 HUD 등은
                    // CoreScene 에 있어서 사라지므로, 다시 게임을 시작할 때 끊긴 참조로 오류가 날 수 있다.
                    // → 타이틀로 갈 때 지워 두고, 게임을 다시 시작하면 CoreScene 이 새로 만든다.
                    var ui = _02._Script._05_Managers.UIManager.Instance;
                    if (ui != null) Object.Destroy(ui.gameObject);
                }
                AsyncOperation op;
#if UNITY_EDITOR
                op = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
#else
                op = SceneManager.LoadSceneAsync(System.IO.Path.GetFileNameWithoutExtension(path), LoadSceneMode.Single);
#endif
                if (op != null) await op;
                else Debug.LogError($"[CATest] 씬을 불러오지 못했습니다: {path} (빌드라면 Build Profiles 씬 목록 확인)");
            }
            finally {
                IsLoading = false;
            }
        }

        public static void QuitGame() {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
