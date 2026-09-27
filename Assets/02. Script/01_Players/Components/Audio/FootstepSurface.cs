using UnityEngine;

namespace _02._Script._01_Players.Components.Audio {
    public enum FootstepMaterial { Default, Stone }

    [DisallowMultipleComponent]
    public sealed class FootstepSurface : MonoBehaviour {
        [SerializeField] private FootstepMaterial material = FootstepMaterial.Stone;
        [Tooltip("통합 콜라이더가 겹쳐 감지될 때 참조할 개별 바닥 재질입니다.")]
        [SerializeField] private FootstepSurface[] surfaceOverrides = System.Array.Empty<FootstepSurface>();
        [SerializeField, Min(0f)] private float surfaceTolerance = 0.05f;
        private Collider2D surfaceCollider;
        public FootstepMaterial Material => material;

        public FootstepMaterial MaterialAt(Vector2 contactPoint) {
            var result = material;
            float closest = surfaceTolerance * surfaceTolerance;
            foreach (var surface in surfaceOverrides) {
                if (surface == null || surface == this || !surface.isActiveAndEnabled) continue;
                if (surface.surfaceCollider == null) surface.surfaceCollider = surface.GetComponent<Collider2D>();
                var collider = surface.surfaceCollider;
                if (collider == null || !collider.enabled || collider.isTrigger) continue;
                float distance = (collider.ClosestPoint(contactPoint) - contactPoint).sqrMagnitude;
                if (distance > closest) continue;
                closest = distance;
                result = surface.material;
            }
            return result;
        }
    }
}
