using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _02._Script.Boss
{
    public class ChasePattern : BossPattern
    {
        [SerializeField] private Transform player;
        [SerializeField] private BossRoom room;
        [SerializeField] private float searchDuration = 3f;
        [SerializeField] private float descendDuration = 1f;

        public override async UniTask Execute(Boss owner, CancellationToken token)
        {
            owner.SetTarget(null);
            owner.SetReturnPosition(owner.RbCompo.position);
            
            var zone = room.GetZone(player.position);
            if (zone == null) return;
            owner.SetReturnPosition(zone.spawnPoint.position);
            owner.RbCompo.position = zone.spawnPoint.position;
            
            await Descend(owner, zone.scanPoint.position, token);
            owner.ShowVision(true);
            
            try
            {
                float elapsed = 0f;

                while (elapsed < searchDuration)
                {
                    if (owner.CanSee(player))
                    {
                        owner.SetTarget(player);
                        return;
                    }

                    await UniTask.NextFrame(cancellationToken: token);
                    elapsed += Time.deltaTime;
                }
            
                owner.SetTarget(room.GetClosestLight(owner.RbCompo.position)?.transform);
            }
            finally
            {
                owner.ShowVision(false);
            }
        }
        
        private async UniTask Descend(Boss owner, Vector2 target, CancellationToken token)
        {
            Vector2 start = owner.RbCompo.position;
            float elapsed = 0f;

            while (elapsed < descendDuration)
            {
                await UniTask.Yield(PlayerLoopTiming.FixedUpdate, token);
                elapsed += Time.fixedDeltaTime;

                owner.RbCompo.MovePosition(Vector2.Lerp(start, target, elapsed / descendDuration));
            }

            owner.RbCompo.MovePosition(target);
        }
    }
}
