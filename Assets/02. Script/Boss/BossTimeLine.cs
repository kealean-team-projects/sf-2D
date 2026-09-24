using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _02._Script.Boss {
    public class BossTimeLine : MonoBehaviour {
        [SerializeField] private BossRoom room;
        [SerializeField] private Boss boss;

        [SerializeField] private Transform descendPoint;
        [SerializeField] private float descendDuration = 2f;

        public async UniTask PlayWarning(CancellationToken token) {
            token.ThrowIfCancellationRequested();
            
            room.SetLightBrightness(1f);

            await UniTask.Delay(TimeSpan.FromSeconds(1f),
                cancellationToken: token);
        }

        public async UniTask Descend(CancellationToken token) {
            var start = boss.RbCompo.position;
            Vector2 end = descendPoint.position;
            var elapsed = 0f;

            while (elapsed < descendDuration) {
                await UniTask.Yield(PlayerLoopTiming.FixedUpdate, token);

                elapsed += Time.fixedDeltaTime;
                boss.RbCompo.MovePosition(
                    Vector2.Lerp(start, end, elapsed / descendDuration));
            }

            boss.RbCompo.MovePosition(end);
        }
    }
}
