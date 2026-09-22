using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _02._Script.Boss.BossPatterns {
    public abstract class BossPattern : MonoBehaviour {
        public abstract UniTask Execute(Boss owner, CancellationToken token);
    }
}