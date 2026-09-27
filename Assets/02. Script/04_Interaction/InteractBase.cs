using _02._Script._01_Players;
using _02._Script._01_Players.Interface;
using UnityEngine;

namespace _02._Script._04_Interaction {
    public abstract class InteractBase : MonoBehaviour, IInteractable {
        [SerializeField] protected SpriteRenderer targetRenderer;
        [SerializeField] private Material outlineMaterial;
        private bool _highlighted;

        private Material _originalMaterial;

        protected virtual void OnDisable() {
            SetHighlight(false);
        }

        public abstract void Interact(Player owner);

        public void SetHighlight(bool highlight) {
            if (targetRenderer == null) return;
            if (_highlighted == highlight) return;

            if (highlight) {
                if (outlineMaterial == null) return;

                _originalMaterial = targetRenderer.sharedMaterial;
                targetRenderer.sharedMaterial = outlineMaterial;
            }
            else {
                targetRenderer.sharedMaterial = _originalMaterial;
            }

            _highlighted = highlight;
        }
    }
}