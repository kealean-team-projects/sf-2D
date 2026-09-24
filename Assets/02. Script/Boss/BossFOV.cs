using System;
using System.Threading;
using _02._Script._04_Interaction;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace _02._Script.Boss {
    public class BossFOV : MonoBehaviour {
        [SerializeField] [Min(0f)] private float viewDistance = 10f;
        [SerializeField] [Range(0f, 360f)] private float viewAngle = 90f;
        [SerializeField] private LayerMask whatIsBlock;

        private float _currentViewAngle;
        private Tween _angleTween;
        
        [SerializeField] private Light2D scanLight;
        private bool _isScanning;

        private BossRoom boss;
        private InteractLight[] _lights;

        private void Awake() {
            scanLight.enabled = false;
            scanLight.pointLightOuterRadius = viewDistance;
            SetViewAngle(0f);
        }

        private void Start()
        {
            if (_lights == null)
                _lights = transform.root.GetComponent<BossRoom>().Lights;
        }


        private void OnDestroy() {
            _angleTween.Stop();
        }
        
        
        private void LateUpdate() {
            if (_lights == null || scanLight == null) return;

            foreach (var light in _lights) {
                if (light == null) continue;

                Vector2 direction =
                    light.transform.position - scanLight.transform.position;

                bool detected = scanLight.enabled
                                && _currentViewAngle > 0f
                                && direction.sqrMagnitude <= viewDistance * viewDistance
                                && Vector2.Angle(scanLight.transform.up, direction)
                                <= _currentViewAngle * 0.5f;

                light.SetShadowActive(detected);
            }
        }
        
        private void OnDisable() {
            _angleTween.Stop();
            _isScanning = false;
            SetViewAngle(0f);

            if (scanLight != null)
                scanLight.enabled = false;

            if (_lights == null) return;

            foreach (var light in _lights) {
                if (light != null)
                    light.SetShadowActive(false);
            }
        }
        

#if UNITY_EDITOR
        private void OnDrawGizmosSelected() {
            if (scanLight == null) return;

            Vector3 origin = scanLight.transform.position;
            Vector3 forward = scanLight.transform.up;
            float angle = Application.isPlaying ? _currentViewAngle : viewAngle;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(origin, viewDistance);

            Vector3 left = Quaternion.Euler(0f, 0f, angle * 0.5f) * forward;
            Vector3 right = Quaternion.Euler(0f, 0f, -angle * 0.5f) * forward;

            Gizmos.DrawRay(origin, left * viewDistance);
            Gizmos.DrawRay(origin, right * viewDistance);
        }
#endif

        public bool CanSee(Transform target) {
            if (target == null || scanLight == null ||
                !_isScanning || _currentViewAngle <= 0f)
                return false;

            Vector2 origin = scanLight.transform.position;
            Vector2 direction = (Vector2)target.position - origin;
            float distance = direction.magnitude;

            if (distance > viewDistance) return false;
            if (Vector2.Angle(scanLight.transform.up, direction) >
                _currentViewAngle * 0.5f)
                return false;

            RaycastHit2D hit = Physics2D.Raycast(
                origin, direction.normalized, distance, whatIsBlock);

            Vector2 end = hit.collider != null ? hit.point : (Vector2)target.position;
            Debug.DrawLine(origin, end, hit.collider != null ? Color.red : Color.green);

            return hit.collider == null;
        }


        private void SetViewAngle(float angle) {
            _currentViewAngle = angle;

            if (scanLight == null) return;
            scanLight.pointLightInnerAngle = angle;
            scanLight.pointLightOuterAngle = angle;
        }

        public void Show(bool visible) {
            _angleTween.Stop();
            _isScanning = visible;

            if (scanLight == null) return;
            if (visible) scanLight.enabled = true;

            _angleTween = Tween.Custom(_currentViewAngle, visible ? 
                        viewAngle : 0f, visible 
                        ? 1.5f : 0.75f, SetViewAngle, Ease.InExpo).OnComplete(() => 
                        {
                            if (!visible && scanLight != null)
                                scanLight.enabled = false;
                        });
        }
        
        public async UniTask Close(CancellationToken token)
        {
            Show(false);
            await UniTask.WaitUntil(
                () => !_angleTween.isAlive, cancellationToken: token);
        }
    }
}
