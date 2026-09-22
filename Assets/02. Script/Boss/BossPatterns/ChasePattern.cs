using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _02._Script.Boss.BossPatterns {
    public class ChasePattern : BossPattern {
        [SerializeField] private Transform player;
        [SerializeField] private BossRoom room;
        [SerializeField] private float searchDuration = 3f;
        [SerializeField] private float descendDuration = 1f;
        [SerializeField] [Min(0f)] private float spawnXOffset = 3f;

        public override async UniTask Execute(Boss owner, CancellationToken token) {
            owner.SetTarget(null);
            owner.SetReturnPosition(owner.RbCompo.position);
            if (player == null) return;

            var zone = room.GetZone(player.position);
            if (zone == null) return;
            var offset = Mathf.Max(0f, spawnXOffset);
            var bounds = zone.area.bounds;
            var x = Random.Range(
                Mathf.Max(bounds.min.x, player.position.x - offset),
                Mathf.Min(bounds.max.x, player.position.x + offset));
            var spawnPosition = new Vector2(x, zone.spawnPoint.position.y);
            var scanPosition = new Vector2(x, zone.scanPoint.position.y);
            owner.SetReturnPosition(spawnPosition);
            owner.RbCompo.position = spawnPosition;

            await Descend(owner, scanPosition, token);
            owner.ShowVision(true);

            try {
                var elapsed = 0f;

                while (elapsed < searchDuration) {
                    if (player == null) break;
                    if (owner.CanSee(player)) {
                        owner.SetTarget(player);
                        return;
                    }

                    await UniTask.NextFrame(token);
                    elapsed += Time.deltaTime;
                }

                var closestLight = room.GetClosestLight(owner.RbCompo.position);
                owner.SetTarget(closestLight != null ? closestLight.transform : null);
            }
            finally {
                owner.ShowVision(false);
            }
        }

        private async UniTask Descend(Boss owner, Vector2 target, CancellationToken token) {
            var start = owner.RbCompo.position;
            var elapsed = 0f;

            while (elapsed < descendDuration) {
                await UniTask.Yield(PlayerLoopTiming.FixedUpdate, token);
                elapsed += Time.fixedDeltaTime;

                owner.RbCompo.MovePosition(Vector2.Lerp(start, target, elapsed / descendDuration));
            }

            owner.RbCompo.MovePosition(target);
        }
    }
}