using UnityEngine;
using UnityEngine.InputSystem;

namespace LHS_CATest {
    /// <summary>
    /// ESC 한 번이 여러 창을 동시에 닫지 않도록 "이번 프레임의 ESC 는 누가 이미 썼다"를 기록한다.
    /// 가장 위에 떠 있는 창(확인 창 → 설정 창 → 일시정지 메뉴 순)이 먼저 Update 되도록 실행 순서를 앞당겨 두고,
    /// 각 창은 ConsumeEscape() 가 true 일 때만 반응한다.
    /// </summary>
    public static class CATestUIInput {
        private static int _consumedFrame = -1;

        public static bool ConsumeEscape() {
            var kb = Keyboard.current;
            if (kb == null || !kb.escapeKey.wasPressedThisFrame) return false;
            if (_consumedFrame == Time.frameCount) return false;
            _consumedFrame = Time.frameCount;
            return true;
        }

        /// <summary>확인/진행 키(E, Enter, Space) 또는 마우스 왼쪽 클릭.</summary>
        public static bool SubmitPressed() {
            var kb = Keyboard.current;
            var m = Mouse.current;
            return (kb != null && (kb.eKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame ||
                                   kb.numpadEnterKey.wasPressedThisFrame))
                   || (m != null && m.leftButton.wasPressedThisFrame);
        }

        public static bool AnyKeyPressed() {
            var kb = Keyboard.current;
            var m = Mouse.current;
            return (kb != null && kb.anyKey.wasPressedThisFrame) || (m != null && m.leftButton.wasPressedThisFrame);
        }
    }
}
