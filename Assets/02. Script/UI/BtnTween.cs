using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _02._Script.UI {
    public class BtnTween : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
        private RectTransform _rt;
        private Tween _sizeTween;
        private Tween _tween;
        private TMP_Text _txt;

        private void Awake() {
            _rt = GetComponent<RectTransform>();
            _txt = GetComponentInChildren<TMP_Text>();
        }

        public void OnPointerEnter(PointerEventData eventData) {
            _tween = Tween.UISizeDelta(_rt, new Vector2(300, 84), 0.4f, Ease.OutBack);
            _sizeTween = Tween.Custom(_txt, _txt.fontSize, 36f, 0.4f, (text, val) => text.fontSize = val, Ease.OutBack);
        }

        public void OnPointerExit(PointerEventData eventData) {
            _tween.Stop();
            _sizeTween.Stop();
            _tween = Tween.UISizeDelta(_rt, new Vector2(250, 70), 0.2f, Ease.OutQuart);
            _sizeTween = Tween.Custom(_txt, _txt.fontSize, 30f, 0.2f, (text, val) => text.fontSize = val, Ease.OutBack);
        }
    }
}