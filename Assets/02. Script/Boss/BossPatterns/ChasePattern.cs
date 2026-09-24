using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _02._Script.Boss.BossPatterns
{
    public class ChasePattern : BossPattern
    {
        [SerializeField] private float descendSpeed = 15f;
        [SerializeField] private Transform player;
        [SerializeField] private BossRoom room;
        [SerializeField] private float searchDuration = 3f;
        [SerializeField, Min(0f)] private float spawnXOffset = 3f;

        [SerializeField] private LayerMask groundLayer;
        [SerializeField, Min(0.1f)] private float scanHeight = 5f;
        [SerializeField, Min(0.1f)] private float groundCheckDistance = 50f;

        private Vector2 returnPosition;
        private Vector2 searchPosition;

        public override async UniTask Execute(Boss owner, CancellationToken token)
        {
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

            returnPosition = new Vector2(x, zone.spawnPoint.position.y);

            var hit = Physics2D.Raycast(returnPosition, Vector2.down, groundCheckDistance, groundLayer);

            if (hit.collider == null) return;
            searchPosition = hit.point + Vector2.up * scanHeight;

            owner.SetReturnPosition(returnPosition);
            owner.RbCompo.position = returnPosition;

            while (Vector2.Distance(owner.RbCompo.position, searchPosition) > 0.01f)
            {
                await UniTask.Yield(PlayerLoopTiming.FixedUpdate, token);
                token.ThrowIfCancellationRequested();

                owner.RbCompo.MovePosition(Vector2.MoveTowards(
                    owner.RbCompo.position,
                    searchPosition,
                    descendSpeed * Time.fixedDeltaTime));
            }

            owner.ShowVision(true);

            try
            {
                var elapsed = 0f;

                while (elapsed < searchDuration)
                {
                    if (player == null) break;
                    if (owner.CanSee(player))
                    {
                        owner.SetTarget(player);
                        owner.ShowDetection(true);
                        return;
                    }

                    await UniTask.NextFrame(token);
                    elapsed += Time.deltaTime;
                }

                var closestLight = room.GetClosestLight(owner.RbCompo.position);
                owner.SetTarget(closestLight != null ? closestLight.transform : null);
            }
            finally
            {
                await owner.CloseVision(token);
            }
        }

        private void OnDrawGizmos()
        {
            Debug.DrawRay(transform.position, Vector2.down * scanHeight, Color.red);
        }
    }
}
