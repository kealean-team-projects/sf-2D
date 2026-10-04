using _02._Script._05_Managers;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LHS_CATest {
    /// <summary>
    /// 인게임 ESC 메뉴 (UGUI). CATest CoreScene 에 하나.
    ///
    /// ■ 열기/닫기: ESC. 열리면 Time.timeScale = 0(게임 정지) + 플레이어 입력 잠금, 닫으면 원래대로.
    /// ■ 메뉴: 계속하기 / 설정 / 타이틀로 / 게임 종료
    ///   - 설정: 타이틀과 같은 설정 창(CATestSettingsPanel). 머리 위 대사 켜기/끄기 포함
    ///   - 타이틀로: 확인 창 → 저장된 마지막 세이브 지점은 그대로 두고 타이틀 씬으로
    /// ■ 열 수 없는 때: 사망 연출 중(시간 정지 중), 엔딩 연출 중(CATestEndingDirector.IsRunning), 씬 전환 중
    /// ■ 원본 UIManager 도 ESC 로 자기 설정창(settingsPanel)을 여는데, CATest 에선 이 메뉴가 대신하므로
    ///   LateUpdate 에서 그 창을 매 프레임 닫아 둔다(같은 프레임 안이라 깜빡이지 않는다).
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public sealed class CATestPauseMenu : MonoBehaviour {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Button resume;
        [SerializeField] private Button settings;
        [SerializeField] private Button toTitle;
        [SerializeField] private Button quit;
        [SerializeField] private Text placeText;
        [SerializeField] private CATestSettingsPanel settingsPanel;
        [SerializeField] private CATestConfirmDialog confirm;

        public static bool IsPaused { get; private set; }
        private float _prevTimeScale = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => IsPaused = false;

        private void Awake() {
            ReplaceSettingsPanel();
            CATestUIKit.Show(group, false);
            if (resume != null) resume.onClick.AddListener(Close);
            if (settings != null) settings.onClick.AddListener(OpenSettings);
            if (toTitle != null) toTitle.onClick.AddListener(AskTitle);
            if (quit != null) quit.onClick.AddListener(AskQuit);
            if (settingsPanel != null) settingsPanel.Closed += () => { if (IsPaused) SelectFirst(settings); };
        }

        /// <summary>
        /// 새 설정 창(배경음/효과음 볼륨, 엔딩 기록 포함)을 Resources 프리팹에서 꺼내 기존 설정 창과 바꿔 끼운다.
        /// CoreScene 파일을 다시 빌드하지 않고도 UI 를 갱신하기 위함. 프리팹이 없으면 기존 창을 그대로 쓴다.
        /// </summary>
        private void ReplaceSettingsPanel() {
            if (settingsPanel == null) return;
            var prefab = Resources.Load<CATestSettingsPanel>("CATestUI/SettingsPanel_Game");
            if (prefab == null) return;
            var old = settingsPanel;
            var parent = old.transform.parent;
            var index = old.transform.GetSiblingIndex();
            var fresh = Instantiate(prefab, parent, false);
            fresh.name = "SettingsPanel";
            fresh.transform.SetSiblingIndex(index);
            settingsPanel = fresh;
            Destroy(old.gameObject);
        }

        private void OnDestroy() {
            if (IsPaused) Time.timeScale = 1f;
            IsPaused = false;
        }

        private bool CanOpen {
            get {
                if (CATestSceneFlow.IsLoading || CATestEndingDirector.IsRunning) return false;
                var p = CATestHUD.CurrentPlayer;
                if (p == null || !p.isActiveAndEnabled || p.IsDead) return false;
                if (GameManager.Instance != null && GameManager.Instance.IsRestarting) return false;
                return true;
            }
        }

        private void Update() {
            if (!CATestUIInput.ConsumeEscape()) return;
            if (IsPaused) Close();
            else if (CanOpen) Open();
        }

        private void LateUpdate() {
            // 원본 UIManager 의 ESC 설정창은 CATest 에서 쓰지 않음
            if (UIManager.Instance != null) UIManager.Instance.CloseSettings();
        }

        public void Open() {
            if (IsPaused) return;
            IsPaused = true;
            _prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            CATestCutscene.LockInput(true);
            CATestAudio.PlayUi("ui_pause");
            if (placeText != null) placeText.text = string.IsNullOrEmpty(CATestHUD.LastTitle) ? "" : "현재 위치 · " + CATestHUD.LastTitle;
            CATestUIKit.Show(group, true);
            FadeIn().Forget();
            SelectFirst(resume);
        }

        public void Close() {
            if (!IsPaused) return;
            if (settingsPanel != null && settingsPanel.IsOpen) settingsPanel.Close();
            if (confirm != null && confirm.IsOpen) confirm.Hide();
            IsPaused = false;
            Time.timeScale = _prevTimeScale <= 0f ? 1f : _prevTimeScale;
            CATestCutscene.LockInput(false);
            CATestUIKit.Show(group, false);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        private void OpenSettings() {
            if (settingsPanel != null) settingsPanel.Open();
        }

        private void AskTitle() {
            if (confirm == null) { GoTitle(); return; }
            confirm.Ask("타이틀 화면으로 돌아갈까요?\n<size=24><color=#b9b0cc>마지막 세이브 지점부터 [이어하기]로 다시 시작할 수 있습니다.</color></size>",
                "돌아가기", GoTitle, () => SelectFirst(toTitle));
        }

        private void AskQuit() {
            if (confirm == null) { CATestSceneFlow.QuitGame(); return; }
            confirm.Ask("게임을 종료할까요?\n<size=24><color=#b9b0cc>마지막 세이브 지점까지 저장되어 있습니다.</color></size>",
                "종료", CATestSceneFlow.QuitGame, () => SelectFirst(quit));
        }

        private void GoTitle() {
            IsPaused = false;
            CATestSceneFlow.GoToTitle();
        }

        private static void SelectFirst(Button b) {
            if (b != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(b.gameObject);
        }

        private async UniTaskVoid FadeIn() {
            if (group == null) return;
            for (var t = 0f; t < 0.2f; t += Time.unscaledDeltaTime) {
                if (!IsPaused || group == null) return;
                group.alpha = t / 0.2f;
                await UniTask.Yield();
            }
            if (IsPaused && group != null) group.alpha = 1f;
        }
    }
}
