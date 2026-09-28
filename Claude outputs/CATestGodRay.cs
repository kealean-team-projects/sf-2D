using UnityEngine;

namespace LHS_CATest {
    /// <summary>
    /// 햇살(God Ray) 한 줄기의 개별 설정.
    /// 모든 햇살이 같은 머티리얼(M_CATest_GodRay)을 공유하되, 이 컴포넌트가 MaterialPropertyBlock 으로
    /// 시드·세기·폭·가닥 수 등을 햇살마다 다르게 넣어 준다 → 머티리얼을 복제하지 않고도 서로 다른 모양.
    /// [ExecuteAlways] 라서 에디터(씬 뷰)에서도 값이 바로 반영된다. Inspector 에서 값을 바꿔 보며 조정 가능.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class CATestGodRay : MonoBehaviour {
        [Tooltip("햇살마다 다른 무늬를 만드는 난수 시드")] public float seed;
        [Range(0f, 4f)] public float intensity = 1f;
        [Tooltip("광원(위) 쪽 폭 비율. 작을수록 위가 좁은 부채꼴")] [Range(0.05f, 1f)] public float topWidth = 0.35f;
        [Tooltip("가닥 촘촘함")] [Range(1f, 20f)] public float rayCount = 7f;
        [Tooltip("가닥이 옆으로 흐르는 속도")] [Range(0f, 0.5f)] public float raySpeed = 0.06f;
        [Range(0f, 2f)] public float dustAmount = 0.7f;
        [Range(0f, 0.6f)] public float flicker = 0.15f;
        [Range(0f, 2f)] public float core = 0.45f;

        private static readonly int SeedId = Shader.PropertyToID("_Seed");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int TopWidthId = Shader.PropertyToID("_TopWidth");
        private static readonly int RayCountId = Shader.PropertyToID("_RayCount");
        private static readonly int RaySpeedId = Shader.PropertyToID("_RaySpeed");
        private static readonly int DustId = Shader.PropertyToID("_DustAmount");
        private static readonly int FlickerId = Shader.PropertyToID("_Flicker");
        private static readonly int CoreId = Shader.PropertyToID("_Core");

        private SpriteRenderer _sr;
        private MaterialPropertyBlock _mpb;

        private void OnEnable() => Apply();
        private void OnValidate() => Apply();

        public void Apply() {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_sr == null) return;
            _mpb ??= new MaterialPropertyBlock();
            _sr.GetPropertyBlock(_mpb);
            _mpb.SetFloat(SeedId, seed);
            _mpb.SetFloat(IntensityId, intensity);
            _mpb.SetFloat(TopWidthId, topWidth);
            _mpb.SetFloat(RayCountId, rayCount);
            _mpb.SetFloat(RaySpeedId, raySpeed);
            _mpb.SetFloat(DustId, dustAmount);
            _mpb.SetFloat(FlickerId, flicker);
            _mpb.SetFloat(CoreId, core);
            _sr.SetPropertyBlock(_mpb);
        }
    }
}
