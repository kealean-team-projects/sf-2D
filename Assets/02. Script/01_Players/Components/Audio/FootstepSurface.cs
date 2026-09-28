using System;
using System.Collections.Generic;
using UnityEngine;

namespace _02._Script._01_Players.Components.Audio {
    public enum FootstepMaterial {
        Default,
        Stone
    }

    [DisallowMultipleComponent]
    public sealed class FootstepSurface : MonoBehaviour {
        [SerializeField] private FootstepMaterial material = FootstepMaterial.Stone;

        [Tooltip("통합 콜라이더가 겹쳐 감지될 때 참조할 개별 바닥 재질입니다.")] [SerializeField]
        private FootstepSurface[] surfaceOverrides = Array.Empty<FootstepSurface>();

        [Tooltip("같은 부모에 여러 지형 콜라이더가 있을 때 돌 소리를 적용할 도형만 지정합니다.")] [SerializeField]
        private PolygonCollider2D[] stoneRegions = Array.Empty<PolygonCollider2D>();

        [SerializeField] [Min(0f)] private float surfaceTolerance = 0.05f;
        private readonly List<Vector2> regionPath = new();
        private Collider2D surfaceCollider;
        public FootstepMaterial Material => material;

        public FootstepMaterial MaterialAt(Vector2 contactPoint) {
            var result = material;
            var closest = surfaceTolerance * surfaceTolerance;
            foreach (var region in stoneRegions) {
                if (region == null || !region.enabled || !region.gameObject.activeInHierarchy ||
                    region.isTrigger) continue;
                if (IsInsideRegion(region, contactPoint)) {
                    result = FootstepMaterial.Stone;
                    break;
                }
            }

            foreach (var surface in surfaceOverrides) {
                if (surface == null || surface == this || !surface.isActiveAndEnabled) continue;
                if (surface.surfaceCollider == null) surface.surfaceCollider = surface.GetComponent<Collider2D>();
                var collider = surface.surfaceCollider;
                if (collider == null || !collider.enabled || collider.isTrigger) continue;
                var distance = (collider.ClosestPoint(contactPoint) - contactPoint).sqrMagnitude;
                if (distance > closest) continue;
                closest = distance;
                result = surface.material;
            }

            return result;
        }

        private bool IsInsideRegion(PolygonCollider2D region, Vector2 point) {
            // ClosestPoint on a merged source can query the entire CompositeCollider2D.
            // Read the source paths directly to keep other map sections out of this region.
            var inside = false;
            for (var path = 0; path < region.pathCount; path++) {
                region.GetPath(path, regionPath);
                if (regionPath.Count < 3) continue;
                Vector2 a = region.transform.TransformPoint(regionPath[regionPath.Count - 1] + region.offset);
                foreach (var vertex in regionPath) {
                    Vector2 b = region.transform.TransformPoint(vertex + region.offset);
                    var edge = b - a;
                    var t = edge.sqrMagnitude > 0f
                        ? Mathf.Clamp01(Vector2.Dot(point - a, edge) / edge.sqrMagnitude)
                        : 0f;
                    if ((point - (a + t * edge)).sqrMagnitude <= surfaceTolerance * surfaceTolerance) return true;
                    if (a.y > point.y != b.y > point.y &&
                        point.x < a.x + (point.y - a.y) * (b.x - a.x) / (b.y - a.y))
                        inside = !inside;
                    a = b;
                }
            }

            return inside;
        }
    }
}