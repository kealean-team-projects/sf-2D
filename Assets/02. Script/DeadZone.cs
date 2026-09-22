using _02._Script._01_Players;
using _02._Script._01_Players.Components.DamageCompo;
using UnityEngine;

namespace _02._Script {
    public class DeadZone : MonoBehaviour {
        private void OnTriggerEnter2D(Collider2D other) {
            var player = other.GetComponentInParent<Player>();
            if (player == null) return;

            DamageModule damage;

            Debug.Log("감지");

            if (player.TryGetComponent(out damage))
                damage.TakeDamage();
        }
    }
}