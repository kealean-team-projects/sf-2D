using UnityEngine;

namespace LHS_CATest {
    /// <summary>
    /// CATest 가 실행 중에 불러 쓰는 "원본 UI 프리팹" 참조 목록.
    /// ■ 위치: LHS_CATest/Resources/CATestUI/CATest_UIRefs.asset (Resources 에 있어야 빌드에서도 찾을 수 있음)
    /// ■ 원본 프리팹(Assets/03. Prefabs/...)은 Resources 폴더 밖이라 코드가 직접 못 찾는다
    ///   → 이 에셋이 그 프리팹을 "참조"로 들고 있어서, 코드는 이 에셋만 Resources.Load 하면 된다.
    /// </summary>
    [CreateAssetMenu(menuName = "LHS_CATest/UI Refs", fileName = "CATest_UIRefs")]
    public sealed class CATestUIRefs : ScriptableObject {
        public const string ResourcePath = "CATestUI/CATest_UIRefs";

        [Tooltip("켜면 기존 글자형 기력 HUD 대신 StaminaUI 프리팹(플레이어 머리 위 게이지)을 사용")]
        public bool useStaminaUI = true;

        [Tooltip("Assets/03. Prefabs/StaminaUI.prefab")]
        public GameObject staminaUIPrefab;

        [Tooltip("기력 게이지 캔버스의 정렬 순서(작을수록 다른 UI 뒤에 그려짐). CATest HUD=400")]
        public int staminaSortingOrder = 5;

        private static CATestUIRefs _cached;
        public static CATestUIRefs Get() {
            if (_cached == null) _cached = Resources.Load<CATestUIRefs>(ResourcePath);
            return _cached;
        }
    }
}
