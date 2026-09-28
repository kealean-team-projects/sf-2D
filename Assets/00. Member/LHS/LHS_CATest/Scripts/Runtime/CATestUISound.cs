using UnityEngine;
using UnityEngine.EventSystems;

namespace LHS_CATest {
    /// <summary>
    /// UGUI 버튼/슬라이더에 붙여 두면 선택이 옮겨 올 때 "ui_move", 누를 때 "ui_select" 효과음을 낸다.
    /// (소리 파일은 CATestAudioLibrary 의 같은 Key 칸. 비어 있으면 조용함)
    /// 창이 열리며 코드로 첫 버튼을 선택하는 순간에는 소리를 내지 않도록, 켜진 직후 0.15초는 무시한다.
    /// </summary>
    public sealed class CATestUISound : MonoBehaviour, ISelectHandler, ISubmitHandler, IPointerClickHandler, IPointerEnterHandler {
        public bool playSelect = true;
        private float _enabledAt;
        private static float _lastMove;

        private void OnEnable() => _enabledAt = Time.unscaledTime;

        public void OnSelect(BaseEventData e) {
            if (Time.unscaledTime - _enabledAt < 0.15f) return;
            if (Time.unscaledTime - _lastMove < 0.05f) return;
            _lastMove = Time.unscaledTime;
            CATestAudio.PlayUi("ui_move");
        }

        public void OnPointerEnter(PointerEventData e) {
            if (Time.unscaledTime - _lastMove < 0.05f) return;
            _lastMove = Time.unscaledTime;
            CATestAudio.PlayUi("ui_move");
        }

        public void OnSubmit(BaseEventData e) { if (playSelect) CATestAudio.PlayUi("ui_select"); }
        public void OnPointerClick(PointerEventData e) { if (playSelect) CATestAudio.PlayUi("ui_select"); }
    }
}
