using System;
using TMPro;
using UnityEngine;

namespace _02._Script.UI {
    public class StaminaHUD : MonoBehaviour {
        [SerializeField] private TextMeshProUGUI stamina;

        public void UpdateStamina(float value) {
            stamina.SetText($"Stamina: {value}");
        }
    }
}