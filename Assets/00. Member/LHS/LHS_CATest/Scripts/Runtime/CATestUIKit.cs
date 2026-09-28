using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LHS_CATest {
    /// <summary>
    /// UGUI 요소를 코드로 만드는 도우미 (에디터 빌더와 런타임 둘 다 사용).
    ///
    /// ■ 왜 코드로?  맵 빌더처럼 한 번에 다시 만들 수 있게. 만들어진 결과는 평범한 UGUI 오브젝트라
    ///   씬 Hierarchy 에서 위치/색/글자를 손으로 고쳐도 된다(단 다시 Build 하면 덮어씀).
    /// ■ 공통 규칙
    ///   - Canvas: Screen Space Overlay, CanvasScaler 1920×1080 기준(가로세로 반반 맞춤) → 해상도가 달라도 비율 유지
    ///   - 글꼴: 빌드 시에는 기본 폰트, 실행하면 CATestUIFont 가 OS 한글 폰트(맑은 고딕/바탕)로 바꿔 끼운다
    ///   - 색: 흰색 글자 + 어두운 반투명 패널 + 따뜻한 강조색(일식 테두리 빛)
    /// </summary>
    public static class CATestUIKit {
        public static readonly Color TextColor = new(0.95f, 0.93f, 0.98f, 1f);
        public static readonly Color DimText = new(0.72f, 0.69f, 0.8f, 1f);
        public static readonly Color Accent = new(1f, 0.72f, 0.55f, 1f);
        public static readonly Color PanelTint = new(1f, 1f, 1f, 1f);

        public static Canvas Canvas(Transform parent, string name, int sortOrder) {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = sortOrder;
            var s = go.AddComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1920, 1080);
            s.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            go.AddComponent<CATestUIFont>();
            return c;
        }

        public static RectTransform Rect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 pos, Vector2 size) {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(Transform parent, string name) {
            var rt = Rect(parent, name, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            return rt;
        }

        public static Image Img(Transform parent, string name, Color color, Sprite sprite = null, bool sliced = false) {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            if (sliced && sprite != null) img.type = Image.Type.Sliced;
            return img;
        }

        public static Text Txt(Transform parent, string name, string text, int size, Color color,
            TextAnchor align = TextAnchor.MiddleLeft, bool title = false) {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = BuiltinFont;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            if (title) go.AddComponent<CATestUITitleText>();
            return t;
        }

        private static Font _builtin;
        public static Font BuiltinFont {
            get {
                if (_builtin == null) _builtin = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _builtin;
            }
        }

        /// <summary>
        /// 메뉴 버튼: 글자 + (선택/마우스 올림 시) 왼쪽 마름모 표식과 아래 선이 나타나는 스타일. 상자 없음.
        /// </summary>
        public static Button MenuButton(Transform parent, string name, string label, Vector2 size, int fontSize,
            Sprite diamond, Sprite line, TextAnchor align = TextAnchor.MiddleLeft) {
            var rt = Rect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            var hit = rt.gameObject.AddComponent<Image>();
            hit.color = new Color(1, 1, 1, 0f); // 투명 클릭 영역
            var btn = rt.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            var txt = Txt(rt, "Label", label, fontSize, TextColor, align);
            var trt = txt.rectTransform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.sizeDelta = Vector2.zero;
            trt.anchoredPosition = align == TextAnchor.MiddleLeft ? new Vector2(34f, 0f) : Vector2.zero;
            var mark = Img(rt, "Marker", Accent, diamond);
            var mrt = mark.rectTransform;
            mrt.anchorMin = mrt.anchorMax = align == TextAnchor.MiddleLeft ? new Vector2(0f, 0.5f) : new Vector2(0.5f, 0.5f);
            mrt.sizeDelta = new Vector2(22f, 22f);
            mrt.anchoredPosition = align == TextAnchor.MiddleLeft ? new Vector2(12f, 0f) : new Vector2(-size.x * 0.5f + 12f, 0f);
            var underline = Img(rt, "Underline", Accent, line);
            var urt = underline.rectTransform;
            urt.anchorMin = new Vector2(0f, 0f); urt.anchorMax = new Vector2(1f, 0f);
            urt.sizeDelta = new Vector2(0f, 6f);
            urt.anchoredPosition = new Vector2(0f, 4f);
            var mb = rt.gameObject.AddComponent<CATestMenuButton>();
            mb.Setup(txt, mark, underline);
            return btn;
        }

        /// <summary>가로 슬라이더(배경 선 + 채움 + 동그란 손잡이).</summary>
        public static Slider Slider(Transform parent, string name, Vector2 size, Sprite knob, Sprite pill) {
            var rt = Rect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            var bg = Img(rt, "Background", new Color(1f, 1f, 1f, 0.18f), pill, true);
            var bgrt = bg.rectTransform;
            bgrt.anchorMin = new Vector2(0f, 0.5f); bgrt.anchorMax = new Vector2(1f, 0.5f); bgrt.sizeDelta = new Vector2(0f, 8f);
            var fillArea = Rect(rt, "Fill Area", new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-8f, 8f));
            var fill = Img(fillArea, "Fill", Accent, pill, true);
            fill.rectTransform.sizeDelta = Vector2.zero;
            var handleArea = Rect(rt, "Handle Slide Area", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-16f, 0f));
            var handle = Img(handleArea, "Handle", Color.white, knob);
            handle.rectTransform.sizeDelta = new Vector2(28f, -12f); // 세로는 슬라이더 높이(40)에 맞춰 늘어나므로 -12 → 28×28 원
            handle.raycastTarget = true;
            var s = rt.gameObject.AddComponent<Slider>();
            s.fillRect = fill.rectTransform;
            s.handleRect = handle.rectTransform;
            s.targetGraphic = handle;
            // 키보드로 선택된 슬라이더는 손잡이가 주황빛으로 → 지금 어느 줄을 조작 중인지 보이게
            var sc = s.colors;
            sc.highlightedColor = Accent; sc.selectedColor = Accent; sc.pressedColor = new Color(0.9f, 0.6f, 0.45f, 1f);
            s.colors = sc;
            s.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            s.minValue = 0f; s.maxValue = 1f;
            var hit = rt.gameObject.AddComponent<Image>();
            hit.color = new Color(1, 1, 1, 0f);
            return s;
        }

        /// <summary>작은 알약 모양 버튼(켜짐/꺼짐, ◀ ▶ 등).</summary>
        public static Button PillButton(Transform parent, string name, string label, Vector2 size, int fontSize, Sprite pill) {
            var rt = Rect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = pill;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            var btn = rt.gameObject.AddComponent<Button>();
            // 버튼 색 = 이미지 색(흰색) × 상태 색. 평소엔 옅은 반투명, 선택/마우스오버 시 주황빛으로 진해짐.
            var cb = btn.colors;
            cb.normalColor = new Color(1f, 1f, 1f, 0.14f);
            cb.highlightedColor = new Color(1f, 0.72f, 0.55f, 0.5f);
            cb.selectedColor = new Color(1f, 0.72f, 0.55f, 0.5f);
            cb.pressedColor = new Color(1f, 0.72f, 0.55f, 0.8f);
            cb.disabledColor = new Color(1f, 1f, 1f, 0.05f);
            cb.colorMultiplier = 1f;
            btn.colors = cb;
            btn.targetGraphic = img;
            var t = Txt(rt, "Label", label, fontSize, TextColor, TextAnchor.MiddleCenter);
            t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one; t.rectTransform.sizeDelta = Vector2.zero;
            return btn;
        }

        public static CanvasGroup Group(GameObject go, bool visible) {
            var g = go.GetComponent<CanvasGroup>();
            if (g == null) g = go.AddComponent<CanvasGroup>();
            g.alpha = visible ? 1f : 0f;
            g.interactable = visible;
            g.blocksRaycasts = visible;
            return g;
        }

        public static void Show(CanvasGroup g, bool on) {
            if (g == null) return;
            g.alpha = on ? 1f : 0f;
            g.interactable = on;
            g.blocksRaycasts = on;
        }

        /// <summary>메뉴 버튼들을 세로로 이어 키보드(위/아래) 이동이 순서대로 되게 명시적 내비게이션을 건다.</summary>
        public static void ChainVertical(IList<Button> buttons) {
            for (var i = 0; i < buttons.Count; i++) {
                var nav = new Navigation { mode = Navigation.Mode.Explicit };
                nav.selectOnUp = buttons[(i - 1 + buttons.Count) % buttons.Count];
                nav.selectOnDown = buttons[(i + 1) % buttons.Count];
                buttons[i].navigation = nav;
            }
        }

        // ───────────── 런타임 폰트 ─────────────
        private static Font _body, _title;
        private static readonly string[] BodyFonts = { "Malgun Gothic", "맑은 고딕", "Apple SD Gothic Neo", "Noto Sans CJK KR", "Arial" };
        private static readonly string[] TitleFonts = { "Batang", "바탕", "Gungsuh", "AppleMyungjo", "Malgun Gothic", "Arial" };

        public static Font BodyFont => _body != null ? _body : _body = Font.CreateDynamicFontFromOSFont(Pick(BodyFonts), 32);
        public static Font TitleFont => _title != null ? _title : _title = Font.CreateDynamicFontFromOSFont(Pick(TitleFonts), 48);

        private static string[] Pick(string[] wanted) {
            var installed = new HashSet<string>(Font.GetOSInstalledFontNames());
            var list = new List<string>();
            foreach (var n in wanted) if (installed.Contains(n)) list.Add(n);
            if (list.Count == 0) list.Add("Arial");
            return list.ToArray();
        }
    }
}
