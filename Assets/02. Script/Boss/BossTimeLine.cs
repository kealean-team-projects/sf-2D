using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _02._Script.Boss
{
    public class BossTimeLine : MonoBehaviour
    {
        [SerializeField] private BossRoom room;
        [SerializeField] private Boss boss;
        
        [SerializeField] private int flickerCount = 3;
        [SerializeField] private float flickerBrightness = 0.2f;
        [SerializeField] private float flickerInterval = 0.15f;
        
        [SerializeField] private Transform descendPoint;
        [SerializeField] private float descendDuration = 2f;

        public async UniTask Play(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            room.SetLightBrightness(1f);

            await UniTask.Delay(TimeSpan.FromSeconds(1f),
                cancellationToken: token);
            
            for (int i = 0; i < flickerCount; i++)
            {
                room.SetLightBrightness(flickerBrightness);

                await UniTask.Delay(
                    TimeSpan.FromSeconds(flickerInterval),
                    cancellationToken: token);

                room.SetLightBrightness(1f);

                await UniTask.Delay(
                    TimeSpan.FromSeconds(flickerInterval),
                    cancellationToken: token);
            }
            

            
            await Descend(token);

        }
        
        private async UniTask Descend(CancellationToken token)
        {
            Vector2 start = boss.RbCompo.position;
            Vector2 end = descendPoint.position;
            float elapsed = 0f;

            while (elapsed < descendDuration)
            {
                await UniTask.Yield(PlayerLoopTiming.FixedUpdate, token);

                elapsed += Time.fixedDeltaTime;
                boss.RbCompo.MovePosition(
                    Vector2.Lerp(start, end, elapsed / descendDuration));
            }

            boss.RbCompo.MovePosition(end);
        }
    }
}
