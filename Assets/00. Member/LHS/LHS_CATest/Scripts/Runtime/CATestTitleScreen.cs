using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LHS_CATest {
    /// <summary>
    /// CATest 메인 타이틀 (UGUI, CATest_Title 씬).
    ///
    /// ■ 메뉴
    ///   이어하기   : Save.sf2d(원본 저장 파일)가 있을 때만 활성. 마지막 세이브 지점 이름을 작게 표시.
    ///                → CoreScene 을 Continue 모드로 불러 그 위치에서 시작 (엔딩 뒤에는 "용의 둥지 앞" 세이브 지점)
    ///   새로 시작   : 기록이 있으면 확인 후 이어하기 기록만 지우고(엔딩 기록은 유지) 오프닝 컷신부터
    ///   설정       : 설정 창 — 엔딩 기록(작게)과 오른쪽 아래 [데이터 초기화](이어하기 + 엔딩 기록 삭제)도 여기에 있음
    ///   게임 종료
    /// ■ 시작 연출: 검은 화면에서 서서히 밝아지며 제목 → 메뉴 순으로 나타남
    /// </summary>
    public sealed class CATestTitleScreen : MonoBehaviour {
        [SerializeField] private CanvasGroup titleGroup;
        [SerializeField] private CanvasGroup menuGroup;
        [SerializeField] private Image blackCover;
        [SerializeField] private Button continueBtn;
        [SerializeField] private Text continueSub;
        [SerializeField] private Button newGameBtn;
        [SerializeField] private Button settingsBtn;
        [SerializeField] private Button quitBtn;
        [SerializeField] private CATestSettingsPanel settingsPanel;
        [SerializeField] private CATestConfirmDialog confirm;

        public static readonly string[] EndingTitles = { "귀환", "황혼의 이방인", "신에게 맞선 자" };

        private bool _busy;

        private void Awake() {
            Time.timeScale = 1f;
            HideLeftoverGameUI();
            if (continueBtn != null) continueBtn.onClick.AddListener(OnContinue);
            if (newGameBtn != null) newGameBtn.onClick.AddListener(OnNewGame);
            if (settingsBtn != null) settingsBtn.onClick.AddListener(() => { settingsPanel?.Open(); SetMenuInteractable(false); });
            if (quitBtn != null) quitBtn.onClick.AddListener(() => confirm?.Ask("게임을 종료할까요?", "종료", CATestSceneFlow.QuitGame, () => Select(quitBtn)));
            if (settingsPanel != null) {
                settingsPanel.Closed += () => { SetMenuInteractable(true); Select(settingsBtn); };
                settingsPanel.DataReset += Refresh; // 데이터 초기화 → 이어하기 버튼 비활성 등 다시 그림
            }
        }

        private void Start() {
            Refresh();
            CATestAudio.PlayBgmNow("title", "amb_twilight"); // 소리 칸이 비어 있으면 아무 소리도 나지 않음
            Intro().Forget();
        }

        /// <summary>게임에서 타이틀로 돌아왔을 때 DontDestroyOnLoad 로 남아 있는 원본 기력 HUD 등을 숨긴다.</summary>
        private static void HideLeftoverGameUI() {
            foreach (var hud in FindObjectsByType<_02._Script.UI.StaminaHUD>(FindObjectsInactive.Exclude))
                hud.gameObject.SetActive(false);
        }

        private void Refresh() {
            var has = CATestSave.HasContinue;
            if (continueBtn != null) continueBtn.interactable = has;
            if (continueSub != null) {
                var place = CATestSave.LastPlaceName;
                continueSub.text = has ? (string.IsNullOrEmpty(place) ? "마지막 세이브 지점" : place) : "저장된 기록 없음";
            }
        }

        private async UniTaskVoid Intro() {
            CATestUIKit.Show(menuGroup, false);
            if (titleGroup != null) titleGroup.alpha = 0f;
            if (blackCover != null) blackCover.color = Color.black;
            for (var t = 0f; t < 1.6f; t += Time.unscaledDeltaTime) {
                if (blackCover != null) blackCover.color = new Color(0f, 0f, 0f, 1f - t / 1.6f);
                if (titleGroup != null) titleGroup.alpha = Mathf.Clamp01((t - 0.5f) / 1.1f);
                await UniTask.Yield();
            }
            if (this == null) return;
            if (blackCover != null) blackCover.color = new Color(0f, 0f, 0f, 0f);
            if (titleGroup != null) titleGroup.alpha = 1f;
            CATestUIKit.Show(menuGroup, true);
            for (var t = 0f; t < 0.6f; t += Time.unscaledDeltaTime) {
                if (menuGroup != null) menuGroup.alpha = t / 0.6f;
                await UniTask.Yield();
            }
            if (this == null) return;
            if (menuGroup != null) menuGroup.alpha = 1f;
            Select(continueBtn != null && continueBtn.interactable ? continueBtn : newGameBtn);
        }

        private void OnContinue() {
            if (_busy || !CATestSave.HasContinue) return;
            Leave(CATestStartMode.Continue).Forget();
        }

        private void OnNewGame() {
            if (_busy) return;
            if (CATestSave.HasContinue && confirm != null) {
                confirm.Ask("새로 시작할까요?\n<size=24><color=#b9b0cc>현재 이어하기 기록은 사라집니다. (엔딩 기록은 유지)</color></size>",
                    "새로 시작", () => { CATestSave.DeleteContinue(); Leave(CATestStartMode.NewGame).Forget(); }, () => Select(newGameBtn));
                return;
            }
            CATestSave.DeleteContinue();
            Leave(CATestStartMode.NewGame).Forget();
        }

        private async UniTaskVoid Leave(CATestStartMode mode) {
            _busy = true;
            SetMenuInteractable(false);
            for (var t = 0f; t < 0.9f; t += Time.unscaledDeltaTime) {
                if (blackCover != null) blackCover.color = new Color(0f, 0f, 0f, t / 0.9f);
                await UniTask.Yield();
            }
            CATestSceneFlow.StartGame(mode);
        }

        private void SetMenuInteractable(bool on) {
            if (menuGroup == null) return;
            menuGroup.interactable = on;
            menuGroup.blocksRaycasts = on;
        }

        private static void Select(Button b) {
            if (b != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(b.gameObject);
        }
    }
}
