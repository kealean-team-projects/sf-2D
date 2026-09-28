using UnityEngine;

namespace NavyMorayAnimatorOnly
{
    [DisallowMultipleComponent]
    public sealed class NavyMorayContactHitbox : MonoBehaviour
    {
        private NavyMorayAttackCycle attackCycle;

        public void Initialize(NavyMorayAttackCycle owner)
        {
            attackCycle = owner;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (attackCycle == null)
                attackCycle = GetComponentInParent<NavyMorayAttackCycle>();
            attackCycle?.TryDamage(other);
        }
    }
}
