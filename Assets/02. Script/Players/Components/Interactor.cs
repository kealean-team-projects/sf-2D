using System;
using _02._Script.Interaction;
using _02._Script.Players.Interface;
using UnityEngine;

namespace _02._Script.Players.Components {
    public class Interactor : MonoBehaviour, IAgentModule, IInteractor {
        [SerializeField] private float radius;
        [SerializeField] private Vector2 offset;
        [SerializeField] private ContactFilter2D target;
        [SerializeField] private bool debug;

        private IInteractable _currentTarget;
        private Collider2D[] _results;

        private Vector2 Offset => offset + (Vector2)transform.position;

        
        private void Update()
        {
            if (_results == null) return;
            FindTarget();
        }

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
            
            _currentTarget?.Interact(owner);
        }
        
        private void FindTarget()
        {
            var count = Physics2D.OverlapCircle(Offset, radius, target, _results);
            
            if (count <= 0)
            {
                SelectOutline(null);
                return;
            }
            
            var distance = float.MaxValue;
            IInteractable currentInteractable = null;
            for (var i = 0; i < count; i++) {
                var collider = _results[i];

                if (!collider.TryGetComponent<IInteractable>(out var interactable)) continue;
                if (interactable is Behaviour behaviour && !behaviour.isActiveAndEnabled) continue;
                var currentDistance = (collider.transform.position - transform.position).sqrMagnitude;

                if (!(currentDistance <= distance)) continue;
                distance = currentDistance;
                currentInteractable = interactable;
            }

            SelectOutline(currentInteractable);
        }

        private void SelectOutline(IInteractable next)
        {
            // 같은 대상이면 바꿀 필요가 없습니다.
            if (ReferenceEquals(_currentTarget, next)) return;

            // 이전 대상의 테두리를 끕니다.
            if (_currentTarget is InteractBase previous && previous != null)
            {
                previous.SetHighlight(false);
            }

            // 새 대상을 저장합니다.
            _currentTarget = next;

            // 새 대상의 테두리를 켭니다.
            if (_currentTarget is InteractBase current && current != null)
            {
                current.SetHighlight(true);
            }
        }
        
        private void OnDisable()
        {
            SelectOutline(null);
        }
    }
}