using PrimeTween;
using TMPro;
using UnityEngine;

namespace _02._Script.UI {
    public class StaminaHUD : MonoBehaviour {
        [SerializeField] private TextMeshProUGUI stamina;

        private RectTransform rectTransform;

        private void OnEnable() {
            rectTransform = GetComponent<RectTransform>();
            Tween.UIAnchoredPosition(rectTransform, new Vector3(0, 0), 1f, Ease.OutQuart);
        }

        public void UpdateStamina(float value) {
            stamina.SetText($"Stamina: {value}");
        }
    }
}