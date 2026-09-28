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
    /// ■ 동작
    ///  - 값을 바꾸는 즉시 CATestSettings 에 반영(미리보기 겸 적용) → [닫기] 때 PlayerPrefs 에 저장
    ///  - 해상도는 ◀ ▶ 버튼으로 모니터가 지원하는 해상도 목록을 돌려 고름
    ///  - 켜짐/꺼짐 항목은 버튼 한 번 누를 때마다 토글
    ///  - ESC(또는 [닫기]) → 창 닫고 Closed 이벤트 → 부른 쪽 메뉴가 다시 선택 상태로
    /// </summary>
    [DefaultExecutionOrder(-60)] // 일시정지 메뉴보다 먼저 ESC 를 받는다
    public sealed class CATestSettingsPanel : MonoBehaviour {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Slider volume;
        [SerializeField] private Text volumeValue;
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

        public bool IsOpen { get; private set; }
        public event Action Closed;

        private readonly List<Vector2Int> _resolutions = new();
        private int _resIndex;
        private bool _refreshing;

        private void Awake() {
            CATestSettings.EnsureLoaded();
            CATestUIKit.Show(group, false);
            if (volume != null) volume.onValueChanged.AddListener(v => { if (!_refreshing) CATestSettings.SetVolume(v); });
            if (brightness != null) {
                brightness.minValue = -1f; brightness.maxValue = 1f;
                brightness.onValueChanged.AddListener(v => { if (!_refreshing) CATestSettings.SetBrightness(v); });
            }
            if (resPrev != null) resPrev.onClick.AddListener(() => StepResolution(-1));
            if (resNext != null) resNext.onClick.AddListener(() => StepResolution(1));
            if (fullscreen != null) fullscreen.onClick.AddListener(() => CATestSettings.SetFullscreen(!CATestSettings.Fullscreen));
            if (playerLines != null) playerLines.onClick.AddListener(() => CATestSettings.SetShowPlayerLines(!CATestSettings.ShowPlayerLines));
            if (shake != null) shake.onClick.AddListener(() => CATestSettings.SetScreenShake(!CATestSettings.ScreenShake));
            if (defaults != null) defaults.onClick.AddListener(CATestSettings.ResetToDefault);
            if (close != null) close.onClick.AddListener(Close);
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
        }

        public void Open() {
            IsOpen = true;
            Refresh();
            CATestUIKit.Show(group, true);
            FadeIn().Forget();
            if (EventSystem.current != null && volume != null) EventSystem.current.SetSelectedGameObject(volume.gameObject);
        }

        public void Close() {
            if (!IsOpen) return;
            IsOpen = false;
            CATestSettings.Save();
            CATestUIKit.Show(group, false);
            Closed?.Invoke();
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
            if (CATestUIInput.ConsumeEscape()) Close();
        }

        // 켜짐 = 주황 글자, 꺼짐 = 흐린 글자 → 상태를 색으로도 구분
        private static void OnOff(Text t, bool on) {
            if (t == null) return;
            t.text = on ? "켜짐" : "꺼짐";
            t.color = on ? CATestUIKit.Accent : CATestUIKit.DimText;
        }

        private void Refresh() {
            _refreshing = true;
            if (volume != null) volume.SetValueWithoutNotify(CATestSettings.MasterVolume);
            if (volumeValue != null) volumeValue.text = Mathf.RoundToInt(CATestSettings.MasterVolume * 100f) + "%";
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
    }
}
