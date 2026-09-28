using System.Collections;
using System.Collections.Generic;
using _02._Script._01_Players.Components.DamageCompo;
using UnityEngine;

namespace NavyMorayAnimatorOnly
{
    [DisallowMultipleComponent]
    public sealed class NavyMorayAttackCycle : MonoBehaviour
    {
        private static readonly int HoldBodyId = Animator.StringToHash("HoldBody");
        private static readonly int RepeatId = Animator.StringToHash("Repeat");
        private static readonly int FinishId = Animator.StringToHash("Finish");

        [SerializeField] private Animator animator;
        [SerializeField, Min(0f)] private float initialDelay = 0.5f;
        [SerializeField, Min(0f)] private float cooldownAfterAttack = 4f;
        [SerializeField] private string startState = "Base Layer.Start_Head";
        [SerializeField] private string endState = "Base Layer.End_Tail";

        private readonly HashSet<DamageModule> damagedPlayersThisCycle = new HashSet<DamageModule>();
        private bool activated;
        private bool attackActive;

        private void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>();

            if (animator == null)
            {
                Debug.LogError("NavyMorayAttackCycle requires an Animator.", this);
                enabled = false;
                return;
            }

            animator.speed = 0f;
            SetContactColliders(false);
        }

        public void Activate()
        {
            if (!isActiveAndEnabled || activated)
                return;

            activated = true;
            StartCoroutine(RunAttackCycles());
        }

        public void TryDamage(Collider2D other)
        {
            if (!attackActive || other == null)
                return;

            DamageModule damageModule = other.GetComponentInParent<DamageModule>();
            if (damageModule != null && damagedPlayersThisCycle.Add(damageModule))
                damageModule.TakeDamage();
        }

        private IEnumerator RunAttackCycles()
        {
            if (initialDelay > 0f)
                yield return new WaitForSeconds(initialDelay);

            while (true)
            {
                yield return PlayOneAttack();
                if (cooldownAfterAttack > 0f)
                    yield return new WaitForSeconds(cooldownAfterAttack);
            }
        }

        private IEnumerator PlayOneAttack()
        {
            damagedPlayersThisCycle.Clear();
            animator.SetBool(HoldBodyId, false);
            animator.SetBool(RepeatId, false);
            animator.ResetTrigger(FinishId);
            animator.speed = 1f;
            animator.Play(startState, 0, 0f);
            animator.Update(0f);

            attackActive = true;
            SetContactColliders(true);

            int endStateHash = Animator.StringToHash(endState);
            while (true)
            {
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                if (!animator.IsInTransition(0) && state.fullPathHash == endStateHash && state.normalizedTime >= 1f)
                    break;
                yield return null;
            }

            attackActive = false;
            SetContactColliders(false);
            animator.speed = 0f;
        }

        private void SetContactColliders(bool active)
        {
            foreach (NavyMorayContactHitbox hitbox in GetComponentsInChildren<NavyMorayContactHitbox>(true))
            {
                Collider2D collider = hitbox.GetComponent<Collider2D>();
                if (collider != null)
                    collider.enabled = active;
            }
        }
    }
}
