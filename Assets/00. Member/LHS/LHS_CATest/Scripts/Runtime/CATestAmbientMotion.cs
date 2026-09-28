using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace LHS_CATest {
    /// <summary>
    /// 프랍에 생명감을 주는 공용 모션. 흔들림(sway), 둥실거림(bob), 수평 흐름(drift), 빛 깜빡임(flicker)을 조합해서 쓴다.
    /// 위상(phase)을 위치로부터 계산해 같은 프랍들이 똑같이 움직이지 않게 한다.
    /// </summary>
    public sealed class CATestAmbientMotion : MonoBehaviour {
        [Header("Sway (회전 흔들림, 해초/풀/덩굴)")]
        public float swayAngle;
        public float swaySpeed = 1f;

        [Header("Bob (위아래 둥실)")]
        public float bobHeight;
        public float bobSpeed = 1f;

        [Header("Drift (수평 흐름, 안개/구름)")]
        public float driftSpeed;
        public float driftRange = 20f;

        [Header("Flicker (Light2D / SpriteRenderer 알파)")]
        public Light2D flickerLight;
        public float flickerAmount;
        public SpriteRenderer pulseSprite;
        public float pulseAmount;

        private Vector3 _base;
        private Quaternion _baseRot;
        private float _phase;
        private float _baseIntensity;
        private float _baseAlpha;

        private void Awake() {
            _base = transform.localPosition;
            _baseRot = transform.localRotation;
            _phase = transform.position.x * 0.37f + transform.position.y * 0.19f;
            if (flickerLight != null) _baseIntensity = flickerLight.intensity;
            if (pulseSprite != null) _baseAlpha = pulseSprite.color.a;
        }

        private void Update() {
            var t = Time.time;
            var pos = _base;
            if (bobHeight != 0f) pos.y += Mathf.Sin(t * bobSpeed + _phase) * bobHeight;
            if (driftSpeed != 0f) pos.x += Mathf.PingPong(t * driftSpeed + _phase * 3f, driftRange) - driftRange * 0.5f;
            transform.localPosition = pos;
            if (swayAngle != 0f)
                transform.localRotation = _baseRot * Quaternion.Euler(0, 0,
                    Mathf.Sin(t * swaySpeed + _phase) * swayAngle + Mathf.Sin(t * swaySpeed * 2.3f + _phase) * swayAngle * 0.25f);
            if (flickerLight != null && flickerAmount != 0f)
                flickerLight.intensity = _baseIntensity * (1f + (Mathf.PerlinNoise(t * 2.5f, _phase) - 0.5f) * 2f * flickerAmount);
            if (pulseSprite != null && pulseAmount != 0f) {
                var c = pulseSprite.color;
                c.a = Mathf.Clamp01(_baseAlpha * (1f + Mathf.Sin(t * 1.3f + _phase) * pulseAmount));
                pulseSprite.color = c;
            }
        }
    }
}
