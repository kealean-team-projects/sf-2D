using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _02._Script.Boss.BossPatterns {
    public class ChasePattern : BossPattern {
        [SerializeField] private float descendSpeed = 15f;
        [SerializeField] private Transform player;
        [SerializeField] private BossRoom room;
        [SerializeField] private float searchDuration = 3f;
        [SerializeField] [Min(0f)] private float spawnXOffset = 3f;
        [SerializeField] [Min(0f)] private float patrolRange = 3f;
        [SerializeField] [Min(0f)] private float patrolSpeed = 2f;

        [SerializeField] private LayerMask groundLayer;
        [SerializeField] [Min(0.1f)] private float scanHeight = 5f;
        [SerializeField] [Min(0.1f)] private float groundCheckDistance = 50f;
        private readonly RaycastHit2D[] patrolHits = new RaycastHit2D[1];

        private Vector2 returnPosition;
        private Vector2 searchPosition;

        private void OnDrawGizmos() {
            Debug.DrawRay(transform.position, Vector2.down * scanHeight, Color.red);
        }

        public override async UniTask Execute(Boss owner, CancellationToken token) {
            owner.SetTarget(null);
            owner.SetReturnPosition(owner.RbCompo.position);
            if (player == null) return;

            var zone = room.GetZone(player.position);

            if (zone == null) return;
            var bounds = zone.area.bounds;
            var bodyRadius = 0f;
            foreach (var collider in owner.GetComponents<CircleCollider2D>())
                if (collider.enabled && !collider.isTrigger)
                    bodyRadius = Mathf.Max(bodyRadius, collider.bounds.extents.x);

            var minX = bounds.min.x + bodyRadius;
            var maxX = bounds.max.x - bodyRadius;
            var minY = bounds.min.y + bodyRadius;
            var maxY = bounds.max.y - bodyRadius;
            if (minX > maxX || minY > maxY) return;

            var playerX = Mathf.Clamp(player.position.x, minX, maxX);
            var offset = Mathf.Max(0f, spawnXOffset);

            var x = Mathf.Clamp(playerX + Random.Range(-offset, offset), minX, maxX);

            returnPosition = new Vector2(x, Mathf.Clamp(zone.spawnPoint.position.y, minY, maxY));

            if (!owner.Navigation.TryGetClearPosition(returnPosition, out returnPosition)) return;
            if (!bounds.Contains(new Vector3(returnPosition.x, returnPosition.y, bounds.center.z))) return;
            x = returnPosition.x;

            var hit = Physics2D.Raycast(returnPosition, Vector2.down, groundCheckDistance, groundLayer);

            if (hit.collider == null) return;
            searchPosition = new Vector2(x,
                Mathf.Clamp(hit.point.y + scanHeight, minY, returnPosition.y));

            owner.SetReturnPosition(returnPosition);
            owner.RbCompo.position = returnPosition;
            Physics2D.SyncTransforms();
            searchPosition = owner.Navigation.GetClearDestination(owner.RbCompo, searchPosition);
            var descendDeadline = Time.time + Vector2.Distance(returnPosition, searchPosition) /
                Mathf.Max(0.1f, descendSpeed) + 2f;

            while (Vector2.Distance(owner.RbCompo.position, searchPosition) > 0.01f) {
                await UniTask.Yield(PlayerLoopTiming.FixedUpdate, token, cancelImmediately: true);
                token.ThrowIfCancellationRequested();

                if (Time.time >= descendDeadline) break;

                owner.RbCompo.MovePosition(Vector2.MoveTowards(
                    owner.RbCompo.position,
                    owner.Navigation.GetClearDestination(owner.RbCompo, searchPosition),
                    Mathf.Max(0.1f, descendSpeed) * Time.fixedDeltaTime));
            }

            searchPosition = owner.RbCompo.position;

            try {
                owner.SetScanDirection(owner.RbCompo.position.x < player.position.x);
                await owner.OpenVision(token);
                var elapsed = 0f;
                var patrolDirection = 1f;
                var patrolMinX = Mathf.Max(minX, searchPosition.x - patrolRange);
                var patrolMaxX = Mathf.Min(maxX, searchPosition.x + patrolRange);
                var patrolFilter = new ContactFilter2D();
                patrolFilter.SetLayerMask(groundLayer);
                patrolFilter.useTriggers = false;

                while (elapsed < searchDuration) {
                    await UniTask.Yield(PlayerLoopTiming.FixedUpdate, token, cancelImmediately: true);
                    token.ThrowIfCancellationRequested();
                    if (player == null) break;
                    if (owner.CanSee(player)) {
                        owner.SetTarget(player);
                        owner.ShowDetection(true);
                        room.OnPlayerDetected();
                        return;
                    }

                    Patrol(owner.RbCompo, patrolMinX, patrolMaxX, patrolFilter, ref patrolDirection);
                    elapsed += Time.fixedDeltaTime;
                }

                var closestLight = room.GetClosestLight(owner.RbCompo.position);
                owner.SetTarget(closestLight != null ? closestLight.transform : null);
            }
            finally {
                await owner.CloseVision(token, () => {
                    if (player == null || owner.Target == player || !owner.CanSee(player)) return;
                    owner.SetTarget(player);
                    owner.ShowDetection(true);
                    room.OnPlayerDetected();
                });
                token.ThrowIfCancellationRequested();
                room.OnScanCompleted();
            }
        }

        private void Patrol(Rigidbody2D body, float minX, float maxX,
            ContactFilter2D filter, ref float direction) {
            if (patrolSpeed <= 0f || maxX <= minX) return;

            var targetX = direction > 0f ? maxX : minX;
            var nextX = Mathf.MoveTowards(body.position.x, targetX,
                patrolSpeed * Time.fixedDeltaTime);
            var distance = Mathf.Abs(nextX - body.position.x);
            if (distance <= 0.001f ||
                body.Cast(Vector2.right * direction, filter, patrolHits, distance + 0.1f) > 0) {
                direction = -direction;
                return;
            }

            body.MovePosition(new Vector2(nextX, body.position.y));
        }
    }
}