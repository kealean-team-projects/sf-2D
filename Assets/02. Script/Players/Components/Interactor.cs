using System;
using _02._Script.Players.Interface;
using UnityEngine;

namespace _02._Script.Players.Components {
    public class Interactor : MonoBehaviour, IAgentModule, IInteractor {
        [SerializeField] private float radius;
        [SerializeField] private Vector2 offset;
        [SerializeField] private ContactFilter2D target;
        [SerializeField] private bool debug;
        private Collider2D[] _results;

        private Vector2 Offset => offset + (Vector2)transform.position;

        private void OnDrawGizmos() {
            if (!debug) return;
            Gizmos.color = Color.deepSkyBlue;
            Gizmos.DrawWireSphere(Offset, radius);
        }

        public void Initialize(Agent owner) {
            _results = new Collider2D[10];
        }

        public Type Type => typeof(IInteractor);

        public void Interact(Player owner) {
            var count = Physics2D.OverlapCircle(Offset, radius, target, _results);
            if (count <= 0) return;
            var distance = float.MaxValue;
            IInteractable currentInteractable = null;
            for (var i = 0; i < count; i++) {
                var collider = _results[i];

                if (!collider.TryGetComponent<IInteractable>(out var interactable)) continue;
                var currentDistance = (collider.transform.position - transform.position).sqrMagnitude;

                if (!(currentDistance <= distance)) continue;
                distance = currentDistance;
                currentInteractable = interactable;
            }

            currentInteractable?.Interact(owner);
        }
    }
}