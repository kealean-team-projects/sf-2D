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
            if (!CATestSettings.ScreenShake) return; // 설정에서 화면 흔들림을 끈 경우
            if (CATestCutsceneCamera.Instance != null) CATestCutsceneCamera.Instance.Shake(strength, duration).Forget();
        }

        /// <summary>씬 전환(타이틀로 가기 등) 직전: 잠금 횟수만 0 으로(플레이어는 곧 사라지므로 입력맵은 건드리지 않음).</summary>
        public static void ResetState() => _lockCount = 0;

        public static UniTask Wait(float seconds) => UniTask.Delay(System.TimeSpan.FromSeconds(seconds), true);
    }

}
