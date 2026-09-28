using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace _02._Script {
    [RequireComponent(typeof(ShadowCaster2D))]
    public class ShadowCasterController : MonoBehaviour {
        private Matrix4x4 boundsMatrix;
        private Bounds cachedBounds;
        private bool hasBounds;
        private ShadowCaster2D shadowCaster;

        private void Awake() {
            shadowCaster = GetComponent<ShadowCaster2D>();
            shadowCaster.enabled = false;
        }

        private void OnDisable() {
            if (shadowCaster != null) shadowCaster.enabled = false;
        }

        public Bounds GetShadowBounds() {
            if (shadowCaster == null) shadowCaster = GetComponent<ShadowCaster2D>();
            var matrix = transform.localToWorldMatrix;
            if (hasBounds && matrix == boundsMatrix) return cachedBounds;
            var points = shadowCaster.shapePath;
            var bounds = new Bounds(transform.position, Vector3.zero);
            if (points == null || points.Length == 0) return bounds;
            bounds = new Bounds(transform.TransformPoint(points[0]), Vector3.zero);
            foreach (var point in points) bounds.Encapsulate(transform.TransformPoint(point));
            cachedBounds = bounds;
            boundsMatrix = matrix;
            hasBounds = true;
            return bounds;
        }

        public void SetShadowActive(bool active) {
            shadowCaster.enabled = active;
        }
    }
}