using System.Threading;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

namespace _02._Script.Boss
{
    public class ChasePattern : BossPattern
    {
        [SerializeField] private Transform player;
        [SerializeField] private BossRoom room;
        [SerializeField] private float searchDuration = 3f;
        [SerializeField] private float descendDuration = 1f;
        [SerializeField, Min(0f)] private float spawnXOffset = 3f;
        [SerializeField] private LayerMask groundMask = 1 << 6;
        [SerializeField, Min(0f)] private float groundClearance = 0.2f;
        private readonly List<RaycastHit2D> groundHits = new List<RaycastHit2D>();

        public override async UniTask Execute(Boss owner, CancellationToken token)
        {
            owner.SetTarget(null);
            owner.SetReturnPosition(owner.RbCompo.position);
            if (player == null) return;
            
            var zone = room.GetZone(player.position);
            if (zone == null) return;
            float offset = Mathf.Max(0f, spawnXOffset);
            Bounds bounds = zone.area.bounds;
            float x = Random.Range(
                Mathf.Max(bounds.min.x, player.position.x - offset),
                Mathf.Min(bounds.max.x, player.position.x + offset));
            Vector2 spawnPosition = new Vector2(x, zone.spawnPoint.position.y);
            Vector2 scanPosition = new Vector2(x, zone.scanPoint.position.y);
            owner.SetReturnPosition(spawnPosition);
            owner.RbCompo.position = spawnPosition;
            
            await Descend(owner, scanPosition, token);
            owner.ShowVision(true);
            
            try
            {
                float elapsed = 0f;

                while (elapsed < searchDuration)
                {
                    if (player == null) break;
                    if (owner.CanSee(player))
                    {
                        owner.SetTarget(player);
                        return;
                    }

                    await UniTask.NextFrame(cancellationToken: token);
                    elapsed += Time.deltaTime;
                }
            
                var closestLight = room.GetClosestLight(owner.RbCompo.position);
                owner.SetTarget(closestLight != null ? closestLight.transform : null);
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
            var filter = new ContactFilter2D();
            filter.SetLayerMask(groundMask);
            filter.useTriggers = false;

            while (true)
            {
                await UniTask.Yield(PlayerLoopTiming.FixedUpdate, token);
                token.ThrowIfCancellationRequested();
                elapsed += Time.fixedDeltaTime;

                float progress = descendDuration > 0f ? elapsed / descendDuration : 1f;
                Vector2 next = Vector2.Lerp(start, target, progress);
                Vector2 movement = next - owner.RbCompo.position;
                float distance = movement.magnitude;

                if (movement.y < 0f)
                {
                    float clearance = Mathf.Max(0f, groundClearance);
                    int count = owner.RbCompo.Cast(
                        movement.normalized, filter, groundHits, distance + clearance);
                    if (count > 0)
                    {
                        foreach (RaycastHit2D hit in groundHits)
                            distance = Mathf.Min(distance, Mathf.Max(0f, hit.distance - clearance));

                        owner.RbCompo.MovePosition(owner.RbCompo.position + movement.normalized * distance);
                        return;
                    }
                }

                owner.RbCompo.MovePosition(next);
                if (progress >= 1f) return;
            }
        }
    }
}
