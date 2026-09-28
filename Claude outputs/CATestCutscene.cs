using System.Reflection;
using _02._Script._01_Players;
using _02._Script._01_Players.Components;
using Cysharp.Threading.Tasks;
using Unity.Cinemachine;
using UnityEngine;

namespace LHS_CATest {
    /// <summary>
    /// 컷신 공통 기능.
    ///
    /// ■ 조작 막기 (LockInput)
    ///   플레이어 입력은 InputReader 안의 Input System 액션맵(Control.Player)으로 들어온다.
    ///   이 액션맵을 Disable 하면 키 입력이 아예 들어오지 않고, 누르고 있던 이동 키도 "취소" 콜백으로 0 이 된다.
    ///   원본 InputReader 를 고치지 않으려고 private 필드(_control)를 리플렉션으로 꺼내 쓴다.
    ///   여러 컷신이 겹쳐도 안전하도록 잠금 횟수(카운터)로 관리한다.
    ///
    /// ■ 컷신 시작/끝 (Begin / End)
    ///   Begin: 검은 띠 들어옴 + 조작 잠금 + 달리기 중지.   End: 검은 띠 빠짐 + 조작 복구.
    ///
    /// ■ 카메라 (Focus / Release)
    ///   CoreScene 의 CM_Cutscene(평소 꺼져 있는 Cinemachine 카메라)을 켜면 Cinemachine 이
    ///   "가장 최근에 켜진 카메라"로 부드럽게 블렌드한다. 끄면 다시 플레이어 카메라로 돌아온다.
    /// </summary>
    public static class CATestCutscene {
        private static int _lockCount;
        private static readonly FieldInfo ControlField =
            typeof(InputReader).GetField("_control", BindingFlags.Instance | BindingFlags.NonPublic);

        public static bool IsPlaying => _lockCount > 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _lockCount = 0;

        public static void Begin(bool letterbox = true) {
            if (letterbox) CATestHUD.SetLetterbox(true);
            LockInput(true);
        }

        public static void End(bool letterbox = true) {
            if (letterbox) CATestHUD.SetLetterbox(false);
            LockInput(false);
        }

        public static void LockInput(bool locked) {
            _lockCount = Mathf.Max(0, _lockCount + (locked ? 1 : -1));
            var p = CATestHUD.CurrentPlayer;
            if (p == null) return;
            var reader = p.GetComponentInChildren<InputReader>(true);
            if (reader == null || ControlField == null) return;
            if (ControlField.GetValue(reader) is not _01.Script.Player.Components.Control control) return;
            if (_lockCount > 0) {
                control.Player.Disable();
                p.SprintControl?.StopSprint();
            }
            else {
                control.Player.Enable();
            }
        }

        /// <summary>안전장치: 사망/리스폰 시 잠금이 남아 있으면 모두 푼다.</summary>
        public static void ForceUnlock() {
            _lockCount = 1;
            End();
        }

        public static void Focus(Vector3 worldPoint, float blend = 1.4f, float distance = -1f) {
            if (CATestCutsceneCamera.Instance != null) CATestCutsceneCamera.Instance.Focus(worldPoint, blend, distance);
        }

        public static void Release(float blend = 1.2f) {
            if (CATestCutsceneCamera.Instance != null) CATestCutsceneCamera.Instance.Release(blend);
        }

        public static void Shake(float strength = 0.5f, float duration = 0.4f) {
            if (CATestCutsceneCamera.Instance != null) CATestCutsceneCamera.Instance.Shake(strength, duration).Forget();
        }

        public static UniTask Wait(float seconds) => UniTask.Delay(System.TimeSpan.FromSeconds(seconds), true);
    }

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
