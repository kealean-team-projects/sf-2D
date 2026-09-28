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
            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
            foreach (SpriteRenderer spriteRenderer in renderers)
            {
                PolygonCollider2D hitbox = spriteRenderer.GetComponent<PolygonCollider2D>();
                if (hitbox == null)
                    hitbox = spriteRenderer.gameObject.AddComponent<PolygonCollider2D>();

                hitbox.isTrigger = true;
                BuildSpriteShape(hitbox, spriteRenderer);
                hitbox.enabled = false;

                NavyMorayContactHitbox relay = spriteRenderer.GetComponent<NavyMorayContactHitbox>();
                if (relay == null)
                    relay = spriteRenderer.gameObject.AddComponent<NavyMorayContactHitbox>();
                relay.Initialize(this);
            }
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

        private static void BuildSpriteShape(PolygonCollider2D collider, SpriteRenderer spriteRenderer)
        {
            Sprite sprite = spriteRenderer.sprite;
            if (sprite == null)
            {
                collider.enabled = false;
                return;
            }

            int shapeCount = sprite.GetPhysicsShapeCount();
            if (shapeCount > 0)
            {
                collider.pathCount = shapeCount;
                var points = new List<Vector2>();
                for (int path = 0; path < shapeCount; path++)
                {
                    points.Clear();
                    sprite.GetPhysicsShape(path, points);
                    if (spriteRenderer.flipX || spriteRenderer.flipY)
                    {
                        for (int i = 0; i < points.Count; i++)
                            points[i] = new Vector2(spriteRenderer.flipX ? -points[i].x : points[i].x,
                                                    spriteRenderer.flipY ? -points[i].y : points[i].y);
                    }
                    collider.SetPath(path, points);
                }
                return;
            }

            Bounds bounds = sprite.bounds;
            Vector2 min = bounds.min;
            Vector2 max = bounds.max;
            collider.pathCount = 1;
            collider.SetPath(0, new[]
            {
                new Vector2(min.x, min.y), new Vector2(max.x, min.y),
                new Vector2(max.x, max.y), new Vector2(min.x, max.y)
            });
        }
    }
}
