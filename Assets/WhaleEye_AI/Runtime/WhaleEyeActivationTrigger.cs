using UnityEngine;

namespace WhaleEyeBlink
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class WhaleEyeActivationTrigger : MonoBehaviour
    {
        private const string DamageModuleFullName = "_02._Script._01_Players.Components.DamageCompo.DamageModule";

        [SerializeField] private WhaleEyeAttackCycle attackCycle;
        private Collider2D activationCollider;
        private bool activated;

        private void Awake()
        {
            activationCollider = GetComponent<Collider2D>();
            activationCollider.isTrigger = true;
            if (attackCycle == null)
                attackCycle = GetComponentInParent<WhaleEyeAttackCycle>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (activated || attackCycle == null || !HasDamageModule(other))
                return;

            activated = true;
            activationCollider.enabled = false;
            attackCycle.Activate();
        }

        private static bool HasDamageModule(Collider2D other)
        {
            MonoBehaviour[] components = other.GetComponentsInParent<MonoBehaviour>(true);
            foreach (MonoBehaviour component in components)
            {
                if (component != null && component.GetType().FullName == DamageModuleFullName)
                    return true;
            }
            return false;
        }
    }
}
