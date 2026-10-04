using UnityEngine;

namespace LHS_CATest {
    /// <summary>
    /// 이 영역에 들어가면 화면 중앙 상단에 지역 이름을 띄운다.
    /// 바로 전에 띄운 이름과 같으면 다시 띄우지 않는다(영역 경계에서 왔다 갔다 할 때 반복 표시 방지).
    /// 물리 트리거 대신 플레이어 위치를 직접 비교한다 → 레이어/충돌 설정과 무관하게 동작.
    /// </summary>
    public sealed class CATestAreaZone : MonoBehaviour {
        public Rect area = new(0, 0, 40, 30);
        public string title = "지역 이름";
        public string subtitle = "";
        [Tooltip("끄면 이 구역에 들어가도 타이틀이 뜨지 않는다(장소 이름 기록만 함). 전체를 끄려면 CATest_HUD 의 Show Area Titles.")]
        public bool showTitle = true;

        private bool _inside;

        private void Update() {
            var p = CATestHUD.PlayerTransform;
            if (p == null) return;
            var inside = area.Contains(p.position);
            if (inside && !_inside && CATestHUD.LastTitle != title) {
                if (showTitle) CATestHUD.ShowTitle(title, subtitle);
                else CATestHUD.RecordPlace(title);
            }
            _inside = inside;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos() {
            Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.5f);
            Gizmos.DrawWireCube(area.center, area.size);
            UnityEditor.Handles.Label(new Vector3(area.xMin + 1f, area.yMax - 1f), "▶ " + title);
        }
#endif
    }
}
