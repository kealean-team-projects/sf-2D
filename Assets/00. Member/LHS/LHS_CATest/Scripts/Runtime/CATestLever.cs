using _02._Script._01_Players;
using _02._Script._04_Interaction;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace LHS_CATest {
    /// <summary>
    /// 레버(E로 상호작용). 기존 InteractBase를 상속해서 플레이어 Interactor가 그대로 찾고, 테두리 강조도 그대로 쓴다.
    ///  Toggle : 누를 때마다 켜짐/꺼짐
    ///  Timed  : 켜면 duration초 뒤 자동으로 꺼짐 (그동안 달려서 문을 통과해야 함)
    ///  OneShot: 한 번 켜면 계속 켜짐
    /// </summary>
    public sealed class CATestLever : InteractBase, ICATestSignal {
        public enum Mode { Toggle, Timed, OneShot }

        [SerializeField] private Mode mode = Mode.Toggle;
        [SerializeField] private float duration = 5f;
        [SerializeField] private Transform handle;
        [SerializeField] private float offAngle = 35f;
        [SerializeField] private float onAngle = -35f;
        [SerializeField] private Light2D lamp;
        [SerializeField] private ParticleSystem pullBurst;
        [SerializeField] private string labelOn = "당기기";
        [SerializeField] private string labelOff = "되돌리기";

        private float _timer;
        public bool IsOn { get; private set; }
        public string PromptText => mode == Mode.Toggle && IsOn ? labelOff : labelOn;

        public override void Interact(Player owner) {
            switch (mode) {
                case Mode.Toggle: IsOn = !IsOn; break;
                case Mode.Timed:
                    IsOn = true;
                    _timer = duration;
                    break;
                case Mode.OneShot:
                    if (IsOn) return;
                    IsOn = true;
                    break;
            }
            if (pullBurst != null) pullBurst.Play(true);
        }

        private void Update() {
            if (mode == Mode.Timed && IsOn) {
                _timer -= Time.deltaTime;
                if (_timer <= 0f) IsOn = false;
            }
            if (handle != null) {
                var target = Quaternion.Euler(0f, 0f, IsOn ? onAngle : offAngle);
                handle.localRotation = Quaternion.Slerp(handle.localRotation, target, Time.deltaTime * 10f);
            }
            if (lamp != null) {
                var blink = 1f;
                if (mode == Mode.Timed && IsOn) {
                    // 남은 시간이 줄수록 빠르게 깜빡여 긴장감을 준다
                    var k = Mathf.Clamp01(_timer / duration);
                    blink = 0.55f + 0.45f * Mathf.Sign(Mathf.Sin(Time.time * Mathf.Lerp(22f, 5f, k)));
                }
                lamp.intensity = Mathf.Lerp(lamp.intensity, (IsOn ? 1.3f : 0.35f) * blink, Time.deltaTime * 10f);
                lamp.color = IsOn ? new Color(1f, 0.85f, 0.45f) : new Color(0.6f, 0.75f, 1f);
            }
        }
    }
}
