using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WhaleEyeBlink
{
    [DisallowMultipleComponent]
    public sealed class WhaleEyeAttackCycle : MonoBehaviour
    {
        private const string DamageModuleFullName = "_02._Script._01_Players.Components.DamageCompo.DamageModule";
        private static readonly int BlinkTriggerId = Animator.StringToHash("Blink");
        private static readonly int OpenStateId = Animator.StringToHash("Base Layer.Open");

        [Header("Blink cycle")]
        [SerializeField] private Animator animator;
        [SerializeField, Min(0f)] private float initialDelay = 2f;
        [SerializeField, Min(0.4f)] private float intervalBetweenBlinks = 5f;

        [Header("Eye-open attack range")]
        [SerializeField] private Vector2 overlapOffset = Vector2.zero;
        [SerializeField] private Vector2 overlapSize = new Vector2(4f, 2.5f);
        [SerializeField] private LayerMask targetLayers = 128;
        [SerializeField, Min(0.02f)] private float detectionInterval = 0.1f;

        private readonly List<Collider2D> overlapResults = new List<Collider2D>(16);
        private readonly HashSet<EntityId> damagedObjectsThisOpenPhase = new HashSet<EntityId>();
        private ContactFilter2D contactFilter;
        private bool eyeOpen;
        private bool activated;

        private void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>();

            if (animator == null)
            {
                Debug.LogError("WhaleEyeAttackCycle requires an Animator.", this);
                enabled = false;
                return;
            }

            contactFilter = new ContactFilter2D();
            contactFilter.SetLayerMask(targetLayers);
            contactFilter.useTriggers = true;

            // Keep the eye closed and idle until the separate activation volume detects a player.
            animator.speed = 0f;
            animator.Play("Base Layer.Closed", 0, 0f);
            animator.Update(0f);
            eyeOpen = false;
        }

        public void Activate()
        {
            if (!isActiveAndEnabled || activated)
                return;

            activated = true;
            StartCoroutine(RunBlinkCycle());
            StartCoroutine(DetectionRoutine());
        }

        public void OnEyeClosed()
        {
            eyeOpen = false;
            damagedObjectsThisOpenPhase.Clear();
        }

        public void OnEyeOpened()
        {
            eyeOpen = true;
            damagedObjectsThisOpenPhase.Clear();
        }

        private IEnumerator RunBlinkCycle()
        {
            if (initialDelay > 0f)
                yield return new WaitForSeconds(initialDelay);

            animator.speed = 1f;
            float interval = Mathf.Max(0.4f, intervalBetweenBlinks);
            while (true)
            {
                // The initial closed pose opens first. Subsequent waits begin after each complete opening.
                yield return WaitUntilOpen();
                yield return new WaitForSeconds(interval);

                animator.ResetTrigger(BlinkTriggerId);
                animator.SetTrigger(BlinkTriggerId);
            }
        }

        private IEnumerator WaitUntilOpen()
        {
            while (animator.isActiveAndEnabled &&
                   (animator.IsInTransition(0) || animator.GetCurrentAnimatorStateInfo(0).fullPathHash != OpenStateId))
                yield return null;
        }

        private IEnumerator DetectionRoutine()
        {
            WaitForSeconds wait = new WaitForSeconds(Mathf.Max(0.02f, detectionInterval));
            while (true)
            {
                if (eyeOpen)
                    CheckOverlapBox();
                yield return wait;
            }
        }

        private void CheckOverlapBox()
        {
            Vector2 center = transform.TransformPoint(overlapOffset);
            Vector3 scale = transform.lossyScale;
            Vector2 size = Vector2.Scale(overlapSize, new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y)));
            overlapResults.Clear();
            int count = Physics2D.OverlapBox(center, size, transform.eulerAngles.z, contactFilter, overlapResults);

            for (int i = 0; i < count; i++)
            {
                Collider2D candidate = overlapResults[i];
                if (candidate == null)
                    continue;

                MonoBehaviour[] components = candidate.GetComponentsInParent<MonoBehaviour>(true);
                foreach (MonoBehaviour component in components)
                {
                    if (component == null || component.GetType().FullName != DamageModuleFullName)
                        continue;

                    EntityId entityId = component.GetEntityId();
                    if (damagedObjectsThisOpenPhase.Add(entityId))
                        component.SendMessage("TakeDamage", SendMessageOptions.DontRequireReceiver);
                    break;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 scale = transform.lossyScale;
            Vector2 size = Vector2.Scale(overlapSize, new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y)));
            Gizmos.color = eyeOpen ? Color.red : Color.gray;
            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.TransformPoint(overlapOffset), transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, size);
            Gizmos.matrix = previous;
        }
    }
}
