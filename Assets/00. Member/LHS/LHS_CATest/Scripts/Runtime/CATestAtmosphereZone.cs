using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace LHS_CATest {
    /// <summary>
    /// 맵 씬 안에 두는 "분위기/카메라 구역". CoreScene의 CATestAtmosphereDirector 가 플레이어 위치로 가중치를 계산해 섞는다.
    /// 씬이 언로드되면 OnDisable에서 자동으로 목록에서 빠진다(씬 간 직접 참조 없이 동작).
    /// </summary>
    public sealed class CATestAtmosphereZone : MonoBehaviour {
        public Rect area = new(0, 0, 40, 20);
        [Tooltip("area 바깥으로 이 거리만큼 서서히 섞인다")]
        public float blend = 12f;
        public int priority;

        [Header("Light / Sky")]
        public bool overrideLight = true;
        public Color globalLightColor = Color.white;
        public float globalLightIntensity = 0.8f;
        public Color backgroundColor = new(0.1f, 0.12f, 0.2f);

        [Header("Camera")]
        public bool overrideCamera;
        public float cameraDistance = 100f;
        public Vector3 targetOffset = new(0f, 1.5f, 0f);
        public Vector3 damping = new(1f, 1f, 1f);

        internal static readonly List<CATestAtmosphereZone> All = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => All.Clear();

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        public float Weight(Vector2 p) {
            var dx = Mathf.Max(area.xMin - p.x, 0f, p.x - area.xMax);
            var dy = Mathf.Max(area.yMin - p.y, 0f, p.y - area.yMax);
            var d = Mathf.Sqrt(dx * dx + dy * dy);
            if (d <= 0f) return 1f;
            return blend <= 0f ? 0f : Mathf.Clamp01(1f - d / blend);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos() {
            Gizmos.color = overrideCamera ? new Color(0.4f, 0.7f, 1f, 0.6f) : new Color(1f, 0.6f, 0.9f, 0.5f);
            Gizmos.DrawWireCube(area.center, area.size);
        }
#endif
    }

}
