using System;
using UnityEngine;

namespace _02._Script.Boss {
    [RequireComponent(typeof(Collider2D))]
    public class BossTrigger : MonoBehaviour {
        private void OnTriggerEnter2D(Collider2D other) {
            if (other.CompareTag("Player"))
                OnEnter?.Invoke();
        }

        public event Action OnEnter;
    }
}