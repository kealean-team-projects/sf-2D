using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LHS_CATest {
    /// <summary>
    /// 용과의 대화창 + 선택지 (UGUI). 엔딩 장면 전용.
    ///
    /// ■ Say(이름, 문장): 화면 아래 대화창에 이름과 문장을 한 글자씩(초당 charsPerSecond) 띄운다.
    ///   진행 키(E / Enter / Space / 클릭)를 누르면 → 타이핑 중이면 전체 문장 즉시 표시, 다 나왔으면 다음으로.
    ///   "▼" 표시가 깜빡이면 넘길 수 있다는 뜻.
    /// ■ Choose(선택지들): 선택지 버튼을 보여 주고 고를 때까지 기다린 뒤 번호(0부터)를 돌려준다.
    ///   키보드 위/아래(W/S, 방향키) + Enter/Space, 또는 마우스 클릭.
    /// ■ 머리 위 대사(플레이어) 설정과 무관하게 항상 표시(엔딩 진행에 꼭 필요하므로).
    /// </summary>
    public sealed class CATestDialogueBox : MonoBehaviour {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Text speaker;
        [SerializeField] private Text body;
        [SerializeField] private Graphic nextMark;
        [SerializeField] private CanvasGroup choiceGroup;
        [SerializeField] private Button[] choiceButtons;
        [SerializeField] private Text[] choiceTexts;
        [SerializeField] private float charsPerSecond = 22f;

        private int _chosen = -1;

        private void Awake() {
            CATestUIKit.Show(group, false);
            CATestUIKit.Show(choiceGroup, false);
            for (var i = 0; i < choiceButtons.Length; i++) {
                var idx = i;
                if (choiceButtons[i] != null) choiceButtons[i].onClick.AddListener(() => _chosen = idx);
            }
        }

        public async UniTask Say(string who, string line) {
            await Open();
            if (speaker != null) speaker.text = who;
            if (body != null) body.text = "";
            if (nextMark != null) nextMark.enabled = false;
            await UniTask.Yield(); // 이전 대사를 넘긴 키 입력이 이번 대사까지 넘기지 않도록 한 프레임 쉼
            var shown = 0f;
            var skipped = false;
            while (shown < line.Length) {
                if (this == null) return;
                if (CATestUIInput.SubmitPressed()) { skipped = true; break; }
                shown += Time.unscaledDeltaTime * charsPerSecond;
                if (body != null) body.text = line.Substring(0, Mathf.Min(line.Length, Mathf.FloorToInt(shown) + 1));
                await UniTask.Yield();
            }
            if (body != null) body.text = line;
            if (skipped) await UniTask.Yield();
            // 넘길 때까지 ▼ 깜빡임
            while (this != null && !CATestUIInput.SubmitPressed()) {
                if (nextMark != null) nextMark.enabled = Mathf.Repeat(Time.unscaledTime, 0.9f) < 0.6f;
                await UniTask.Yield();
            }
            if (nextMark != null) nextMark.enabled = false;
        }

        public async UniTask<int> Choose(string[] options) {
            await Open();
            _chosen = -1;
            for (var i = 0; i < choiceButtons.Length; i++) {
                var on = i < options.Length;
                if (choiceButtons[i] != null) choiceButtons[i].gameObject.SetActive(on);
                if (on && i < choiceTexts.Length && choiceTexts[i] != null) choiceTexts[i].text = options[i];
            }
            CATestUIKit.Show(choiceGroup, true);
            await UniTask.Yield();
            if (EventSystem.current != null && choiceButtons.Length > 0 && choiceButtons[0] != null)
                EventSystem.current.SetSelectedGameObject(choiceButtons[0].gameObject);
            // E 키로도 고를 수 있게(UI 기본 Submit 은 Enter/Space)
            while (this != null && _chosen < 0) {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null && kb.eKey.wasPressedThisFrame && EventSystem.current != null) {
                    var sel = EventSystem.current.currentSelectedGameObject;
                    for (var i = 0; i < choiceButtons.Length; i++) if (choiceButtons[i] != null && sel == choiceButtons[i].gameObject) _chosen = i;
                }
                await UniTask.Yield();
            }
            CATestUIKit.Show(choiceGroup, false);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            return _chosen;
        }

        private async UniTask Open() {
            if (group == null || group.alpha >= 0.99f) return;
            group.interactable = group.blocksRaycasts = true;
            for (var t = 0f; t < 0.3f; t += Time.unscaledDeltaTime) {
                group.alpha = t / 0.3f;
                await UniTask.Yield();
            }
            group.alpha = 1f;
        }

        public async UniTask Close() {
            if (group == null) return;
            for (var t = 0f; t < 0.3f; t += Time.unscaledDeltaTime) {
                group.alpha = 1f - t / 0.3f;
                await UniTask.Yield();
            }
            CATestUIKit.Show(group, false);
        }
    }
}
