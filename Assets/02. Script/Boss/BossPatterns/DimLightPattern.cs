using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _02._Script.Boss
{
    public class DimLightsPattern : BossPattern
    {
        [SerializeField] private BossRoom room;
        [SerializeField, Min(0f)] private float waitTime = 5f;
        [SerializeField, Min(0.01f)] private float dimDuration = 3f;
        [SerializeField, Range(0f, 1f)] private float dimRatio = 0.3f;

        public override async UniTask Execute(Boss owner, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            room.SetLightBrightness(1f);

            await UniTask.Delay(TimeSpan.FromSeconds(waitTime), cancellationToken: token);
            float elapsed = 0f;

            while (elapsed < dimDuration)
            {
                await UniTask.NextFrame(cancellationToken: token);
                elapsed += Time.deltaTime;
                room.SetLightBrightness(Mathf.Lerp(1f, dimRatio, elapsed / dimDuration));
            }

            room.SetLightBrightness(dimRatio);
        }
    }
}