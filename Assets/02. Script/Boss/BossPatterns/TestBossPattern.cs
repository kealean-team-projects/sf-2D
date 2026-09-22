using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _02._Script.Boss.BossPatterns {
    public class TestBossPattern : BossPattern {
        [SerializeField] private string patternName = "A";
        [SerializeField] [Min(0f)] private float duration = 2f;

        public override async UniTask Execute(Boss owner, CancellationToken token) {
            token.ThrowIfCancellationRequested();

            Debug.Log($"{patternName} 시작", owner);

            await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: token);

            Debug.Log($"{patternName} 완료", owner);
        }
    }
}