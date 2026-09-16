using _02._Script.Component;
using _02._Script.Players;
using UnityEngine;

namespace _02._Script.Trap {
    public enum DamageType {
        None,
        Damage
    }

    [RequireComponent(typeof(Collider2D))]
    public abstract class TrapBase : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponentInParent<Player>();
            if (player == null) return;

            OnPlayerEnter(player);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var player = other.GetComponentInParent<Player>();
            if (player == null) return;

            OnPlayerExit(player);
        }

        protected abstract void OnPlayerEnter(Player player);

        protected virtual void OnPlayerExit(Player player)
        {
        }
    }
}