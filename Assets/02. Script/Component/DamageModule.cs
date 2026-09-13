using System;
using UnityEngine;

namespace _02._Script.Component {
    public sealed class DamageModule : MonoBehaviour {
        private void OnTriggerEnter2D(Collider2D other) {
            OnDamaged?.Invoke();
        }

        public event Action OnDamaged;
    }
}