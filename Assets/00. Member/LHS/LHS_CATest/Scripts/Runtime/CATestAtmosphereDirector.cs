using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace LHS_CATest {
    /// <summary>
    /// CoreScene에 하나. 전역 Light2D 색/세기, 카메라 배경색, Cinemachine 거리/오프셋/댐핑을 구역별로 부드럽게 전환한다.
    /// (맵 씬마다 Global Light를 두면 두 맵이 동시에 로드된 경계에서 전역광이 겹치므로 한 곳에서 관리한다.)
    /// </summary>
    public sealed class CATestAtmosphereDirector : MonoBehaviour {
        [SerializeField] private Transform target;
        [SerializeField] private Light2D globalLight;
        [SerializeField] private Camera cam;
        [SerializeField] private CinemachinePositionComposer composer;
        [SerializeField] private float lerpSpeed = 1.5f;

        [Header("Default")]
        [SerializeField] private Color defaultLightColor = new(0.8f, 0.85f, 1f);
        [SerializeField] private float defaultLightIntensity = 0.8f;
        [SerializeField] private Color defaultBackground = new(0.08f, 0.1f, 0.16f);
        [SerializeField] private float defaultDistance = 100f;
        [SerializeField] private Vector3 defaultOffset = new(0f, 1.5f, 0f);
        [SerializeField] private Vector3 defaultDamping = new(1f, 1f, 1f);

        private Color _lightColor;
        private float _intensity;
        private Color _bg;
        private float _dist;
        private Vector3 _offset;
        private Vector3 _damp;

        public void SetTarget(Transform t) => target = t;

        private void Start() {
            _lightColor = defaultLightColor;
            _intensity = defaultLightIntensity;
            _bg = defaultBackground;
            _dist = defaultDistance;
            _offset = defaultOffset;
            _damp = defaultDamping;
        }

        private void LateUpdate() {
            if (target == null || !target.gameObject.activeInHierarchy) return;
            Vector2 p = target.position;

            // 가중 평균: 기본값에 weight=0.0001로 시작해서 구역들을 섞는다.
            float wl = 0.0001f, wc = 0.0001f;
            var lc = defaultLightColor * wl; var li = defaultLightIntensity * wl; var bg = defaultBackground * wl;
            var cd = defaultDistance * wc; var co = defaultOffset * wc; var cdamp = defaultDamping * wc;
            foreach (var z in CATestAtmosphereZone.All) {
                var w = z.Weight(p);
                if (w <= 0f) continue;
                w *= 1f + z.priority;
                if (z.overrideLight) {
                    wl += w; lc += z.globalLightColor * w; li += z.globalLightIntensity * w; bg += z.backgroundColor * w;
                }
                if (z.overrideCamera) {
                    wc += w; cd += z.cameraDistance * w; co += z.targetOffset * w; cdamp += z.damping * w;
                }
            }
            var k = 1f - Mathf.Exp(-lerpSpeed * Time.deltaTime);
            _lightColor = Color.Lerp(_lightColor, lc / wl, k);
            _intensity = Mathf.Lerp(_intensity, li / wl, k);
            _bg = Color.Lerp(_bg, bg / wl, k);
            if (wc < 0.01f) { cd = defaultDistance; co = defaultOffset; cdamp = defaultDamping; wc = 1f; }
            _dist = Mathf.Lerp(_dist, cd / wc, k * 0.8f);
            _offset = Vector3.Lerp(_offset, co / wc, k);
            _damp = Vector3.Lerp(_damp, cdamp / wc, k * 2f);

            if (globalLight != null) {
                globalLight.color = _lightColor;
                globalLight.intensity = _intensity;
            }
            if (cam != null) cam.backgroundColor = _bg;
            if (composer != null) {
                composer.CameraDistance = _dist;
                composer.TargetOffset = _offset;
                composer.Damping = _damp;
            }
        }
    }
}
