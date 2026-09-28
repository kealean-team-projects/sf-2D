using _02._Script._04_Interaction;
using UnityEngine;

namespace LHS_CATest {
    /// <summary>
    /// 등불(InteractLight)에 붙이면 켜진 뒤 duration초 후 자동으로 꺼진다. 꺼지기 직전에는 깜빡여서 경고한다.
    /// 빛 발판(CATestLightBloomPlatform)과 묶으면 "제한 시간 안에 건너기" 퍼즐이 된다.
    /// </summary>
    [RequireComponent(typeof(InteractLight))]
    public sealed class CATestLampTimer : MonoBehaviour {
        [SerializeField] private float duration = 7f;
        [SerializeField] private float warnTime = 2f;

        private InteractLight _lamp;
        private float _timer;
        private bool _wasOn;

        private void Awake() => _lamp = GetComponent<InteractLight>();

        private void Update() {
            if (_lamp == null) return;
            var on = _lamp.IsActive;
            if (on && !_wasOn) _timer = duration;
            _wasOn = on;
            if (!on) return;
            _timer -= Time.deltaTime;
            if (_timer <= 0f) {
                _lamp.SetBrightness(1f);
                _lamp.TurnOff();
                _wasOn = false;
                return;
            }
            if (_timer < warnTime) {
                var f = Mathf.Lerp(20f, 6f, _timer / warnTime);
                _lamp.SetBrightness(Mathf.Sin(Time.time * f) > 0f ? 1f : 0.35f);
            }
            else _lamp.SetBrightness(1f);
        }
    }
}
