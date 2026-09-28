using _02._Script._04_Interaction;
using UnityEngine;

namespace LHS_CATest {
    /// <summary>상호작용 안내(E) 옆에 띄울 문구. 등불처럼 켜짐/꺼짐이 있는 대상은 두 문구를 따로 줄 수 있다.</summary>
    public sealed class CATestInteractLabel : MonoBehaviour {
        [SerializeField] private string label = "상호작용";
        [SerializeField] private string labelWhenOn = "";

        public string Resolve(Component target) {
            if (!string.IsNullOrEmpty(labelWhenOn)) {
                if (target is InteractLight l && l.IsActive) return labelWhenOn;
                if (target is ICATestSignal s && s.IsOn) return labelWhenOn;
            }
            return label;
        }
    }
}
