using _02._Script._01_Players;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace _02._Script._04_Interaction
{
    public class InteractLight : InteractBase
    {
        [SerializeField] private GameObject lightPrefab;
        [SerializeField, Min(0f)] private float normalIntensity = 1f;
        private Light2D targetLight;
        
        private bool isActive;
        public bool IsActive => isActive;

        private void Awake()
        {
            targetLight = lightPrefab.GetComponentInChildren<Light2D>(true);
            normalIntensity = targetLight.intensity;
        }

        public void SetBrightness(float ratio)
        {
            targetLight.intensity = normalIntensity * Mathf.Clamp01(ratio);
        }

        public override void Interact(Player owner)
        {
            SetLightActive(!isActive);
        }

        public void TurnOn() => SetLightActive(true);
        public void TurnOff() => SetLightActive(false);

        private void SetLightActive(bool active)
        {
            isActive = active;
            targetRenderer.color = active ? Color.white : Color.gray;
            lightPrefab.SetActive(active);
        }
    }
}