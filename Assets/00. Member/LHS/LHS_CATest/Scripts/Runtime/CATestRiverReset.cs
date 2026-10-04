using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LHS_CATest {
    /// <summary>
    /// 강물(튜토리얼)에 빠지면: 짧게 암전 → 강가(safePoint)로 되돌림 → 밝아짐. 죽지 않는다.
    /// "위험하지만 벌은 가볍게" — 튜토리얼 구간에서 실패해도 흐름이 끊기지 않게.
    /// 처음 빠졌을 때만 대사(lineKey)를 한 번 말한다.
    /// 판정은 Rect 와 플레이어 위치 비교(수면보다 조금 아래까지 잠기면 발동).
    /// </summary>
    public sealed class CATestRiverReset : MonoBehaviour {
        public Rect area = new(0, -10, 20, 9);
        public Vector2 safePoint;
        public string lineKey = "sunny_river_fall";

        private bool _busy;
        private bool _spoken;

        private void Update() {
            if (_busy) return;
            var p = CATestHUD.CurrentPlayer;
            if (p == null || !p.isActiveAndEnabled || p.IsDead) return;
            if (area.Contains(p.transform.position)) Run(p).Forget();
        }

        private async UniTaskVoid Run(_02._Script._01_Players.Player p) {
            _busy = true;
            CATestCutscene.LockInput(true);
            if (p != null) CATestAudio.PlaySfx("water_splash", p.transform.position);
            await CATestHUD.FadeTo(1f, 0.35f);
            if (p != null) p.RestoreState(safePoint, p.CurrentStamina);
            await UniTask.Delay(System.TimeSpan.FromSeconds(0.25f), true);
            CATestCutscene.LockInput(false);
            await CATestHUD.FadeTo(0f, 0.5f);
            if (!_spoken && !string.IsNullOrEmpty(lineKey)) {
                _spoken = true;
                CATestHUD.SayLines(CATestLines.Get(lineKey)).Forget();
            }
            _busy = false;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos() {
            Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.5f);
            Gizmos.DrawWireCube(area.center, area.size);
            Gizmos.DrawWireSphere(safePoint, 0.5f);
        }
#endif
    }
}
