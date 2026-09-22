using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _02._Script.Boss
{
    public class RoomLightCycle : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float waitTime = 5f;
        [SerializeField, Min(0.01f)] private float dimDuration = 3f;
        [SerializeField, Range(0f, 1f)] private float dimRatio = 0.3f;

        // 반복은 BossRoom이 관리하고, 여기서는 감광 한 번만 실행합니다.
        public async UniTask Dim(BossRoom room, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await UniTask.Delay(TimeSpan.FromSeconds(Mathf.Max(0f, waitTime)),
                cancellationToken: token);

            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, dimDuration);
            while (elapsed < duration)
            {
                await UniTask.NextFrame(cancellationToken: token);
                elapsed += Time.deltaTime;
                room.SetLightBrightness(Mathf.Lerp(1f, dimRatio, elapsed / duration));
            }
            room.SetLightBrightness(dimRatio);
        }
    }
}
