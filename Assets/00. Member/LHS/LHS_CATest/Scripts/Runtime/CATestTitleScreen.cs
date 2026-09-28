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
    ///   설정       : 설정 창
    ///   진행도 초기화 : 확인 후 이어하기 기록 + 엔딩 기록 모두 삭제
    ///   게임 종료
    /// ■ 엔딩 수집: 아래쪽에 ◆(본 엔딩) ◇(안 본 엔딩) 과 이름 표시
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
        [SerializeField] private Button resetBtn;
        [SerializeField] private Button quitBtn;
        [SerializeField] private Image[] endingMarks;
        [SerializeField] private Text[] endingNames;
        [SerializeField] private Text endingCount;
        [SerializeField] private Sprite markSeen, markUnseen;
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
            if (resetBtn != null) resetBtn.onClick.AddListener(OnReset);
            if (quitBtn != null) quitBtn.onClick.AddListener(() => confirm?.Ask("게임을 종료할까요?", "종료", CATestSceneFlow.QuitGame, () => Select(quitBtn)));
            if (settingsPanel != null) settingsPanel.Closed += () => { SetMenuInteractable(true); Select(settingsBtn); };
        }

        private void Start() {
            Refresh();
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
            var seen = 0;
            for (var i = 0; i < CATestSave.EndingCount; i++) {
                var s = CATestSave.EndingSeen(i);
                if (s) seen++;
                if (endingMarks != null && i < endingMarks.Length && endingMarks[i] != null) {
                    endingMarks[i].sprite = s ? markSeen : markUnseen;
                    endingMarks[i].color = s ? CATestUIKit.Accent : new Color(1f, 1f, 1f, 0.45f);
                }
                if (endingNames != null && i < endingNames.Length && endingNames[i] != null)
                    endingNames[i].text = s ? EndingTitles[i] : "???";
            }
            if (endingCount != null) endingCount.text = $"엔딩 기록  {seen} / {CATestSave.EndingCount}";
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

        private void OnReset() {
            if (confirm == null) return;
            confirm.Ask("진행도를 초기화할까요?\n<size=24><color=#b9b0cc>이어하기 기록과 엔딩 기록이 모두 지워집니다. (설정은 유지)</color></size>",
                "초기화", () => { CATestSave.ResetAll(); Refresh(); Select(newGameBtn); }, () => Select(resetBtn));
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
