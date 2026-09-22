using _02._Script._01_Players;
using UnityEngine;

namespace _02._Script._03_TrapAndEnemy.Traps {
    public enum DamageType {
        None,
        Damage
    }

    [RequireComponent(typeof(Collider2D))]
    public abstract class TrapBase : MonoBehaviour {
        private void OnTriggerEnter2D(Collider2D other) {
            var player = other.GetComponentInParent<Player>();
            if (player == null) return;

            OnPlayerEnter(player);
        }

        private void OnTriggerExit2D(Collider2D other) {
            var player = other.GetComponentInParent<Player>();
            if (player == null) return;

            OnPlayerExit(player);
        }

        private void OnTriggerStay2D(Collider2D other) {
            var player = other.GetComponentInParent<Player>();
            if (player == null) return;
            OnPlayerStay(player);
        }

        protected abstract void OnPlayerEnter(Player player);

        protected virtual void OnPlayerStay(Player player) { }

        protected virtual void OnPlayerExit(Player player) { }
    }
}