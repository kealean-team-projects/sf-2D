using Cysharp.Threading.Tasks;
using Unity.Cinemachine;
using UnityEngine;

namespace LHS_CATest {
    // ※ MonoBehaviour 는 "클래스 이름 = 파일 이름" 이어야 Unity 가 씬에 붙은 컴포넌트를 불러올 수 있다.
    //   (예전에는 CATestCutscene.cs 안에 같이 있어서 "The associated script can not be loaded" 로 빠져 있었고,
    //    그래서 컷신 카메라 Focus 가 동작하지 않았다 → 별도 파일로 분리)
    /// <summary>
    /// 컷신 전용 카메라. CoreScene 에 하나. 평소에는 vcam 오브젝트가 꺼져 있다.
    /// Focus 로 목표 지점(target Transform 이동)을 비추고, Release 로 끈다.
    /// 흔들기(Shake)는 플레이어/컷신 카메라 둘 다에 적용되도록 Cinemachine 과 무관하게 메인 카메라 뒤에서 offset 을 준다.
    /// </summary>
    public sealed class CATestCutsceneCamera : MonoBehaviour {
        [SerializeField] private CinemachineCamera vcam;
        [SerializeField] private CinemachinePositionComposer composer;
        [SerializeField] private Transform target;
        [SerializeField] private float defaultDistance = 110f;

        public static CATestCutsceneCamera Instance { get; private set; }
        private CinemachineBrain _brain;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        private void Awake() {
            Instance = this;
            if (vcam != null) vcam.gameObject.SetActive(false);
        }

        private void OnDestroy() {
            if (Instance == this) Instance = null;
        }

        private CinemachineBrain Brain {
            get {
                if (_brain == null && Camera.main != null) _brain = Camera.main.GetComponent<CinemachineBrain>();
                return _brain;
            }
        }

        public void Focus(Vector3 point, float blend, float distance) {
            if (vcam == null || target == null) return;
            target.position = point;
            if (composer != null) composer.CameraDistance = distance > 0f ? distance : defaultDistance;
            if (Brain != null) Brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, blend);
            if (!vcam.gameObject.activeSelf) vcam.gameObject.SetActive(true);
        }

        /// <summary>이미 켜진 상태에서 목표만 옮길 때(카메라가 따라감).</summary>
        public void MoveTarget(Vector3 point) {
            if (target != null) target.position = point;
        }

        public void Release(float blend) {
            if (vcam == null) return;
            if (Brain != null) Brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, blend);
            vcam.gameObject.SetActive(false);
        }

        public async UniTaskVoid Shake(float strength, float duration) {
            var cam = Camera.main;
            if (cam == null) return;
            // Cinemachine 이 매 프레임 카메라 위치를 덮어쓰므로, 렌더 직전(LateUpdate 이후)에 흔들림 offset 을 더한다.
            for (var t = 0f; t < duration; t += Time.unscaledDeltaTime) {
                if (cam == null) return;
                var k = 1f - t / duration;
                cam.transform.position += (Vector3)(Random.insideUnitCircle * strength * k);
                await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
            }
        }
    }
}
