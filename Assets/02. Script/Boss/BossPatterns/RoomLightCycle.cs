using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _02._Script.Boss.BossPatterns {
    public class RoomLightCycle : MonoBehaviour {
        [SerializeField] [Min(0f)] [InspectorName("Bright Wait Time")]
        [Tooltip("다시 어두워지기 전 밝게 유지하는 시간")]
        private float waitTime = 5f;
        [SerializeField] [Min(0.01f)] private float dimDuration = 3f;
        [SerializeField] [Range(0f, 1f)] private float dimRatio = 0.3f;
        [SerializeField] [Min(0f)] [Tooltip("완전히 어두워진 뒤 최소 유지 시간. 보스 행동이 길면 그만큼 연장됩니다.")]
        private float darkHoldDuration = 5f;

        public float DarkHoldDuration => Mathf.Max(0f, darkHoldDuration);

        public UniTask Wait(CancellationToken token) =>
            UniTask.Delay(TimeSpan.FromSeconds(Mathf.Max(0f, waitTime)),
                cancellationToken: token);

        public async UniTask Dim(BossRoom room, CancellationToken token) {
            token.ThrowIfCancellationRequested();
            var elapsed = 0f;
            var duration = Mathf.Max(0.01f, dimDuration);
            while (elapsed < duration) {
                await UniTask.NextFrame(token);
                elapsed += Time.deltaTime;
                room.SetLightBrightness(Mathf.Lerp(1f, dimRatio, elapsed / duration));
            }

            room.SetLightBrightness(dimRatio);
        }
    }
}
