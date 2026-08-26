using System;
using _01.Script.Player.Interface;
using UnityEngine;

namespace _01.Script.Player.Components
{
    public class Interactor : MonoBehaviour, IAgentModule {
        [SerializeField] private float radius;
        [SerializeField] private Vector2 offset;
        [SerializeField] private ContactFilter2D target;
        [SerializeField] private bool debug;

        private Vector2 Offset => offset + (Vector2)transform.position;
        private Collider2D[] _results;
        
        public void Interact(Player owner)
        {
            int count = Physics2D.OverlapCircle(Offset, radius, target, _results);
            if (count <= 0) return;
            float distance = float.MaxValue;
            IInteractable _currentInteractable = null;
            for (int i = 0; i < count; i++)
            {
                Collider2D collider = _results[i];
                if (collider.TryGetComponent<IInteractable>(out var interactable))
                {
                    float _currentDistance = (collider.transform.position - transform.position).sqrMagnitude;
                    if (_currentDistance <= distance)
                    {
                        distance = _currentDistance;
                        _currentInteractable = interactable;
                    }
                }
            }

            _currentInteractable?.Interact(owner);
        }

        private void OnDrawGizmos()
        {
            if (!debug) return;
            Gizmos.color = Color.deepSkyBlue;
            Gizmos.DrawWireSphere(Offset, radius);
        }

        public void Initialize(Agent owner) {
            _results = new Collider2D[10];
        }

        public Type Type => typeof(IInteractor);
    }
    
}