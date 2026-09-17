using UnityEngine;
using _02._Script.Players;
using _02._Script.Players.Interface;

namespace _02._Script.Interaction
{
    public abstract class InteractBase : MonoBehaviour, IInteractable
    {
        [SerializeField] protected SpriteRenderer targetRenderer;
        [SerializeField] private Material outlineMaterial;

        private Material _originalMaterial;
        private bool _highlighted;

        public void SetHighlight(bool highlight)
        {
            if (targetRenderer == null) return;
            if (_highlighted == highlight) return;

            if (highlight)
            {
                if (outlineMaterial == null) return;

                _originalMaterial = targetRenderer.sharedMaterial;
                targetRenderer.sharedMaterial = outlineMaterial;
            }
            else
            {
                targetRenderer.sharedMaterial = _originalMaterial;
            }

            _highlighted = highlight;
        }

        protected virtual void OnDisable()
        {
            SetHighlight(false);
        }

        public abstract void Interact(Player owner);
    }
}