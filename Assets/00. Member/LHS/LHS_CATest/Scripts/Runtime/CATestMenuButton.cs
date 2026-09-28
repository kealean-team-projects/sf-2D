using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LHS_CATest {
    /// <summary>
    /// 메뉴 버튼 반응: 선택(키보드) 또는 마우스를 올리면 → 글자가 강조색으로, 왼쪽 마름모와 밑줄이 서서히 나타나고 글자가 살짝 오른쪽으로.
    /// 마우스를 올리면 그 버튼을 "선택"으로 만들어 키보드/마우스 표시가 하나로 맞춰지게 한다.
    /// </summary>
    public sealed class CATestMenuButton : MonoBehaviour, IPointerEnterHandler, ISelectHandler, IDeselectHandler {
        [SerializeField] private Text label;
        [SerializeField] private Image marker;
        [SerializeField] private Image underline;
        [SerializeField] private float speed = 10f;

        private float _k;
        private bool _selected;
        private Vector2 _labelBase;
        private Button _button;

        public void Setup(Text l, Image m, Image u) {
            label = l; marker = m; underline = u;
        }

        private void Awake() {
            _button = GetComponent<Button>();
            if (label != null) _labelBase = label.rectTransform.anchoredPosition;
            Apply(0f);
        }

        private void OnEnable() {
            _selected = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
        }

        public void OnPointerEnter(PointerEventData e) {
            if (_button != null && _button.interactable && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(gameObject);
        }

        public void OnSelect(BaseEventData e) => _selected = true;
        public void OnDeselect(BaseEventData e) => _selected = false;

        private void Update() {
            var target = _selected ? 1f : 0f;
            if (Mathf.Approximately(_k, target)) {
                if (_button != null && label != null) label.color = Tint(_k);
                return;
            }
            _k = Mathf.MoveTowards(_k, target, Time.unscaledDeltaTime * speed);
            Apply(_k);
        }

        private Color Tint(float k) {
            var c = Color.Lerp(CATestUIKit.TextColor, CATestUIKit.Accent, k);
            if (_button != null && !_button.interactable) c = new Color(c.r, c.g, c.b, 0.35f);
            return c;
        }

        private void Apply(float k) {
            var e = k * k * (3f - 2f * k);
            if (label != null) {
                label.color = Tint(e);
                label.rectTransform.anchoredPosition = _labelBase + new Vector2(8f * e, 0f);
            }
            if (marker != null) {
                var c = marker.color; c.a = e; marker.color = c;
                marker.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.4f, 1f, e);
            }
            if (underline != null) {
                var c = underline.color; c.a = e * 0.8f; underline.color = c;
                underline.rectTransform.localScale = new Vector3(Mathf.Lerp(0.2f, 1f, e), 1f, 1f);
            }
        }
    }
}
