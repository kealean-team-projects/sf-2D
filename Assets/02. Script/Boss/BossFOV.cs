using System;
using System.Threading;
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
        [SerializeField] [Range(0f, 180f)] private float sweepAngle = 45f;
        [SerializeField] [Min(0.1f)] private float sweepDuration = 3f;

        [SerializeField] private Light2D scanLight;

        [SerializeField] private Transform shadowRoot;
        private Tween _angleTween;
        private bool _canSweep;


        private float _currentViewAngle;
        private bool _isScanning;
        private Quaternion _scanRotation;
        private Collider2D[] _shadowColliders;
        private ShadowCasterController[] _shadows;
        private float _sweepTime;

        private float scanDir = 1f;

        private void Awake() {
            _scanRotation = scanLight.transform.localRotation;
            scanLight.enabled = false;
            scanLight.pointLightOuterRadius = viewDistance;
            SetViewAngle(0f);
        }

        private void Start() {
            var root = shadowRoot != null ? shadowRoot : transform.root;
            _shadows = root.GetComponentsInChildren<ShadowCasterController>(true);
            _shadowColliders = new Collider2D[_shadows.Length];
            for (var i = 0; i < _shadows.Length; i++)
                _shadowColliders[i] = _shadows[i].GetComponent<Collider2D>();
        }

        private void Update() {
            if (!_isScanning || !_canSweep || scanLight == null) return;

            _sweepTime += Time.deltaTime;

            var progress = Mathf.PingPong(_sweepTime / sweepDuration, 1f);
            var angle = Mathf.SmoothStep(0f, sweepAngle, progress);

            scanLight.transform.rotation =
                Quaternion.Euler(0f, 0f, 180f + angle * scanDir);
        }


        private void LateUpdate() {
            if (_shadows == null || scanLight == null) return;


            for (var i = 0; i < _shadows.Length; i++) {
                var shadow = _shadows[i];
                if (shadow == null || !shadow.isActiveAndEnabled) continue;

                var collider = _shadowColliders[i];
                var bounds = collider != null && collider.enabled &&
                             collider.compositeOperation == Collider2D.CompositeOperation.None
                    ? collider.bounds
                    : shadow.GetShadowBounds();
                var detected = scanLight.enabled
                               && _currentViewAngle > 0f
                               && OverlapsVision(bounds);

                shadow.SetShadowActive(detected);
            }
        }

        private void OnDisable() {
            _angleTween.Stop();
            _isScanning = false;
            SetViewAngle(0f);

            if (scanLight != null) {
                scanLight.enabled = false;
                scanLight.transform.localRotation = _scanRotation;
            }

            if (_shadows == null) return;

            foreach (var shadow in _shadows)
                if (shadow != null && shadow.isActiveAndEnabled)
                    shadow.SetShadowActive(false);
        }


        private void OnDestroy() {
            _angleTween.Stop();
        }


#if UNITY_EDITOR
        private void OnDrawGizmosSelected() {
            if (scanLight == null) return;

            var origin = scanLight.transform.position;
            var forward = scanLight.transform.up;
            var angle = Application.isPlaying ? _currentViewAngle : viewAngle;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(origin, viewDistance);

            var left = Quaternion.Euler(0f, 0f, angle * 0.5f) * forward;
            var right = Quaternion.Euler(0f, 0f, -angle * 0.5f) * forward;

            Gizmos.DrawRay(origin, left * viewDistance);
            Gizmos.DrawRay(origin, right * viewDistance);
        }
#endif

        private bool OverlapsVision(Bounds bounds) {
            Vector2 origin = scanLight.transform.position;
            var closest = new Vector2(
                Mathf.Clamp(origin.x, bounds.min.x, bounds.max.x),
                Mathf.Clamp(origin.y, bounds.min.y, bounds.max.y));
            if ((closest - origin).sqrMagnitude > viewDistance * viewDistance)
                return false;

            var direction = (Vector2)bounds.center - origin;
            var distance = direction.magnitude;
            var radius = ((Vector2)bounds.extents).magnitude;
            if (distance <= radius) return true;

            // Enclose the bounds in a circle so partially visible terrain is never missed.
            var margin = Mathf.Asin(Mathf.Clamp01(radius / distance)) * Mathf.Rad2Deg;
            return Vector2.Angle(scanLight.transform.up, direction)
                   <= _currentViewAngle * 0.5f + margin;
        }

        public bool CanSee(Transform target) {
            if (target == null || scanLight == null ||
                !_isScanning || _currentViewAngle <= 0f)
                return false;

            Vector2 origin = scanLight.transform.position;
            var direction = (Vector2)target.position - origin;
            var distance = direction.magnitude;

            if (distance > viewDistance) return false;
            if (Vector2.Angle(scanLight.transform.up, direction) >
                _currentViewAngle * 0.5f)
                return false;

            var hit = Physics2D.Raycast(
                origin, direction.normalized, distance, whatIsBlock);

            var visible = hit.collider == null || hit.transform == target ||
                          hit.transform.IsChildOf(target);
            var end = hit.collider != null ? hit.point : (Vector2)target.position;
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
                _canSweep = false;
                _sweepTime = 0f;
                scanLight.transform.rotation = Quaternion.Euler(0f, 0f, 180f);
                _isScanning = true;
                scanLight.enabled = true;
            }

            _angleTween = Tween.Custom(_currentViewAngle, visible ? viewAngle : 0f, visible
                ? 1.5f
                : 0.75f, SetViewAngle, Ease.InExpo).OnComplete(() => {
                if (visible) _canSweep = true;
                if (!visible) {
                    _isScanning = false;
                    if (scanLight != null) scanLight.enabled = false;
                }
            });
        }

        public async UniTask Open(CancellationToken token) {
            Show(true);
            await UniTask.WaitUntil(() => !_angleTween.isAlive, cancellationToken: token);
        }

        public async UniTask Close(CancellationToken token, Action scan = null) {
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


        public void SetDetected(bool detected) {
            scanLight.color = detected ? detectedColor : normalColor;
        }

        public void SetScanDir(bool spawnedLeft) {
            scanDir = spawnedLeft ? 1f : -1f;
        }
    }
}