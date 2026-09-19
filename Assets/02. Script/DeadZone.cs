using _02._Script._01_Players;
using _02._Script._01_Players.Components.DamageCompo;
using UnityEngine;

namespace _02._Script {
    public class DeadZone : MonoBehaviour {
        private void OnTriggerEnter2D(Collider2D other) {
            Player player = other.GetComponentInParent<Player>();
            if (player == null) return;

            DamageModule damage;

            if (player.TryGetComponent<DamageModule>(out damage))
                damage.TakeDamage();
        }
    }
}