using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LHS_CATest {
    /// <summary>
    /// 마우스로 빈 곳을 클릭하면 UGUI 의 "선택된 버튼"이 비워져서 방향키/Enter 가 먹지 않게 되는 문제를 막는다.
    ///
    /// ■ 원리
    ///   EventSystem 은 클릭한 곳에 버튼이 없으면 currentSelectedGameObject 를 null 로 만든다.
    ///   키보드 내비게이션은 "지금 선택된 것"에서 출발하므로, 선택이 null 이면 방향키를 눌러도 아무 일도 없다.
    ///   → 마지막으로 선택돼 있던 버튼을 기억해 두었다가, 선택이 비어 있는 상태에서 방향키/확인 키가 눌리면 그 버튼을 다시 선택한다.
    ///
    /// ■ 씬에 따로 놓을 필요 없음: 게임 시작 시 자동으로 하나 만들어져 씬이 바뀌어도 유지된다(DontDestroyOnLoad).
    /// </summary>
    public sealed class CATestSelectionKeeper : MonoBehaviour {
        private GameObject _last;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create() {
            if (FindAnyObjectByType<CATestSelectionKeeper>() != null) return;
            var go = new GameObject("[CATest] SelectionKeeper");
            DontDestroyOnLoad(go);
            go.AddComponent<CATestSelectionKeeper>();
        }

        private void Update() {
            var es = EventSystem.current;
            if (es == null) return;
            var cur = es.currentSelectedGameObject;
            if (cur != null && cur.activeInHierarchy) {
                _last = cur;
                return;
            }
            if (!NavPressed()) return;
            if (_last == null || !_last.activeInHierarchy) return;
            // 창이 닫혀 있거나(CanvasGroup.interactable=false) 비활성 버튼이면 되살리지 않음
            var sel = _last.GetComponent<Selectable>();
            if (sel == null || !sel.IsInteractable()) return;
            es.SetSelectedGameObject(_last);
        }

        private static bool NavPressed() {
            var kb = Keyboard.current;
            if (kb == null) return false;
            return kb.upArrowKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame ||
                   kb.leftArrowKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame ||
                   kb.wKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame ||
                   kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame;
        }
    }
}
