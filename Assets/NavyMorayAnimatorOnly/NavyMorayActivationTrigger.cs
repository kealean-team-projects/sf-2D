using UnityEngine;
using _02._Script._01_Players.Components.DamageCompo;

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
            if (attackCycle != null && other.GetComponentInParent<DamageModule>() != null)
                attackCycle.Activate();
        }
    }
}
