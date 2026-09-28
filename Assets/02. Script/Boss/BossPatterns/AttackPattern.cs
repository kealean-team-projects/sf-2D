using System.Threading;
using _02._Script._01_Players;
using _02._Script._01_Players.Components.DamageCompo;
using _02._Script._04_Interaction;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _02._Script.Boss.BossPatterns {
    public class AttackPattern : BossPattern {
        [SerializeField] private float dashSpeed = 15f;
        [SerializeField] [Min(0.1f)] private float returnSpeed = 15f;
        public bool IsAttacking { get; private set; }

        public override async UniTask Execute(Boss owner, CancellationToken token) {
            try {
                if (owner.Target != null)
                    await Dash(owner, token);
            }
            finally {
                IsAttacking = false;
                owner.ShowDetection(false);
            }

            await Return(owner, token);
        }

        private async UniTask Dash(Boss owner, CancellationToken token) {
            IsAttacking = true;
            owner.Navigation.ResetPath();
            var targetPlayer = owner.Target.GetComponentInParent<Player>();
            var targetDamage = owner.Target.GetComponentInChildren<DamageModule>();
            var targetLight = owner.Target.GetComponent<InteractLight>();
            var targetColliders = owner.Target.GetComponentsInChildren<Collider2D>();
            var bodyCollider = owner.GetComponent<Collider2D>();
            var progressPosition = owner.RbCompo.position;
            var lastProgressTime = Time.time;
            var dashDeadline = Time.time + 20f;

            while (IsAttacking) {
                await UniTask.Yield(PlayerLoopTiming.FixedUpdate, token, cancelImmediately: true);
                token.ThrowIfCancellationRequested();

                if (!IsAttacking || owner.Target == null) break;
                if (targetPlayer != null && targetPlayer.IsDead) break;
                if (Vector2.Distance(progressPosition, owner.RbCompo.position) > 0.05f) {
                    progressPosition = owner.RbCompo.position;
                    lastProgressTime = Time.time;
                }

                if (Time.time - lastProgressTime > 2f || Time.time >= dashDeadline) break;

                var touching = false;
                foreach (var collider in targetColliders) {
                    if (collider == null || !collider.enabled || collider.isTrigger) continue;
                    var contact = bodyCollider.Distance(collider);
                    if (contact.isValid && contact.distance <= 0.02f) {
                        touching = true;
                        break;
                    }
                }

                if (touching || Vector2.Distance(owner.RbCompo.position, owner.Target.position) <= 0.1f) {
                    if (targetLight != null) targetLight.Break();
                    if (targetDamage != null) targetDamage.TakeDamage();
                    Finish();
                    break;
                }

                owner.Navigation.MoveTowards(owner.RbCompo, owner.Target.position, dashSpeed);
            }
        }

        private async UniTask Return(Boss owner, CancellationToken token) {
            // Return to the validated spawn point, not an arbitrary point above the target.
            var target = owner.ReturnPosition;
            owner.Navigation.ResetPath();
            var progressPosition = owner.RbCompo.position;
            var lastProgressTime = Time.time;
            var returnDeadline = Time.time + 20f;

            while (Vector2.Distance(owner.RbCompo.position, target) > 0.01f) {
                await UniTask.Yield(PlayerLoopTiming.FixedUpdate, token, cancelImmediately: true);
                token.ThrowIfCancellationRequested();

                if (Vector2.Distance(progressPosition, owner.RbCompo.position) > 0.05f) {
                    progressPosition = owner.RbCompo.position;
                    lastProgressTime = Time.time;
                }

                if (Time.time - lastProgressTime > 2f || Time.time >= returnDeadline) {
                    // Recover only into a collision-checked navigation position.
                    if (owner.Navigation.TryGetClearPosition(target, out var safePosition)) {
                        owner.RbCompo.position = safePosition;
                        owner.RbCompo.linearVelocity = Vector2.zero;
                        Physics2D.SyncTransforms();
                    }

                    break;
                }

                owner.Navigation.MoveTowards(owner.RbCompo, target, Mathf.Max(0.1f, returnSpeed));
            }
        }

        public void Finish() {
            IsAttacking = false;
        }
    }
}