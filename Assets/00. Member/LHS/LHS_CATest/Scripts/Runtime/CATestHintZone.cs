using UnityEngine;

namespace LHS_CATest {
    /// <summary>영역 안에 있는 동안 화면 하단에 조작 힌트를 띄운다(튜토리얼 표지판 역할).</summary>
    public sealed class CATestHintZone : MonoBehaviour {
        public Rect area = new(0, 0, 10, 6);
        [TextArea] public string message = "C : 웅크리기";
        [Tooltip("한 번 본 뒤에는 다시 띄우지 않음")]
        public bool onlyOnce;

        private bool _inside;
        private bool _seen;

        private void Update() {
            var p = CATestHUD.PlayerTransform;
            var inside = p != null && area.Contains(p.position) && !(onlyOnce && _seen && !_inside);
            if (inside && !_inside) CATestHUD.ShowHint(this, message);
            if (!inside && _inside) {
                CATestHUD.HideHint(this);
                _seen = true;
            }
            _inside = inside;
        }

        private void OnDisable() {
            if (_inside) CATestHUD.HideHint(this);
            _inside = false;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos() {
            Gizmos.color = new Color(0.4f, 0.9f, 1f, 0.4f);
            Gizmos.DrawWireCube(area.center, area.size);
        }
#endif
    }
}
