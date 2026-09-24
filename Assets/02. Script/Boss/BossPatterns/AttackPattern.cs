using System.Threading;
using Cysharp.Threading.Tasks;
using _02._Script._04_Interaction;
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

        private async UniTask Dash(Boss owner, CancellationToken token)
        {
            IsAttacking = true;

            while (IsAttacking)
            {
                await UniTask.Yield(PlayerLoopTiming.FixedUpdate, token);
                token.ThrowIfCancellationRequested();

                if (!IsAttacking || owner.Target == null) break;

                if (Vector2.Distance(owner.RbCompo.position, owner.Target.position) <= 0.1f &&
                    owner.Target.TryGetComponent<InteractLight>(out var light))
                {
                    light.Break();
                    Finish();
                    break;
                }

                owner.RbCompo.MovePosition(Vector2.MoveTowards(
                    owner.RbCompo.position,
                    owner.Target.position,
                    dashSpeed * Time.fixedDeltaTime));
            }
        }

        private async UniTask Return(Boss owner, CancellationToken token)
        {
            Vector2 target = new Vector2(
                owner.RbCompo.position.x,
                owner.ReturnPosition.y);

            while (Vector2.Distance(owner.RbCompo.position, target) > 0.01f)
            {
                await UniTask.Yield(PlayerLoopTiming.FixedUpdate, token);
                token.ThrowIfCancellationRequested();

                owner.RbCompo.MovePosition(Vector2.MoveTowards(
                    owner.RbCompo.position, target,
                    Mathf.Max(0.1f, returnSpeed) * Time.fixedDeltaTime));
            }
        }

        public void Finish() => IsAttacking = false;
    }
}
