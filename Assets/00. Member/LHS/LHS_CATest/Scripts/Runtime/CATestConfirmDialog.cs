using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LHS_CATest {
    /// <summary>
    /// "정말 ~할까요?" 확인 창 (UGUI). 진행도 초기화 / 새로 시작(기존 기록 덮어쓰기) / 타이틀로 가기에서 사용.
    /// Ask(문구, 확인 버튼 글자, 확인 시 할 일) → [확인] 누르면 onYes 실행, [취소]나 ESC 는 그냥 닫힘.
    /// 기본 선택은 [취소] — 실수로 Enter 를 눌러 기록이 지워지지 않게.
    /// </summary>
    [DefaultExecutionOrder(-70)] // 가장 위에 뜨는 창이므로 ESC 를 가장 먼저 받는다
    public sealed class CATestConfirmDialog : MonoBehaviour {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Text message;
        [SerializeField] private Button yes;
        [SerializeField] private Text yesLabel;
        [SerializeField] private Button no;

        private Action _onYes;
        private Action _onClosed;
        public bool IsOpen { get; private set; }

        private void Awake() {
            CATestUIKit.Show(group, false);
            if (yes != null) yes.onClick.AddListener(() => { var a = _onYes; Hide(); a?.Invoke(); });
            if (no != null) no.onClick.AddListener(Hide);
        }

        public void Ask(string text, string yesText, Action onYes, Action onClosed = null) {
            _onYes = onYes;
            _onClosed = onClosed;
            if (message != null) message.text = text;
            if (yesLabel != null) yesLabel.text = yesText;
            IsOpen = true;
            CATestUIKit.Show(group, true);
            if (EventSystem.current != null && no != null) EventSystem.current.SetSelectedGameObject(no.gameObject);
        }

        public void Hide() {
            if (!IsOpen) return;
            IsOpen = false;
            CATestUIKit.Show(group, false);
            var c = _onClosed;
            _onClosed = null;
            c?.Invoke();
        }

        private void Update() {
            if (!IsOpen) return;
            if (CATestUIInput.ConsumeEscape()) Hide();
        }
    }
}
