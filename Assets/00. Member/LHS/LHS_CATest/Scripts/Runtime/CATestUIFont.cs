using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LHS_CATest {
    /// <summary>실행 시 Canvas 아래의 모든 Text 에 OS 한글 폰트를 끼운다(빌드 시 폰트 에셋이 없어서).</summary>
    public sealed class CATestUIFont : MonoBehaviour {
        private void Awake() => ApplyTo(transform);

        public static void ApplyTo(Transform root) {
            foreach (var t in root.GetComponentsInChildren<Text>(true))
                t.font = t.GetComponent<CATestUITitleText>() != null ? CATestUIKit.TitleFont : CATestUIKit.BodyFont;
        }
    }
}
