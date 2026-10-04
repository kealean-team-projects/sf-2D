using System.Reflection;
using _02._Script._01_Players;
using _02._Script._01_Players.Components;
using _02._Script._04_Interaction;
using _02._Script._05_Managers;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace LHS_CATest {
    /// <summary>
    /// CATest 전용 화면 UI (CoreScene에 1개 상주).
    ///  1) 화면 암전(페이드)          : 구덩이 낙하 → 심해 착수 연출
    ///  2) 지역 이름(화면 중앙 상단)  : CATestAreaZone 에 들어가면 표시
    ///  3) 상호작용 안내(E 키)         : 플레이어 Interactor 가 지금 선택한 대상 위에 표시
    ///  4) 조작 힌트(화면 하단)        : CATestHintZone 안에 있을 때 표시 (화창한 숲 튜토리얼에서만 사용)
    ///  5) 영화 모드 검은 띠(레터박스)  : 컷신 동안 위/아래에서 검은 띠가 들어옴 (SetLetterbox)
    ///  6) 머리 위 대사                : 텍스트 상자 없이 흰 글자로 플레이어 머리 위에 한 글자씩 나타남 (Say / SayLines)
    ///
    /// UI는 코드로 만든다(프리팹/폰트 에셋 없이 동작). 한글은 TMP 기본 폰트(LiberationSans)에 글리프가 없어서
    /// OS 한글 폰트(바탕/맑은 고딕)를 동적 폰트로 불러와 UGUI Text 로 그린다.
    /// → 정식 빌드에서는 한글 TMP 폰트 에셋을 만들어 교체하는 것을 권장.
    /// </summary>
    [DefaultExecutionOrder(-120)]
    public sealed class CATestHUD : MonoBehaviour {
        [Header("Fonts (OS 폰트 이름, 앞에서부터 있는 것을 사용)")]
        [SerializeField] private string[] titleFonts = { "Batang", "바탕", "Gungsuh", "Malgun Gothic", "맑은 고딕", "AppleMyungjo", "Arial" };
        [SerializeField] private string[] bodyFonts = { "Malgun Gothic", "맑은 고딕", "Apple SD Gothic Neo", "Noto Sans CJK KR", "Arial" };
        [SerializeField] private Sprite keySprite;
        [SerializeField] private Sprite softSprite;

        [Header("Area Title")]
        [Tooltip("끄면 구역 이름 타이틀(화창한 숲, 안개 갈림길, 몽환의 숲 …)이 화면에 뜨지 않는다.\n" +
                 "끈 상태에서도 '마지막 장소 이름'은 기록되므로 타이틀 화면의 '이어하기 · 장소' 표시는 그대로 동작한다.\n" +
                 "구역 하나만 끄려면 그 구역의 CATestAreaZone.showTitle 을 끈다.")]
        [SerializeField] private bool showAreaTitles = true;
        [SerializeField] private float titleFadeIn = 0.9f;
        [SerializeField] private float titleHold = 2.6f;
        [SerializeField] private float titleFadeOut = 1.3f;

        public static CATestHUD Instance { get; private set; }

        private Canvas _canvas;
        private RectTransform _canvasRect;
        private Image _fader;
        private CanvasGroup _titleGroup;
        private Text _title, _subtitle;
        private RectTransform _prompt;
        private CanvasGroup _promptGroup;
        private Text _promptKey;
        private CanvasGroup _hintGroup;
        private Text _hintText;
        private RectTransform _barTop, _barBottom;
        private float _barAmount, _barTarget, _barSpeed = 1f;
        private const float BarHeight = 118f; // 1080 기준 (약 11%) — 2.39:1 영화 화면비에 가까운 두께
        private RectTransform _speech;
        private CanvasGroup _speechGroup;
        private Text _speechText;
        private int _speechVersion;
        private Transform _speechTarget;

        private int _titleVersion;
        private float _fadeTarget;
        private float _fadeSpeed;
        private Color _fadeColor = Color.black;
        private object _hintOwner;
        private float _hintAlpha;

        private Player _player;
        private Interactor _interactor;
        private static readonly FieldInfo CurrentTargetField =
            typeof(Interactor).GetField("_currentTarget", BindingFlags.Instance | BindingFlags.NonPublic);

        public static string LastTitle { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() {
            Instance = null;
            LastTitle = null;
        }

        /// <summary>플레이어 Transform (없으면 null). 맵 씬의 구역 스크립트들이 공통으로 사용.</summary>
        public static Transform PlayerTransform {
            get {
                if (Instance == null) return null;
                Instance.FindPlayer();
                return Instance._player != null && Instance._player.isActiveAndEnabled ? Instance._player.transform : null;
            }
        }

        public static Player CurrentPlayer {
            get {
                if (Instance == null) return null;
                Instance.FindPlayer();
                return Instance._player;
            }
        }

        private void Awake() {
            Instance = this;
            BuildUI();
        }

        private void OnDestroy() {
            if (Instance == this) Instance = null;
        }

        private void FindPlayer() {
            if (_player != null) return;
            if (GameManager.Instance != null && GameManager.Instance.player != null)
                _player = GameManager.Instance.player.GetComponent<Player>();
            if (_player == null) _player = FindAnyObjectByType<Player>(FindObjectsInactive.Include);
            if (_player != null) _interactor = _player.GetComponentInChildren<Interactor>(true);
        }

        // ───────────────────────── Fade ─────────────────────────
        /// <summary>화면을 alpha(0=투명, 1=검정)까지 duration초 동안 바꾼다. 시간 정지 중에도 동작(unscaled).</summary>
        public static async UniTask FadeTo(float alpha, float duration) {
            if (Instance == null) return;
            var hud = Instance;
            hud._fadeTarget = alpha;
            hud._fadeSpeed = duration <= 0f ? 999f : Mathf.Abs(alpha - hud._fader.color.a) / duration;
            while (hud != null && hud._fader != null && !Mathf.Approximately(hud._fader.color.a, alpha) && Mathf.Approximately(hud._fadeTarget, alpha))
                await UniTask.Yield(PlayerLoopTiming.Update);
        }

        // ───────────────────────── Title ─────────────────────────
        /// <summary>구역 이름 타이틀 전체 켜기/끄기(인스펙터 CATest_HUD > Show Area Titles 와 같은 값). 코드에서 바꿀 때 사용.</summary>
        public static bool AreaTitlesEnabled {
            get => Instance == null || Instance.showAreaTitles;
            set { if (Instance != null) Instance.showAreaTitles = value; }
        }

        /// <summary>타이틀은 띄우지 않고 "지금 장소 이름"만 기록.</summary>
        public static void RecordPlace(string title) {
            if (!string.IsNullOrEmpty(title)) LastTitle = title;
        }

        public static void ShowTitle(string title, string subtitle = null) {
            if (Instance == null || string.IsNullOrEmpty(title)) return;
            LastTitle = title; // 표시를 꺼도 장소 이름은 기록(세이브 장소 표시용)
            if (!Instance.showAreaTitles) return;
            Instance.TitleRoutine(title, subtitle).Forget();
        }

        private async UniTaskVoid TitleRoutine(string title, string subtitle) {
            var version = ++_titleVersion;
            _title.text = title;
            _subtitle.text = subtitle ?? string.Empty;
            var start = _titleGroup.alpha;
            for (var t = 0f; t < titleFadeIn; t += Time.unscaledDeltaTime) {
                if (version != _titleVersion) return;
                var k = t / titleFadeIn;
                _titleGroup.alpha = Mathf.Lerp(start, 1f, k);
                _title.rectTransform.anchoredPosition = new Vector2(0f, Mathf.Lerp(-10f, 0f, k));
                await UniTask.Yield();
            }
            _titleGroup.alpha = 1f;
            await UniTask.Delay(System.TimeSpan.FromSeconds(titleHold), true);
            for (var t = 0f; t < titleFadeOut; t += Time.unscaledDeltaTime) {
                if (version != _titleVersion) return;
                _titleGroup.alpha = 1f - t / titleFadeOut;
                await UniTask.Yield();
            }
            if (version == _titleVersion) _titleGroup.alpha = 0f;
        }

        // ───────────────────────── Hint ─────────────────────────
        public static void ShowHint(object owner, string message) {
            if (Instance == null) return;
            Instance._hintOwner = owner;
            Instance._hintText.text = message;
        }

        public static void HideHint(object owner) {
            if (Instance == null || Instance._hintOwner != owner) return;
            Instance._hintOwner = null;
        }

        // ───────────────────────── Letterbox ─────────────────────────
        /// <summary>영화 모드 검은 띠. on=true 면 위/아래에서 띠가 들어오고, false 면 빠진다.</summary>
        public static void SetLetterbox(bool on, float duration = 0.9f) {
            if (Instance == null) return;
            Instance._barTarget = on ? 1f : 0f;
            Instance._barSpeed = 1f / Mathf.Max(0.01f, duration);
        }

        public static bool LetterboxOn => Instance != null && Instance._barTarget > 0.5f;

        // ───────────────────────── Speech (머리 위 대사) ─────────────────────────
        /// <summary>
        /// 대사 한 줄을 target(없으면 플레이어) 머리 위에 띄운다. 한 글자씩 타자기처럼 나타나고, hold 초 뒤 사라진다.
        /// hold &lt; 0 이면 글자 수로 자동 계산. 끝날 때까지 기다릴 수 있도록 UniTask 를 돌려준다(컷신용).
        /// 새 대사가 오면 이전 대사는 즉시 교체된다.
        /// </summary>
        public static UniTask Say(string text, float hold = -1f, Transform target = null) {
            if (Instance == null || string.IsNullOrEmpty(text)) return UniTask.CompletedTask;
            // 설정에서 "머리 위 대사"를 끈 경우: 글자는 띄우지 않고, 컷신 흐름이 너무 급해지지 않도록 짧게만 기다린다.
            if (!CATestSettings.ShowPlayerLines) return UniTask.Delay(System.TimeSpan.FromSeconds(0.35f), true);
            return Instance.SpeechRoutine(text, hold, target);
        }

        /// <summary>설정에서 대사를 끄면 지금 떠 있는 대사도 바로 숨긴다.</summary>
        public static void HideSpeechNow() {
            if (Instance == null) return;
            Instance._speechVersion++;
            Instance._speechGroup.alpha = 0f;
        }

        /// <summary>페이드 색(기본 검정). 흰 빛으로 사라지는 연출(차원 이동, 귀환 엔딩)에 흰색으로 바꿔 쓴다.</summary>
        public static void SetFadeColor(Color c) {
            if (Instance == null) return;
            Instance._fadeColor = new Color(c.r, c.g, c.b, 1f);
            var f = Instance._fader.color;
            Instance._fader.color = new Color(c.r, c.g, c.b, f.a);
        }

        /// <summary>여러 줄을 순서대로 말한다. 줄 사이 gap 초.</summary>
        public static async UniTask SayLines(string[] lines, float gap = 0.25f, Transform target = null) {
            if (lines == null) return;
            foreach (var line in lines) {
                if (Instance == null) return;
                await Say(line, -1f, target);
                await UniTask.Delay(System.TimeSpan.FromSeconds(gap), true);
            }
        }

        private async UniTask SpeechRoutine(string text, float hold, Transform target) {
            var version = ++_speechVersion;
            _speechTarget = target;
            if (hold < 0f) hold = Mathf.Clamp(1.1f + text.Length * 0.07f, 1.6f, 4.5f);
            _speechText.text = string.Empty;
            // 등장: 살짝 위로 떠오르며 나타남
            for (var t = 0f; t < 0.2f; t += Time.unscaledDeltaTime) {
                if (version != _speechVersion) return;
                _speechGroup.alpha = t / 0.2f;
                await UniTask.Yield();
            }
            _speechGroup.alpha = 1f;
            // 타자기: 초당 약 18글자
            var shown = 0f;
            while (shown < text.Length) {
                if (version != _speechVersion) return;
                shown += Time.unscaledDeltaTime * 18f;
                _speechText.text = text.Substring(0, Mathf.Min(text.Length, Mathf.FloorToInt(shown) + 1));
                await UniTask.Yield();
            }
            _speechText.text = text;
            await UniTask.Delay(System.TimeSpan.FromSeconds(hold), true);
            for (var t = 0f; t < 0.45f; t += Time.unscaledDeltaTime) {
                if (version != _speechVersion) return;
                _speechGroup.alpha = 1f - t / 0.45f;
                await UniTask.Yield();
            }
            if (version == _speechVersion) {
                _speechGroup.alpha = 0f;
                _speechTarget = null;
            }
        }

        /// <summary>
        /// 플레이어 "머리 꼭대기" 월드 좌표.
        /// 콜라이더는 몸통만 감싸는 경우가 많아(머리카락/모자 스프라이트가 콜라이더 위로 튀어나옴)
        /// 콜라이더 윗면과 SpriteRenderer 들의 윗면 중 더 높은 쪽을 쓴다 → 대사가 머리와 겹치지 않는다.
        /// </summary>
        private static Vector3 PlayerHeadTop(Component p) {
            var pcol = p.GetComponent<Collider2D>();
            var x = pcol != null ? pcol.bounds.center.x : p.transform.position.x;
            var top = pcol != null ? pcol.bounds.max.y : p.transform.position.y + 1.5f;
            foreach (var r in p.GetComponentsInChildren<SpriteRenderer>()) {
                if (r == null || !r.enabled || r.sprite == null) continue;
                var b = r.bounds;
                if (b.size.y > 6f) continue; // 이펙트 등 비정상적으로 큰 스프라이트 무시
                top = Mathf.Max(top, b.max.y);
            }
            return new Vector3(x, top, p.transform.position.z);
        }

        private void UpdateSpeech() {
            if (_speechGroup.alpha <= 0.001f) return;
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 head;
            if (_speechTarget != null) head = _speechTarget.position + Vector3.up * 2.4f;
            else {
                FindPlayer();
                if (_player == null) return;
                head = PlayerHeadTop(_player) + Vector3.up * 0.6f;
            }
            var screen = cam.WorldToScreenPoint(head);
            if (screen.z < 0f) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screen, null, out var local);
            // 부드럽게 따라가기(카메라가 흔들려도 글자가 떨리지 않게)
            _speech.anchoredPosition = Vector2.Lerp(_speech.anchoredPosition, local + new Vector2(0f, 14f), 1f - Mathf.Exp(-Time.unscaledDeltaTime * 18f));
        }

        // ───────────────────────── Update ─────────────────────────
        private void Update() {
            _barAmount = Mathf.MoveTowards(_barAmount, _barTarget, _barSpeed * Time.unscaledDeltaTime);
            var eased = _barAmount * _barAmount * (3f - 2f * _barAmount);
            _barTop.sizeDelta = new Vector2(0f, BarHeight * eased);
            _barBottom.sizeDelta = new Vector2(0f, BarHeight * eased);
            UpdateSpeech();

            var a = _fader.color.a;
            if (!Mathf.Approximately(a, _fadeTarget)) {
                a = Mathf.MoveTowards(a, _fadeTarget, _fadeSpeed * Time.unscaledDeltaTime);
                _fader.color = new Color(_fadeColor.r, _fadeColor.g, _fadeColor.b, a);
            }
            _fader.enabled = a > 0.001f;

            _hintAlpha = Mathf.MoveTowards(_hintAlpha, _hintOwner != null && _barTarget < 0.5f ? 1f : 0f, Time.unscaledDeltaTime * 3f);
            _hintGroup.alpha = _hintAlpha;

            UpdatePrompt();
        }

        private void UpdatePrompt() {
            FindPlayer();
            Component target = null;
            if (_interactor != null && _player != null && _player.isActiveAndEnabled && !_player.IsDead && CurrentTargetField != null)
                target = CurrentTargetField.GetValue(_interactor) as Component;
            if (target != null && target is Behaviour b && !b.isActiveAndEnabled) target = null;

            // 컷신(검은 띠) 중에는 E 안내를 숨긴다
            var want = target != null && _barTarget < 0.5f ? 1f : 0f;
            _promptGroup.alpha = Mathf.MoveTowards(_promptGroup.alpha, want, Time.unscaledDeltaTime * 8f);
            if (target == null) return;

            var cam = Camera.main;
            if (cam == null) return;
            var top = target.transform.position + Vector3.up * 1.2f;
            var col = target.GetComponent<Collider2D>();
            if (col != null) top = new Vector3(col.bounds.center.x, col.bounds.max.y + 0.9f, target.transform.position.z);
            // 플레이어 머리와 겹치지 않도록: 대상 윗면과 플레이어 머리 중 더 높은 쪽 위에 띄운다
            var pcol = _player.GetComponent<Collider2D>();
            if (pcol != null) top.y = Mathf.Max(top.y, pcol.bounds.max.y + 0.9f);
            var screen = cam.WorldToScreenPoint(top);
            if (screen.z < 0f) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screen, null, out var local);
            _prompt.anchoredPosition = local + new Vector2(0f, Mathf.Sin(Time.unscaledTime * 3f) * 4f);
            // 부드럽게 커졌다 줄어드는 숨쉬기 효과
            _prompt.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 4f) * 0.04f);
        }

        private static string LabelFor(Component target) {
            var label = target.GetComponentInParent<CATestInteractLabel>();
            if (label != null) return label.Resolve(target);
            return target switch {
                InteractLight l => l.IsActive ? "끄기" : "켜기",
                CATestBreakableWall => "부수기",
                CATestLever lv => lv.PromptText,
                _ => "상호작용"
            };
        }

        // ───────────────────────── UI 생성 ─────────────────────────
        private void BuildUI() {
            var titleFont = LoadFont(titleFonts, 64);
            var bodyFont = LoadFont(bodyFonts, 32);

            var canvasGo = new GameObject("CATest_HUD_Canvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 400;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            _canvasRect = (RectTransform)canvasGo.transform;

            // 지역 이름
            var titleRoot = NewRect("AreaTitle", _canvasRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(1200f, 200f));
            _titleGroup = titleRoot.gameObject.AddComponent<CanvasGroup>();
            _titleGroup.alpha = 0f;
            _titleGroup.blocksRaycasts = false;
            var glow = NewImage("Glow", titleRoot, softSprite, new Color(0f, 0f, 0f, 0.35f));
            glow.rectTransform.anchoredPosition = new Vector2(0f, -55f);
            glow.rectTransform.sizeDelta = new Vector2(900f, 190f);
            _title = NewText("Title", titleRoot, titleFont, 68, new Color(0.96f, 0.93f, 0.84f), TextAnchor.MiddleCenter);
            _title.rectTransform.sizeDelta = new Vector2(1200f, 90f);
            _title.rectTransform.anchorMin = _title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _title.rectTransform.pivot = new Vector2(0.5f, 1f);
            var shadow = _title.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            shadow.effectDistance = new Vector2(2f, -3f);
            var line = NewImage("Line", titleRoot, null, new Color(0.96f, 0.9f, 0.75f, 0.7f));
            line.rectTransform.anchoredPosition = new Vector2(0f, -100f);
            line.rectTransform.sizeDelta = new Vector2(360f, 2f);
            _subtitle = NewText("Subtitle", titleRoot, bodyFont, 26, new Color(0.9f, 0.88f, 0.8f, 0.85f), TextAnchor.MiddleCenter);
            _subtitle.rectTransform.anchoredPosition = new Vector2(0f, -128f);
            _subtitle.rectTransform.sizeDelta = new Vector2(1200f, 40f);

            // 상호작용 안내
            _prompt = NewRect("InteractPrompt", _canvasRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(260f, 64f));
            _promptGroup = _prompt.gameObject.AddComponent<CanvasGroup>();
            _promptGroup.alpha = 0f;
            _promptGroup.blocksRaycasts = false;
            // [E] 하나만: 흰 테두리 키캡 + 흰 글자 (피드백 반영: 문구 없이 간결하게)
            var key = NewImage("Key", _prompt, keySprite, Color.white);
            key.rectTransform.anchoredPosition = new Vector2(0f, 34f);
            key.rectTransform.sizeDelta = new Vector2(62f, 62f);
            _promptKey = NewText("KeyText", key.rectTransform, bodyFont, 30, Color.white, TextAnchor.MiddleCenter);
            _promptKey.text = "E";
            _promptKey.fontStyle = FontStyle.Bold;
            _promptKey.rectTransform.sizeDelta = new Vector2(62f, 62f);
            var ks = _promptKey.gameObject.AddComponent<Shadow>();
            ks.effectColor = new Color(0f, 0f, 0f, 0.5f);
            ks.effectDistance = new Vector2(1.5f, -1.5f);

            // 조작 힌트
            var hintRoot = NewRect("Hint", _canvasRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(1100f, 70f));
            _hintGroup = hintRoot.gameObject.AddComponent<CanvasGroup>();
            _hintGroup.alpha = 0f;
            _hintGroup.blocksRaycasts = false;
            var hbg = NewImage("Bg", hintRoot, softSprite, new Color(0f, 0f, 0f, 0.45f));
            hbg.rectTransform.sizeDelta = new Vector2(900f, 110f);
            _hintText = NewText("Text", hintRoot, bodyFont, 30, new Color(0.97f, 0.95f, 0.88f), TextAnchor.MiddleCenter);
            _hintText.rectTransform.sizeDelta = new Vector2(1100f, 70f);

            // 머리 위 대사: 상자·외곽선·그림자 없이 순수한 흰 글자, 작게(22px @1080p).
            //  Text 컴포넌트만 두고 Outline/Shadow 효과 컴포넌트는 붙이지 않는다.
            _speech = NewRect("Speech", _canvasRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(700f, 40f));
            _speechGroup = _speech.gameObject.AddComponent<CanvasGroup>();
            _speechGroup.alpha = 0f;
            _speechGroup.blocksRaycasts = false;
            _speechText = NewText("Text", _speech, bodyFont, 22, Color.white, TextAnchor.LowerCenter);
            _speechText.rectTransform.pivot = new Vector2(0.5f, 0f);
            _speechText.rectTransform.sizeDelta = new Vector2(700f, 40f);

            // 영화 모드 검은 띠 (위/아래). 높이 0 에서 시작해 컷신 때 BarHeight 까지 늘어난다.
            _barTop = NewRect("LetterboxTop", _canvasRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
            _barTop.anchorMin = new Vector2(0f, 1f);
            _barTop.anchorMax = new Vector2(1f, 1f);
            _barTop.gameObject.AddComponent<Image>().color = Color.black;
            _barBottom = NewRect("LetterboxBottom", _canvasRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
            _barBottom.anchorMin = new Vector2(0f, 0f);
            _barBottom.anchorMax = new Vector2(1f, 0f);
            _barBottom.gameObject.AddComponent<Image>().color = Color.black;
            _barTop.GetComponent<Image>().raycastTarget = false;
            _barBottom.GetComponent<Image>().raycastTarget = false;

            // 페이드(가장 위)
            _fader = NewImage("Fader", _canvasRect, null, new Color(0f, 0f, 0f, 0f));
            _fader.rectTransform.anchorMin = Vector2.zero;
            _fader.rectTransform.anchorMax = Vector2.one;
            _fader.rectTransform.offsetMin = _fader.rectTransform.offsetMax = Vector2.zero;
            _fader.enabled = false;
        }

        private static Font LoadFont(string[] names, int size) {
            Font f = null;
            try {
                f = Font.CreateDynamicFontFromOSFont(names, size);
            }
            catch {
                // ignored → 아래 기본 폰트
            }
            if (f == null) f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return f;
        }

        private static RectTransform NewRect(string name, RectTransform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size) {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        private static Image NewImage(string name, RectTransform parent, Sprite sprite, Color color) {
            var rt = NewRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100, 100));
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static Text NewText(string name, RectTransform parent, Font font, int size, Color color, TextAnchor align) {
            var rt = NewRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 60));
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }
    }
}
