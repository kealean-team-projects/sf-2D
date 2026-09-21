using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _02._Script.Boss
{
    public class Boss : MonoBehaviour
    {
        [SerializeField] private BossPattern[] patterns;

        private CancellationTokenSource patternCts;

        public void Begin()
        {
            if (patternCts != null || !isActiveAndEnabled) return;
            if (patterns == null || patterns.Length == 0 || Array.Exists(patterns, pattern => pattern == null))
            {
                Debug.LogError("패턴 목록을 확인하세요.", this);
                return;
            }

            patternCts = new CancellationTokenSource();
            RunPatterns(patternCts.Token).Forget();
        }

        private async UniTask RunPatterns(CancellationToken token)
        {
            try
            {
                while (true)
                {
                    foreach (BossPattern pattern in patterns)
                    {
                        token.ThrowIfCancellationRequested();
                        await pattern.Execute(this, token);
                    }

                    await UniTask.NextFrame(cancellationToken: token);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // 정상적인 중단 요청
            }
            finally
            {
                patternCts.Dispose();
                patternCts = null;
            }
        }

        public void Stop()
        {
            patternCts?.Cancel();
        }

        private void OnDisable()
        {
            Stop();
        }
    }
}