using UnityEngine;
using _02._Script._01_Players;

namespace NavyMorayAnimatorOnly
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class NavyMorayActivationTrigger : MonoBehaviour
    {
        [SerializeField] private NavyMorayAttackCycle attackCycle;

        private void Awake()
        {
            Collider2D trigger = GetComponent<Collider2D>();
            trigger.isTrigger = true;
            if (attackCycle == null)
                attackCycle = GetComponentInParent<NavyMorayAttackCycle>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (attackCycle != null && TryGetPlayer(other, out _))
                attackCycle.Activate();
        }

        private static bool TryGetPlayer(Collider2D other, out Player player)
        {
            for (Transform current = other.transform; current != null; current = current.parent)
            {
                if (current.TryGetComponent(out player))
                    return true;
            }

            player = null;
            return false;
        }
    }
}