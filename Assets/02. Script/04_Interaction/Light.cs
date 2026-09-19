using _02._Script._01_Players;
using UnityEngine;

namespace _02._Script._04_Interaction {
    public class Light : InteractBase {
        [SerializeField] private GameObject lightPrefab;
        private bool isActive;
        public override void Interact(Player owner) {
            Debug.Log("빛");
            isActive = !isActive;
            targetRenderer.color = isActive ? Color.white : Color.gray;
            lightPrefab.SetActive(isActive);
        }
    }
}
