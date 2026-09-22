using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _02._Script.Boss
{
    public class AttackPattern : BossPattern
    {
        [SerializeField] private float dashSpeed = 15f;
        [SerializeField] private float maxDashTime = 2f;
        [SerializeField, Min(0.1f)] private float returnSpeed = 15f;
        public bool IsAttacking { get; private set; }

        public override async UniTask Execute(Boss owner, CancellationToken token)
        {
            try
            {
                if (owner.Target != null)
                    await Dash(owner, token);
            }
            finally
            {
                IsAttacking = false;
            }

            await Return(owner, token);
        }

        private async UniTask Dash(Boss owner, CancellationToken token)
        {

            IsAttacking = true;
            Vector2 targetPosition = owner.Target.position;
            
            float elapsed = 0f;
            
            while (IsAttacking && elapsed < maxDashTime && owner.RbCompo.position != targetPosition)
            {
                await UniTask.Yield(PlayerLoopTiming.FixedUpdate, token);
                token.ThrowIfCancellationRequested();
                if (!IsAttacking) break;
                elapsed += Time.fixedDeltaTime;

                Vector2 nextPosition = Vector2.MoveTowards(
                    owner.RbCompo.position,
                    targetPosition,
                    dashSpeed * Time.fixedDeltaTime);

                owner.RbCompo.MovePosition(nextPosition);
            }
        }
        
        private async UniTask Return(Boss owner, CancellationToken token)
        {
            while (Vector2.Distance(owner.RbCompo.position, owner.ReturnPosition) > 0.01f)
            {
                await UniTask.Yield(PlayerLoopTiming.FixedUpdate, token);
                token.ThrowIfCancellationRequested();
                owner.RbCompo.MovePosition(Vector2.MoveTowards(
                    owner.RbCompo.position, owner.ReturnPosition,
                    Mathf.Max(0.1f, returnSpeed) * Time.fixedDeltaTime));
            }
        }

        public void Finish() => IsAttacking = false;
    }
}
