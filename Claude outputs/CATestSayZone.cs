using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LHS_CATest {
    /// <summary>
    /// 플레이어가 이 영역에 처음 들어오면 머리 위에 대사를 띄운다(CATestLines 의 키로 대사를 찾음).
    /// 물리 트리거 대신 플레이어 위치를 Rect 와 비교 → 레이어/충돌 설정과 무관.
    /// - once: 한 번만 (기본). 끄면 영역을 나갔다 다시 들어올 때마다.
    /// - requireGrounded: 땅을 딛고 있을 때만(점프 중 스쳐 지나갈 때 뜨지 않게).
    /// - 컷신 중에는 띄우지 않는다.
    /// </summary>
    public sealed class CATestSayZone : MonoBehaviour {
        public Rect area = new(0, 0, 8, 6);
        public string lineKey = "";
        public bool once = true;
        public bool requireGrounded = true;
        public float delay = 0.2f;

        private bool _inside;
        private bool _done;

        private void Update() {
            if (_done && once) return;
            var p = CATestHUD.CurrentPlayer;
            if (p == null || !p.isActiveAndEnabled || p.IsDead) return;
            var inside = area.Contains(p.transform.position);
            if (inside && !_inside && !CATestCutscene.IsPlaying && (!requireGrounded || p.IsGrounded)) {
                _done = true;
                Play().Forget();
                _inside = true;
                return;
            }
            if (!inside) _inside = false;
        }

        private async UniTaskVoid Play() {
            if (delay > 0f) await UniTask.Delay(System.TimeSpan.FromSeconds(delay));
            if (this == null) return;
            await CATestHUD.SayLines(CATestLines.Get(lineKey));
        }

#if UNITY_EDITOR
        private void OnDrawGizmos() {
            Gizmos.color = new Color(1f, 1f, 1f, 0.35f);
            Gizmos.DrawWireCube(area.center, area.size);
            UnityEditor.Handles.Label(new Vector3(area.xMin + 0.5f, area.yMax - 0.5f), "대사: " + lineKey);
        }
#endif
    }
}
