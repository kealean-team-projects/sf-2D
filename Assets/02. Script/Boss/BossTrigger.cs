using System;
using UnityEngine;
using _02._Script._01_Players;

namespace _02._Script.Boss {
    [RequireComponent(typeof(Collider2D))]
    public class BossTrigger : MonoBehaviour {
        private void OnTriggerEnter2D(Collider2D other) {
            var player = other.GetComponentInParent<Player>();
            if (player != null && !player.IsDead)
                OnEnter?.Invoke();
        }

        public event Action OnEnter;
    }
}
