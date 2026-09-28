using UnityEngine;

namespace _02._Script._01_Players.Components {
    // Imported PSB rigs contain one renderer per body part.
    [DisallowMultipleComponent]
    public sealed class PlayerVisualMaterial : MonoBehaviour {
        [SerializeField] private Material material;

        private void Awake() {
            if (material == null) return;
            foreach (var sprite in GetComponentsInChildren<SpriteRenderer>(true))
                sprite.sharedMaterial = material;
        }
    }
}