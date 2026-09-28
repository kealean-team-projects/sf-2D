#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace LHS_CATest.EditorTools {
    /// <summary>
    /// UGUI 화면들을 만든다.
    ///  - 타이틀 씬(CATest_Title): 황혼 하늘 배경 + 메뉴(이어하기/새로 시작/설정/진행도 초기화/게임 종료) + 엔딩 수집 + 설정 창 + 확인 창
    ///  - CoreScene 게임 UI: ESC 메뉴 + 설정 창 + 확인 창 + 용 대화창/선택지 + 엔딩 화면
    /// 모두 1920×1080 기준 Canvas(Scale With Screen Size). 한글 폰트는 실행 시 CATestUIFont 가 OS 폰트로 바꿔 끼운다.
    /// </summary>
    public static partial class CATestBuilder {
        public const string TitleScenePath = ScenesDir + "/CATest_Title.unity";
        private const string UIArt = ArtDir + "/UI/";
        public const string GameTitle = "문 너머";              // (가제) 게임 제목 — 여기서 바꾸면 타이틀에 반영
        public const string GameSubtitle = "BEYOND THE DOOR";

        private static Sprite UiPanel => Sp(UIArt + "UI_Panel.png");
        private static Sprite UiLine => Sp(UIArt + "UI_Line.png");
        private static Sprite UiDiamond => Sp(UIArt + "UI_Diamond.png");
        private static Sprite UiKnob => Sp(UIArt + "UI_Knob.png");
        private static Sprite UiRing => Sp(UIArt + "UI_Ring.png");
        private static Sprite UiPill => Sp(UIArt + "UI_Pill.png");
        private static Sprite UiGradLeft => Sp(UIArt + "UI_GradientLeft.png");
        private static Sprite UiGradUp => Sp(UIArt + "UI_GradientUp.png");

        /// <summary>UI 스프라이트 9-slice 테두리 설정(PrepareAssets 뒤에 호출).</summary>
        public static void PrepareUISprites() {
            SetBorder(UIArt + "UI_Panel.png", new Vector4(24, 24, 24, 24));
            SetBorder(UIArt + "UI_Pill.png", new Vector4(20, 18, 20, 18));
        }

        private static void SetBorder(string path, Vector4 border) {
            if (AssetImporter.GetAtPath(path) is not TextureImporter ti) return;
            if (ti.spriteBorder == border) return;
            ti.spriteBorder = border;
            ti.SaveAndReimport();
            SpriteCache.Clear();
        }

        // ───────────────────────── 공통: 설정 창 / 확인 창 ─────────────────────────
        private static CATestSettingsPanel BuildSettingsPanel(Transform canvas) {
            var root = CATestUIKit.Stretch(canvas, "SettingsPanel");
            var group = CATestUIKit.Group(root.gameObject, false);
            var dim = CATestUIKit.Img(root, "Dim", new Color(0.02f, 0.01f, 0.05f, 0.7f));
            Fill(dim.rectTransform);
            dim.raycastTarget = true;
            var win = CATestUIKit.Img(root, "Window", Color.white, UiPanel, true).rectTransform;
            win.sizeDelta = new Vector2(900f, 780f);
            CATestUIKit.Txt(win, "Title", "설정", 44, CATestUIKit.TextColor, TextAnchor.MiddleCenter, true).rectTransform.anchoredPosition = new Vector2(0f, 322f);
            var line = CATestUIKit.Img(win, "Line", new Color(1f, 0.85f, 0.75f, 0.6f), UiLine).rectTransform;
            line.sizeDelta = new Vector2(620f, 6f); line.anchoredPosition = new Vector2(0f, 280f);

            var rows = new[] { "전체 볼륨", "화면 밝기", "해상도", "전체 화면", "머리 위 대사 표시", "화면 흔들림" };
            var y0 = 210f;
            Text Label(int i) {
                var t = CATestUIKit.Txt(win, "Label_" + i, rows[i], 30, CATestUIKit.TextColor, TextAnchor.MiddleLeft);
                t.rectTransform.sizeDelta = new Vector2(340f, 50f);
                t.rectTransform.anchoredPosition = new Vector2(-230f, y0 - i * 76f);
                return t;
            }
            for (var i = 0; i < rows.Length; i++) Label(i);

            var vol = CATestUIKit.Slider(win, "VolumeSlider", new Vector2(330f, 40f), UiKnob, UiPill);
            vol.GetComponent<RectTransform>().anchoredPosition = new Vector2(130f, y0);
            var volText = CATestUIKit.Txt(win, "VolumeValue", "100%", 26, CATestUIKit.DimText, TextAnchor.MiddleLeft);
            volText.rectTransform.sizeDelta = new Vector2(100f, 40f); volText.rectTransform.anchoredPosition = new Vector2(355f, y0);
            var bri = CATestUIKit.Slider(win, "BrightnessSlider", new Vector2(330f, 40f), UiKnob, UiPill);
            bri.GetComponent<RectTransform>().anchoredPosition = new Vector2(130f, y0 - 76f);
            var briText = CATestUIKit.Txt(win, "BrightnessValue", "기본", 26, CATestUIKit.DimText, TextAnchor.MiddleLeft);
            briText.rectTransform.sizeDelta = new Vector2(100f, 40f); briText.rectTransform.anchoredPosition = new Vector2(355f, y0 - 76f);

            var prev = CATestUIKit.PillButton(win, "ResPrev", "◀", new Vector2(60f, 46f), 22, UiPill);
            prev.GetComponent<RectTransform>().anchoredPosition = new Vector2(-10f, y0 - 152f);
            var resText = CATestUIKit.Txt(win, "ResValue", "1920 × 1080", 28, CATestUIKit.TextColor, TextAnchor.MiddleCenter);
            resText.rectTransform.sizeDelta = new Vector2(220f, 46f); resText.rectTransform.anchoredPosition = new Vector2(130f, y0 - 152f);
            var next = CATestUIKit.PillButton(win, "ResNext", "▶", new Vector2(60f, 46f), 22, UiPill);
            next.GetComponent<RectTransform>().anchoredPosition = new Vector2(270f, y0 - 152f);

            Button Toggle(string n, int row, out Text label) {
                var b = CATestUIKit.PillButton(win, n, "켜짐", new Vector2(200f, 46f), 26, UiPill);
                b.GetComponent<RectTransform>().anchoredPosition = new Vector2(130f, y0 - row * 76f);
                label = b.GetComponentInChildren<Text>();
                return b;
            }
            var full = Toggle("Fullscreen", 3, out var fullText);
            var lines = Toggle("PlayerLines", 4, out var linesText);
            var shake = Toggle("Shake", 5, out var shakeText);

            var def = CATestUIKit.PillButton(win, "Defaults", "기본값", new Vector2(210f, 54f), 28, UiPill);
            def.GetComponent<RectTransform>().anchoredPosition = new Vector2(-125f, -305f);
            var close = CATestUIKit.PillButton(win, "Close", "닫기", new Vector2(210f, 54f), 28, UiPill);
            close.GetComponent<RectTransform>().anchoredPosition = new Vector2(125f, -305f);
            var line2 = CATestUIKit.Img(win, "LineBottom", new Color(1f, 0.85f, 0.75f, 0.35f), UiLine).rectTransform;
            line2.sizeDelta = new Vector2(620f, 4f); line2.anchoredPosition = new Vector2(0f, -240f);

            // 키보드 내비게이션: 위/아래로 줄 이동
            var order = new Selectable[] { vol, bri, next, full, lines, shake, close };
            for (var i = 0; i < order.Length; i++) {
                var nav = new Navigation { mode = Navigation.Mode.Explicit };
                nav.selectOnUp = order[(i - 1 + order.Length) % order.Length];
                nav.selectOnDown = order[(i + 1) % order.Length];
                if (order[i] == next) nav.selectOnLeft = prev;
                if (order[i] == close) nav.selectOnLeft = def;
                order[i].navigation = nav;
            }
            var pn = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = next, selectOnUp = bri, selectOnDown = full };
            prev.navigation = pn;
            var dn = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = close, selectOnUp = shake, selectOnDown = vol };
            def.navigation = dn;

            var panel = root.gameObject.AddComponent<CATestSettingsPanel>();
            Set(panel, "group", group);
            Set(panel, "volume", vol);
            Set(panel, "volumeValue", volText);
            Set(panel, "brightness", bri);
            Set(panel, "brightnessValue", briText);
            Set(panel, "resPrev", prev);
            Set(panel, "resNext", next);
            Set(panel, "resText", resText);
            Set(panel, "fullscreen", full);
            Set(panel, "fullscreenText", fullText);
            Set(panel, "playerLines", lines);
            Set(panel, "playerLinesText", linesText);
            Set(panel, "shake", shake);
            Set(panel, "shakeText", shakeText);
            Set(panel, "defaults", def);
            Set(panel, "close", close);
            return panel;
        }

        private static CATestConfirmDialog BuildConfirm(Transform canvas) {
            var root = CATestUIKit.Stretch(canvas, "ConfirmDialog");
            var group = CATestUIKit.Group(root.gameObject, false);
            var dim = CATestUIKit.Img(root, "Dim", new Color(0f, 0f, 0f, 0.6f));
            Fill(dim.rectTransform);
            dim.raycastTarget = true;
            var win = CATestUIKit.Img(root, "Window", Color.white, UiPanel, true).rectTransform;
            win.sizeDelta = new Vector2(820f, 330f);
            var msg = CATestUIKit.Txt(win, "Message", "…", 32, CATestUIKit.TextColor, TextAnchor.MiddleCenter);
            msg.rectTransform.sizeDelta = new Vector2(720f, 160f);
            msg.rectTransform.anchoredPosition = new Vector2(0f, 48f);
            msg.horizontalOverflow = HorizontalWrapMode.Wrap;
            msg.lineSpacing = 1.2f;
            var yes = CATestUIKit.PillButton(win, "Yes", "확인", new Vector2(230f, 58f), 28, UiPill);
            yes.GetComponent<RectTransform>().anchoredPosition = new Vector2(-135f, -100f);
            var no = CATestUIKit.PillButton(win, "No", "취소", new Vector2(230f, 58f), 28, UiPill);
            no.GetComponent<RectTransform>().anchoredPosition = new Vector2(135f, -100f);
            yes.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = no, selectOnLeft = no };
            no.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = yes, selectOnLeft = yes };
            var c = root.gameObject.AddComponent<CATestConfirmDialog>();
            Set(c, "group", group);
            Set(c, "message", msg);
            Set(c, "yes", yes);
            Set(c, "yesLabel", yes.GetComponentInChildren<Text>());
            Set(c, "no", no);
            return c;
        }

        private static void Fill(RectTransform rt) {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero; rt.anchoredPosition = Vector2.zero;
        }

        private static Button LeftMenuButton(Transform parent, string name, string label, float y) {
            var b = CATestUIKit.MenuButton(parent, name, label, new Vector2(520f, 62f), 38, UiDiamond, UiLine);
            var rt = b.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(150f, y);
            return b;
        }

        // ───────────────────────── 게임(CoreScene) UI ─────────────────────────
        public static void BuildGameUI(Transform parent) {
            // 1) 용 대화창 + 선택지 (HUD 레터박스 위에)
            var dCanvas = CATestUIKit.Canvas(parent, "CATest_DialogueCanvas", 450).transform;
            var box = CATestUIKit.Rect(dCanvas, "DialogueBox", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 380f));
            var bg = CATestUIKit.Img(box, "Backdrop", new Color(0.02f, 0.01f, 0.05f, 0.85f), UiGradUp);
            Fill(bg.rectTransform);
            var dline = CATestUIKit.Img(box, "TopLine", new Color(1f, 0.6f, 0.5f, 0.7f), UiLine).rectTransform;
            dline.anchorMin = dline.anchorMax = new Vector2(0.5f, 0f); dline.sizeDelta = new Vector2(1300f, 6f); dline.anchoredPosition = new Vector2(0f, 300f);
            var speaker = CATestUIKit.Txt(box, "Speaker", "수호룡", 34, CATestUIKit.Accent, TextAnchor.MiddleLeft, true);
            speaker.rectTransform.anchorMin = speaker.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            speaker.rectTransform.sizeDelta = new Vector2(1200f, 50f);
            speaker.rectTransform.anchoredPosition = new Vector2(0f, 262f);
            var body = CATestUIKit.Txt(box, "Body", "", 34, CATestUIKit.TextColor, TextAnchor.UpperLeft);
            body.rectTransform.anchorMin = body.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            body.rectTransform.sizeDelta = new Vector2(1200f, 110f);
            body.rectTransform.anchoredPosition = new Vector2(0f, 185f);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.lineSpacing = 1.25f;
            var mark = CATestUIKit.Txt(box, "Next", "▼", 26, CATestUIKit.Accent, TextAnchor.MiddleCenter);
            mark.rectTransform.anchorMin = mark.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            mark.rectTransform.sizeDelta = new Vector2(40f, 40f);
            mark.rectTransform.anchoredPosition = new Vector2(600f, 140f);
            // 선택지 창은 화면 왼쪽 위(플레이어 쪽)에 — 오른쪽의 용 머리를 가리지 않도록
            var choices = CATestUIKit.Rect(dCanvas, "Choices", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-440f, 150f), new Vector2(760f, 260f));
            var cg = CATestUIKit.Group(choices.gameObject, false);
            var cbg = CATestUIKit.Img(choices, "Panel", Color.white, UiPanel, true);
            Fill(cbg.rectTransform);
            var cbtns = new List<Button>();
            var ctexts = new List<Text>();
            for (var i = 0; i < 3; i++) {
                var b = CATestUIKit.MenuButton(choices, "Choice_" + i, "선택지", new Vector2(660f, 60f), 32, UiDiamond, UiLine);
                b.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 70f - i * 70f);
                cbtns.Add(b);
                ctexts.Add(b.transform.Find("Label").GetComponent<Text>());
            }
            CATestUIKit.ChainVertical(cbtns);
            var dlg = box.gameObject.AddComponent<CATestDialogueBox>();
            var boxGroup = CATestUIKit.Group(box.gameObject, false);
            Set(dlg, "group", boxGroup);
            Set(dlg, "speaker", speaker);
            Set(dlg, "body", body);
            Set(dlg, "nextMark", mark);
            Set(dlg, "choiceGroup", cg);
            Set(dlg, "choiceButtons", cbtns.Cast<Object>().ToArray());
            Set(dlg, "choiceTexts", ctexts.Cast<Object>().ToArray());

            // 2) 엔딩 화면
            var eCanvas = CATestUIKit.Canvas(parent, "CATest_EndingCanvas", 500).transform;
            var card = CATestUIKit.Stretch(eCanvas, "EndingCard");
            var ecg = CATestUIKit.Group(card.gameObject, false);
            var ebg = CATestUIKit.Img(card, "Background", Color.black);
            Fill(ebg.rectTransform);
            ebg.raycastTarget = true;
            var elabel = CATestUIKit.Txt(card, "Label", "ENDING", 30, CATestUIKit.Accent, TextAnchor.MiddleCenter);
            elabel.rectTransform.sizeDelta = new Vector2(900f, 50f); elabel.rectTransform.anchoredPosition = new Vector2(0f, 250f);
            var etitle = CATestUIKit.Txt(card, "Title", "", 72, CATestUIKit.TextColor, TextAnchor.MiddleCenter, true);
            etitle.rectTransform.sizeDelta = new Vector2(1400f, 100f); etitle.rectTransform.anchoredPosition = new Vector2(0f, 170f);
            var elines = new List<Text>();
            for (var i = 0; i < 3; i++) {
                var t = CATestUIKit.Txt(card, "Line_" + i, "", 32, CATestUIKit.TextColor, TextAnchor.MiddleCenter);
                t.rectTransform.sizeDelta = new Vector2(1500f, 50f); t.rectTransform.anchoredPosition = new Vector2(0f, 30f - i * 64f);
                elines.Add(t);
            }
            var erec = CATestUIKit.Txt(card, "Record", "", 26, CATestUIKit.DimText, TextAnchor.MiddleCenter);
            erec.rectTransform.sizeDelta = new Vector2(600f, 40f); erec.rectTransform.anchoredPosition = new Vector2(0f, -250f);
            var efoot = CATestUIKit.Txt(card, "Footer", "", 24, CATestUIKit.DimText, TextAnchor.MiddleCenter);
            efoot.rectTransform.sizeDelta = new Vector2(600f, 40f); efoot.rectTransform.anchoredPosition = new Vector2(0f, -380f);
            var ec = card.gameObject.AddComponent<CATestEndingCard>();
            Set(ec, "group", ecg);
            Set(ec, "background", ebg);
            Set(ec, "label", elabel);
            Set(ec, "title", etitle);
            Set(ec, "lines", elines.Cast<Object>().ToArray());
            Set(ec, "record", erec);
            Set(ec, "footer", efoot);

            // 3) ESC 메뉴 (+ 설정 창, 확인 창)
            var pCanvas = CATestUIKit.Canvas(parent, "CATest_PauseCanvas", 600).transform;
            var menu = CATestUIKit.Stretch(pCanvas, "PauseMenu");
            var mg = CATestUIKit.Group(menu.gameObject, false);
            var mdim = CATestUIKit.Img(menu, "Dim", new Color(0.03f, 0.02f, 0.06f, 0.55f));
            Fill(mdim.rectTransform);
            mdim.raycastTarget = true;
            var grad = CATestUIKit.Img(menu, "LeftShade", new Color(0f, 0f, 0f, 0.75f), UiGradLeft).rectTransform;
            grad.anchorMin = new Vector2(0f, 0f); grad.anchorMax = new Vector2(0f, 1f); grad.pivot = new Vector2(0f, 0.5f);
            grad.sizeDelta = new Vector2(900f, 0f); grad.anchoredPosition = Vector2.zero;
            var ptitle = CATestUIKit.Txt(menu, "Title", "일시정지", 60, CATestUIKit.TextColor, TextAnchor.MiddleLeft, true);
            var ptr = ptitle.rectTransform;
            ptr.anchorMin = ptr.anchorMax = new Vector2(0f, 0.5f); ptr.pivot = new Vector2(0f, 0.5f);
            ptr.sizeDelta = new Vector2(600f, 80f); ptr.anchoredPosition = new Vector2(160f, 210f);
            var pl = CATestUIKit.Img(menu, "Line", new Color(1f, 0.8f, 0.7f, 0.6f), UiLine).rectTransform;
            pl.anchorMin = pl.anchorMax = new Vector2(0f, 0.5f); pl.pivot = new Vector2(0f, 0.5f);
            pl.sizeDelta = new Vector2(460f, 6f); pl.anchoredPosition = new Vector2(150f, 160f);
            var resume = LeftMenuButton(menu, "Resume", "계속하기", 90f);
            var settingsB = LeftMenuButton(menu, "Settings", "설정", 20f);
            var toTitle = LeftMenuButton(menu, "ToTitle", "타이틀로", -50f);
            var quit = LeftMenuButton(menu, "Quit", "게임 종료", -120f);
            CATestUIKit.ChainVertical(new List<Button> { resume, settingsB, toTitle, quit });
            var place = CATestUIKit.Txt(menu, "Place", "", 24, CATestUIKit.DimText, TextAnchor.MiddleLeft);
            var plr = place.rectTransform;
            plr.anchorMin = plr.anchorMax = new Vector2(0f, 0f); plr.pivot = new Vector2(0f, 0f);
            plr.sizeDelta = new Vector2(700f, 40f); plr.anchoredPosition = new Vector2(160f, 90f);
            var hint = CATestUIKit.Txt(menu, "KeysHint", "ESC 닫기   ·   ↑↓ 선택   ·   Enter 확인", 22, CATestUIKit.DimText, TextAnchor.MiddleRight);
            var hr = hint.rectTransform;
            hr.anchorMin = hr.anchorMax = new Vector2(1f, 0f); hr.pivot = new Vector2(1f, 0f);
            hr.sizeDelta = new Vector2(700f, 40f); hr.anchoredPosition = new Vector2(-80f, 90f);
            var settingsPanel = BuildSettingsPanel(pCanvas);
            var confirm = BuildConfirm(pCanvas);
            var pm = menu.gameObject.AddComponent<CATestPauseMenu>();
            Set(pm, "group", mg);
            Set(pm, "resume", resume);
            Set(pm, "settings", settingsB);
            Set(pm, "toTitle", toTitle);
            Set(pm, "quit", quit);
            Set(pm, "placeText", place);
            Set(pm, "settingsPanel", settingsPanel);
            Set(pm, "confirm", confirm);
        }

        // ───────────────────────── 타이틀 씬 ─────────────────────────
        public static void BuildTitleScene() {
            EnsureFolder(ScenesDir);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var rnd = new Random(77);
            var world = new GameObject("TitleWorld").transform;

            // 카메라 (게임과 같은 원근/FOV)
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.position = new Vector3(0f, 6.5f, -112f);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = false;
            cam.fieldOfView = 12.9f;
            cam.farClipPlane = 5000f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = TwBg;
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<CATestTitleCameraDrift>();

            var gl = new GameObject("Global Light 2D");
            gl.transform.SetParent(world, false);
            var light = gl.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 0.85f;
            light.color = C(0.68f, 0.62f, 0.88f);
            ApplyAllSortingLayers(light);

            // 배경: 일식 하늘 + 먼 산 + 모래 언덕 + 거울 물 + 휘어진 바위 + 흰 기둥
            var skyGo = new GameObject("TitleSky");
            skyGo.transform.SetParent(world, false);
            skyGo.transform.position = new Vector3(0f, 6.5f, 800f);
            var sky = skyGo.AddComponent<SpriteRenderer>();
            sky.sprite = WhiteSprite;
            sky.sharedMaterial = MatSky;
            sky.sortingLayerID = SL("BackGround");
            sky.sortingOrder = -500;
            var ss = WhiteSprite.bounds.size;
            skyGo.transform.localScale = new Vector3(760f / ss.x, 400f / ss.y, 1f);
            var dome = skyGo.AddComponent<CATestSkyDome>();
            Set(dome, "areaMinX", -400f);
            Set(dome, "areaMaxX", 400f);
            Set(dome, "baseY", 6.5f);
            Set(dome, "eclipseUV", new Vector2(0.607f, 0.62f));

            BuildPlainLayers(world, world, rnd, -200f, 200f);
            var horns = new[] { Sp(TWA + "TW_HornRock_A.png"), Sp(TWA + "TW_HornRock_B.png"), Sp(TWA + "TW_HornRock_C.png") };
            (float x, float y, float z, float s, bool f, int h)[] hs = {
                (26f, -6f, 40f, 2.6f, true, 0), (40f, -4f, 60f, 3.4f, true, 1), (-8f, -8f, 50f, 2.2f, false, 2), (14f, -10f, 25f, 1.8f, true, 2),
            };
            foreach (var h in hs) {
                var hsp = horns[h.h];
                if (hsp == null) continue;
                Put(world, hsp, new Vector3(h.x, -3f - ParallaxSink(h.z) - hsp.bounds.min.y * h.s, h.z), h.s, "Default", -30, Color.Lerp(TwBone, TwHaze, h.z / 70f), h.f);
            }
            var pillar = Sp(TWA + "TW_Pillar.png");
            var pillarB = Sp(TWA + "TW_PillarBroken.png");
            foreach (var (x, z, b) in new[] { (-4f, 14f, false), (3f, 20f, true), (9f, 16f, false), (18f, 30f, true) })
            {
                var ps = b ? pillarB : pillar;
                Put(world, ps, new Vector3(x, -1.5f - ps.bounds.min.y * 1.1f, z), 1.1f, "Default", -20, Color.Lerp(TwBone, TwHaze, z / 40f));
            }
            // 가까운 검은 모래 언덕(전경)
            var fd = Sp(TWD + "Foreground dune.png", "Foreground dune_1");
            PutTop(world, fd, 4f, -0.5f, -8f, 1.3f, "Architecture", 50, C(0.06f, 0.045f, 0.1f));
            Motes(world, "Ash", new Rect(-40, 0, 80, 40), 1f, MatAddDot, C(1f, 0.9f, 0.9f, 0.45f), C(0.8f, 0.8f, 1f, 0.3f), 30f, 0.03f, 0.08f,
                new Vector2(-0.4f, 0.12f), 0.5f, 10f, "Player", 40);
            var prof = AssetDatabase.LoadAssetAtPath<VolumeProfile>(SettingsDir + "/VP_CATest_Twilight.asset");
            if (prof != null) {
                var vgo = new GameObject("Volume");
                vgo.transform.SetParent(world, false);
                var v = vgo.AddComponent<Volume>();
                v.isGlobal = true;
                v.sharedProfile = prof;
            }

            // EventSystem (새 Input System 용 UI 모듈)
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            // UI
            var canvas = CATestUIKit.Canvas(null, "TitleCanvas", 100).transform;
            var shade = CATestUIKit.Img(canvas, "LeftShade", new Color(0.01f, 0f, 0.03f, 0.8f), UiGradLeft).rectTransform;
            shade.anchorMin = new Vector2(0f, 0f); shade.anchorMax = new Vector2(0f, 1f); shade.pivot = new Vector2(0f, 0.5f);
            shade.sizeDelta = new Vector2(1000f, 0f);

            var titleRt = CATestUIKit.Rect(canvas, "TitleGroup", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(900f, 400f));
            var tg = titleRt.gameObject.AddComponent<CanvasGroup>();
            var t1 = CATestUIKit.Txt(titleRt, "GameTitle", GameTitle, 110, CATestUIKit.TextColor, TextAnchor.MiddleLeft, true);
            t1.rectTransform.anchorMin = t1.rectTransform.anchorMax = new Vector2(0f, 0.5f); t1.rectTransform.pivot = new Vector2(0f, 0.5f);
            t1.rectTransform.sizeDelta = new Vector2(900f, 140f); t1.rectTransform.anchoredPosition = new Vector2(150f, 300f);
            var t2 = CATestUIKit.Txt(titleRt, "Subtitle", GameSubtitle, 26, CATestUIKit.Accent, TextAnchor.MiddleLeft);
            t2.rectTransform.anchorMin = t2.rectTransform.anchorMax = new Vector2(0f, 0.5f); t2.rectTransform.pivot = new Vector2(0f, 0.5f);
            t2.rectTransform.sizeDelta = new Vector2(700f, 40f); t2.rectTransform.anchoredPosition = new Vector2(156f, 222f);
            var tl = CATestUIKit.Img(titleRt, "Line", new Color(1f, 0.8f, 0.7f, 0.7f), UiLine).rectTransform;
            tl.anchorMin = tl.anchorMax = new Vector2(0f, 0.5f); tl.pivot = new Vector2(0f, 0.5f);
            tl.sizeDelta = new Vector2(520f, 6f); tl.anchoredPosition = new Vector2(150f, 190f);

            var menuRt = CATestUIKit.Stretch(canvas, "Menu");
            var mg = menuRt.gameObject.AddComponent<CanvasGroup>();
            var cont = LeftMenuButton(menuRt, "Continue", "이어하기", 90f);
            var contSub = CATestUIKit.Txt(cont.transform, "Sub", "", 22, CATestUIKit.DimText, TextAnchor.MiddleLeft);
            contSub.rectTransform.anchorMin = contSub.rectTransform.anchorMax = new Vector2(0f, 0.5f); contSub.rectTransform.pivot = new Vector2(0f, 0.5f);
            contSub.rectTransform.sizeDelta = new Vector2(400f, 30f); contSub.rectTransform.anchoredPosition = new Vector2(230f, -2f);
            var newG = LeftMenuButton(menuRt, "NewGame", "새로 시작", 20f);
            var setB = LeftMenuButton(menuRt, "Settings", "설정", -50f);
            var resetB = LeftMenuButton(menuRt, "ResetProgress", "진행도 초기화", -120f);
            var quitB = LeftMenuButton(menuRt, "Quit", "게임 종료", -190f);
            CATestUIKit.ChainVertical(new List<Button> { cont, newG, setB, resetB, quitB });

            var ecount = CATestUIKit.Txt(menuRt, "EndingCount", "엔딩 기록  0 / 3", 24, CATestUIKit.DimText, TextAnchor.MiddleLeft);
            var ecr = ecount.rectTransform;
            ecr.anchorMin = ecr.anchorMax = new Vector2(0f, 0f); ecr.pivot = new Vector2(0f, 0f);
            ecr.sizeDelta = new Vector2(500f, 34f); ecr.anchoredPosition = new Vector2(156f, 150f);
            var marks = new List<Image>();
            var names = new List<Text>();
            for (var i = 0; i < 3; i++) {
                var m = CATestUIKit.Img(menuRt, "EndingMark_" + i, Color.white, UiRing);
                var mr = m.rectTransform;
                mr.anchorMin = mr.anchorMax = new Vector2(0f, 0f);
                mr.sizeDelta = new Vector2(26f, 26f); mr.anchoredPosition = new Vector2(170f + i * 250f, 115f);
                marks.Add(m);
                var n = CATestUIKit.Txt(menuRt, "EndingName_" + i, "???", 22, CATestUIKit.DimText, TextAnchor.MiddleLeft);
                var nr = n.rectTransform;
                nr.anchorMin = nr.anchorMax = new Vector2(0f, 0f); nr.pivot = new Vector2(0f, 0.5f);
                nr.sizeDelta = new Vector2(220f, 30f); nr.anchoredPosition = new Vector2(192f + i * 250f, 115f);
                names.Add(n);
            }
            var ver = CATestUIKit.Txt(canvas, "Version", "LHS_CATest", 20, new Color(1f, 1f, 1f, 0.35f), TextAnchor.LowerRight);
            var vr = ver.rectTransform;
            vr.anchorMin = vr.anchorMax = new Vector2(1f, 0f); vr.pivot = new Vector2(1f, 0f);
            vr.sizeDelta = new Vector2(400f, 30f); vr.anchoredPosition = new Vector2(-40f, 30f);

            var settingsPanel = BuildSettingsPanel(canvas);
            var confirm = BuildConfirm(canvas);
            var black = CATestUIKit.Img(canvas, "BlackCover", Color.black);
            Fill(black.rectTransform);

            var ts = canvas.gameObject.AddComponent<CATestTitleScreen>();
            Set(ts, "titleGroup", tg);
            Set(ts, "menuGroup", mg);
            Set(ts, "blackCover", black);
            Set(ts, "continueBtn", cont);
            Set(ts, "continueSub", contSub);
            Set(ts, "newGameBtn", newG);
            Set(ts, "settingsBtn", setB);
            Set(ts, "resetBtn", resetB);
            Set(ts, "quitBtn", quitB);
            Set(ts, "endingMarks", marks.Cast<Object>().ToArray());
            Set(ts, "endingNames", names.Cast<Object>().ToArray());
            Set(ts, "endingCount", ecount);
            Set(ts, "markSeen", UiDiamond);
            Set(ts, "markUnseen", UiRing);
            Set(ts, "settingsPanel", settingsPanel);
            Set(ts, "confirm", confirm);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, TitleScenePath);
        }
    }
}
#endif
