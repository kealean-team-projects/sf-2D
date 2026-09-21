using _02._Script._01_Players;
using UnityEngine;

namespace _02._Script._04_Interaction
{
    public class InteractLight : InteractBase
    {
        [SerializeField] private GameObject lightPrefab;
        private bool isActive;

        public bool IsActive => isActive;

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