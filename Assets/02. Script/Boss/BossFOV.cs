using System;
using System.Threading;
using _02._Script._04_Interaction;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace _02._Script.Boss {
    public class BossFOV : MonoBehaviour {
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color detectedColor = Color.red;
        
        [SerializeField] [Min(0f)] private float viewDistance = 10f;
        [SerializeField] [Range(0f, 360f)] private float viewAngle = 90f;
        [SerializeField] private LayerMask whatIsBlock;
        

        private float _currentViewAngle;
        private Tween _angleTween;
        
        [SerializeField] private Light2D scanLight;
        private bool _isScanning;

        [SerializeField] private Transform shadowRoot;
        private ShadowCasterController[] _shadows;
        private Collider2D[] _shadowColliders;

        private void Awake() {
            scanLight.enabled = false;
            scanLight.pointLightOuterRadius = viewDistance;
            SetViewAngle(0f);
        }

        private void Start()
        {
            var root = shadowRoot != null ? shadowRoot : transform.root;
            _shadows = root.GetComponentsInChildren<ShadowCasterController>(true);
            _shadowColliders = new Collider2D[_shadows.Length];
            for (int i = 0; i < _shadows.Length; i++)
                _shadowColliders[i] = _shadows[i].GetComponent<Collider2D>();
        }


        private void OnDestroy() {
            _angleTween.Stop();
        }
        
        
        private void LateUpdate() {
            if (_shadows == null || scanLight == null) return;

            for (int i = 0; i < _shadows.Length; i++) {
                var shadow = _shadows[i];
                if (shadow == null || !shadow.isActiveAndEnabled) continue;

                var collider = _shadowColliders[i];
                var bounds = collider != null && collider.enabled
                    ? collider.bounds
                    : new Bounds(shadow.transform.position, Vector3.zero);
                bool detected = scanLight.enabled
                                && _currentViewAngle > 0f
                                && OverlapsVision(bounds);

                shadow.SetShadowActive(detected);
            }
        }

        private bool OverlapsVision(Bounds bounds)
        {
            Vector2 origin = scanLight.transform.position;
            Vector2 closest = new Vector2(
                Mathf.Clamp(origin.x, bounds.min.x, bounds.max.x),
                Mathf.Clamp(origin.y, bounds.min.y, bounds.max.y));
            if ((closest - origin).sqrMagnitude > viewDistance * viewDistance)
                return false;

            Vector2 direction = (Vector2)bounds.center - origin;
            float distance = direction.magnitude;
            float radius = ((Vector2)bounds.extents).magnitude;
            if (distance <= radius) return true;

            // Enclose the bounds in a circle so partially visible terrain is never missed.
            float margin = Mathf.Asin(Mathf.Clamp01(radius / distance)) * Mathf.Rad2Deg;
            return Vector2.Angle(scanLight.transform.up, direction)
                   <= _currentViewAngle * 0.5f + margin;
        }
        
        private void OnDisable() {
            _angleTween.Stop();
            _isScanning = false;
            SetViewAngle(0f);

            if (scanLight != null)
                scanLight.enabled = false;

            if (_shadows == null) return;

            foreach (var shadow in _shadows) {
                if (shadow != null && shadow.isActiveAndEnabled)
                    shadow.SetShadowActive(false);
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

            bool visible = hit.collider == null || hit.transform == target ||
                           hit.transform.IsChildOf(target);
            Vector2 end = hit.collider != null ? hit.point : (Vector2)target.position;
            Debug.DrawLine(origin, end, visible ? Color.green : Color.red);

            return visible;
        }


        private void SetViewAngle(float angle) {
            _currentViewAngle = angle;

            if (scanLight == null) return;
            scanLight.pointLightInnerAngle = angle;
            scanLight.pointLightOuterAngle = angle;
        }

        public void Show(bool visible) {
            _angleTween.Stop();

            if (scanLight == null) return;
            if (visible) {
                _isScanning = true;
                scanLight.enabled = true;
            }

            _angleTween = Tween.Custom(_currentViewAngle, visible ? 
                        viewAngle : 0f, visible 
                        ? 1.5f : 0.75f, SetViewAngle, Ease.InExpo).OnComplete(() => 
                        {
                            if (!visible) {
                                _isScanning = false;
                                if (scanLight != null) scanLight.enabled = false;
                            }
                        });
        }
        
        public async UniTask Close(CancellationToken token, Action scan = null)
        {
            Show(false);
            try {
                while (_angleTween.isAlive) {
                    token.ThrowIfCancellationRequested();
                    scan?.Invoke();
                    await UniTask.NextFrame(token);
                }
            }
            finally {
                _angleTween.Stop();
                _isScanning = false;
                SetViewAngle(0f);
                if (scanLight != null) scanLight.enabled = false;
            }
        }
        
        

        public void SetDetected(bool detected)
        {
            scanLight.color = detected ? detectedColor : normalColor;
        }
    }
}
