using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LHS_CATest {
    /// <summary>
    /// 설정 창 (UGUI). 타이틀 화면과 인게임 ESC 메뉴가 같은 구성의 창을 하나씩 가진다.
    /// UI 오브젝트는 빌더(CATestBuilder.UI)가 만들고, 이 컴포넌트는 값 연결만 한다.
    ///
    /// ■ 항목
    ///  - 전체 볼륨 / 배경음 / 효과음 / 화면 밝기 슬라이더
    ///  - 해상도 ◀ ▶, 전체 화면 · 머리 위 대사 표시 · 화면 흔들림 켜짐/꺼짐
    ///  - 아래쪽: 엔딩 기록(작게) + [기본값] [닫기]
    ///  - 오른쪽 아래 작은 [데이터 초기화] — 타이틀 화면의 설정 창에서만 보임(게임 도중엔 저장 파일을 지우지 않도록)
    /// ■ 동작
    ///  - 값을 바꾸는 즉시 CATestSettings 에 반영(미리보기 겸 적용) → [닫기] 때 PlayerPrefs 에 저장
    ///  - ESC(또는 [닫기]) → 창 닫고 Closed 이벤트 → 부른 쪽 메뉴가 다시 선택 상태로
    ///  - 데이터 초기화 → 확인 창 → 이어하기 기록 + 엔딩 기록 삭제(설정 값은 유지) → DataReset 이벤트
    /// </summary>
    [DefaultExecutionOrder(-60)] // 일시정지 메뉴보다 먼저 ESC 를 받는다
    public sealed class CATestSettingsPanel : MonoBehaviour {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Slider volume;
        [SerializeField] private Text volumeValue;
        [SerializeField] private Slider bgm;
        [SerializeField] private Text bgmValue;
        [SerializeField] private Slider sfx;
        [SerializeField] private Text sfxValue;
        [SerializeField] private Slider brightness;
        [SerializeField] private Text brightnessValue;
        [SerializeField] private Button resPrev, resNext;
        [SerializeField] private Text resText;
        [SerializeField] private Button fullscreen;
        [SerializeField] private Text fullscreenText;
        [SerializeField] private Button playerLines;
        [SerializeField] private Text playerLinesText;
        [SerializeField] private Button shake;
        [SerializeField] private Text shakeText;
        [SerializeField] private Button defaults;
        [SerializeField] private Button close;

        [Header("엔딩 기록 / 데이터 초기화")]
        [SerializeField] private Text endingCount;
        [SerializeField] private Image[] endingMarks;
        [SerializeField] private Text[] endingNames;
        [SerializeField] private Sprite markSeen, markUnseen;
        [SerializeField] private Button dataReset;
        [SerializeField] private bool allowDataReset;
        [SerializeField] private CATestConfirmDialog confirm;

        public bool IsOpen { get; private set; }
        public event Action Closed;
        public event Action DataReset;

        private readonly List<Vector2Int> _resolutions = new();
        private int _resIndex;
        private bool _refreshing;

        private void Awake() {
            CATestSettings.EnsureLoaded();
            CATestUIKit.Show(group, false);
            if (volume != null) volume.onValueChanged.AddListener(v => { if (!_refreshing) CATestSettings.SetVolume(v); });
            if (bgm != null) bgm.onValueChanged.AddListener(v => { if (!_refreshing) CATestSettings.SetBgmVolume(v); });
            if (sfx != null) sfx.onValueChanged.AddListener(v => {
                if (_refreshing) return;
                CATestSettings.SetSfxVolume(v);
                CATestAudio.PlayUi("ui_move"); // 효과음 크기를 바로 들어 볼 수 있게
            });
            if (brightness != null) {
                brightness.minValue = -1f; brightness.maxValue = 1f;
                brightness.onValueChanged.AddListener(v => { if (!_refreshing) CATestSettings.SetBrightness(v); });
            }
            if (resPrev != null) resPrev.onClick.AddListener(() => StepResolution(-1));
            if (resNext != null) resNext.onClick.AddListener(() => StepResolution(1));
            if (fullscreen != null) fullscreen.onClick.AddListener(() => { CATestSettings.SetFullscreen(!CATestSettings.Fullscreen); CATestAudio.PlayUi("ui_toggle"); });
            if (playerLines != null) playerLines.onClick.AddListener(() => { CATestSettings.SetShowPlayerLines(!CATestSettings.ShowPlayerLines); CATestAudio.PlayUi("ui_toggle"); });
            if (shake != null) shake.onClick.AddListener(() => { CATestSettings.SetScreenShake(!CATestSettings.ScreenShake); CATestAudio.PlayUi("ui_toggle"); });
            if (defaults != null) defaults.onClick.AddListener(() => { CATestSettings.ResetToDefault(); CATestAudio.PlayUi("ui_select"); });
            if (close != null) close.onClick.AddListener(Close);
            if (dataReset != null) {
                dataReset.gameObject.SetActive(allowDataReset);
                dataReset.onClick.AddListener(AskDataReset);
            }
            BuildResolutionList();
        }

        private void OnEnable() => CATestSettings.Changed += Refresh;
        private void OnDisable() => CATestSettings.Changed -= Refresh;

        private void BuildResolutionList() {
            _resolutions.Clear();
            foreach (var r in Screen.resolutions) {
                var v = new Vector2Int(r.width, r.height);
                if (v.x >= 1024 && !_resolutions.Contains(v)) _resolutions.Add(v);
            }
            if (_resolutions.Count == 0) _resolutions.Add(new Vector2Int(Screen.width, Screen.height));
            var cur = CATestSettings.Resolution;
            _resIndex = Mathf.Max(0, _resolutions.IndexOf(cur));
            if (_resolutions.IndexOf(cur) < 0) _resIndex = _resolutions.Count - 1;
        }

        private void StepResolution(int d) {
            if (_resolutions.Count == 0) return;
            _resIndex = (_resIndex + d + _resolutions.Count) % _resolutions.Count;
            CATestSettings.SetResolution(_resolutions[_resIndex]);
            CATestAudio.PlayUi("ui_toggle");
        }

        public void Open() {
            IsOpen = true;
            Refresh();
            RefreshEndings();
            CATestUIKit.Show(group, true);
            FadeIn().Forget();
            if (EventSystem.current != null && volume != null) EventSystem.current.SetSelectedGameObject(volume.gameObject);
        }

        public void Close() {
            if (!IsOpen) return;
            IsOpen = false;
            CATestSettings.Save();
            CATestUIKit.Show(group, false);
            CATestAudio.PlayUi("ui_back");
            Closed?.Invoke();
        }

        private void AskDataReset() {
            if (confirm == null) return;
            CATestAudio.PlayUi("ui_select");
            // 확인 창이 떠 있는 동안 설정 창 버튼은 잠금(방향키가 뒤의 창으로 새지 않게)
            if (group != null) group.interactable = false;
            confirm.Ask("데이터를 초기화할까요?\n<size=24><color=#b9b0cc>이어하기 기록과 엔딩 기록이 모두 지워집니다. (설정은 유지)</color></size>",
                "초기화", () => {
                    CATestSave.ResetAll();
                    RefreshEndings();
                    DataReset?.Invoke();
                }, () => {
                    if (group != null) group.interactable = IsOpen;
                    if (IsOpen && EventSystem.current != null && dataReset != null) EventSystem.current.SetSelectedGameObject(dataReset.gameObject);
                });
        }

        private async UniTaskVoid FadeIn() {
            if (group == null) return;
            for (var t = 0f; t < 0.18f; t += Time.unscaledDeltaTime) {
                if (!IsOpen || group == null) return;
                group.alpha = t / 0.18f;
                await UniTask.Yield();
            }
            if (IsOpen && group != null) group.alpha = 1f;
        }

        private void Update() {
            if (!IsOpen) return;
            if (confirm != null && confirm.IsOpen) return; // 확인 창이 ESC 를 먼저 처리
            if (CATestUIInput.ConsumeEscape()) Close();
        }

        // 켜짐 = 주황 글자, 꺼짐 = 흐린 글자 → 상태를 색으로도 구분
        private static void OnOff(Text t, bool on) {
            if (t == null) return;
            t.text = on ? "켜짐" : "꺼짐";
            t.color = on ? CATestUIKit.Accent : CATestUIKit.DimText;
        }

        private static void Percent(Slider s, Text t, float v) {
            if (s != null) s.SetValueWithoutNotify(v);
            if (t != null) t.text = Mathf.RoundToInt(v * 100f) + "%";
        }

        private void Refresh() {
            _refreshing = true;
            Percent(volume, volumeValue, CATestSettings.MasterVolume);
            Percent(bgm, bgmValue, CATestSettings.BgmVolume);
            Percent(sfx, sfxValue, CATestSettings.SfxVolume);
            if (brightness != null) brightness.SetValueWithoutNotify(CATestSettings.Brightness);
            if (brightnessValue != null) {
                var b = Mathf.RoundToInt(CATestSettings.Brightness * 100f);
                brightnessValue.text = b == 0 ? "기본" : (b > 0 ? "+" + b : b.ToString());
            }
            var r = CATestSettings.Resolution;
            if (resText != null) resText.text = $"{r.x} × {r.y}";
            OnOff(fullscreenText, CATestSettings.Fullscreen);
            OnOff(playerLinesText, CATestSettings.ShowPlayerLines);
            OnOff(shakeText, CATestSettings.ScreenShake);
            _refreshing = false;
        }

        /// <summary>엔딩 기록: ◆(본 엔딩, 이름 표시) ○(안 본 엔딩, ???)</summary>
        private void RefreshEndings() {
            var seen = 0;
            for (var i = 0; i < CATestSave.EndingCount; i++) {
                var s = CATestSave.EndingSeen(i);
                if (s) seen++;
                if (endingMarks != null && i < endingMarks.Length && endingMarks[i] != null) {
                    endingMarks[i].sprite = s ? markSeen : markUnseen;
                    endingMarks[i].color = s ? CATestUIKit.Accent : new Color(1f, 1f, 1f, 0.4f);
                }
                if (endingNames != null && i < endingNames.Length && endingNames[i] != null) {
                    endingNames[i].text = s ? CATestTitleScreen.EndingTitles[i] : "???";
                    endingNames[i].color = s ? CATestUIKit.TextColor : CATestUIKit.DimText;
                }
            }
            if (endingCount != null) endingCount.text = $"엔딩 기록  {seen} / {CATestSave.EndingCount}";
        }
    }
}
