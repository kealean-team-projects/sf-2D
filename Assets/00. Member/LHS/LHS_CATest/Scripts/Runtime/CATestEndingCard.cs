using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace LHS_CATest {
    /// <summary>
    /// 엔딩 화면 (UGUI). 검은 화면 위에 "ENDING A" → 엔딩 제목 → 후일담 문장들이 한 줄씩 떠오르고,
    /// 엔딩 수집 현황을 보여 준 뒤 아무 키나 누르면 타이틀로.
    /// </summary>
    public sealed class CATestEndingCard : MonoBehaviour {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Image background;
        [SerializeField] private Text label;
        [SerializeField] private Text title;
        [SerializeField] private Text[] lines;
        [SerializeField] private Text record;
        [SerializeField] private Text footer;

        private void Awake() => CATestUIKit.Show(group, false);

        public async UniTask Play(string endingLabel, string endingTitle, string[] epilogue, Color bgColor, bool bad) {
            if (group == null) return;
            CATestUIKit.Show(group, true);
            group.alpha = 1f;
            if (background != null) background.color = bgColor;
            // 밝은 배경(귀환 엔딩의 흰 빛)이면 글자를 어둡게
            var ink = bgColor.grayscale > 0.6f ? new Color(0.2f, 0.17f, 0.28f, 0f) : new Color(0.95f, 0.93f, 0.98f, 0f);
            foreach (var g in new Graphic[] { title, record, footer }) if (g != null) g.color = ink;
            foreach (var l in lines) if (l != null) l.color = ink;
            SetAlpha(label, 0f);
            if (label != null) { label.text = endingLabel; label.color = bad ? new Color(1f, 0.45f, 0.42f, 0f) : new Color(1f, 0.8f, 0.62f, 0f); }
            if (title != null) title.text = endingTitle;
            for (var i = 0; i < lines.Length; i++) if (lines[i] != null) lines[i].text = epilogue != null && i < epilogue.Length ? epilogue[i] : "";
            // 배경을 서서히 원하는 색으로(검정/흰색 등) — 이미 화면이 덮여 있으므로 바로 시작
            await CATestCutscene.Wait(0.8f);
            CATestAudio.PlaySfx("ending_card");
            await FadeText(label, 1.2f);
            await CATestCutscene.Wait(0.3f);
            await FadeText(title, 1.4f);
            await CATestCutscene.Wait(1.0f);
            foreach (var l in lines) {
                if (l == null || string.IsNullOrEmpty(l.text)) continue;
                await FadeText(l, 1.3f);
                await CATestCutscene.Wait(1.1f);
            }
            if (record != null) record.text = $"엔딩 기록  {CATestSave.EndingsSeenCount} / {CATestSave.EndingCount}";
            await FadeText(record, 0.8f);
            await CATestCutscene.Wait(0.8f);
            if (footer != null) footer.text = "아무 키나 눌러 타이틀로";
            await FadeText(footer, 0.6f);
            while (this != null && !CATestUIInput.AnyKeyPressed()) {
                if (footer != null) SetAlpha(footer, 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 2.5f));
                await UniTask.Yield();
            }
        }

        private static void SetAlpha(Graphic g, float a) {
            if (g == null) return;
            var c = g.color; c.a = a; g.color = c;
        }

        private static async UniTask FadeText(Graphic g, float time) {
            if (g == null) return;
            for (var t = 0f; t < time; t += Time.unscaledDeltaTime) {
                if (g == null) return;
                SetAlpha(g, t / time);
                await UniTask.Yield();
            }
            SetAlpha(g, 1f);
        }
    }
}
