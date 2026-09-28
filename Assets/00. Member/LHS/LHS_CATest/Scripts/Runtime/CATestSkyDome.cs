using UnityEngine;

namespace LHS_CATest {
    /// <summary>
    /// 황혼 하늘(일식) 사각형을 카메라 뒤 아주 먼 곳에 붙여 두는 스크립트.
    ///
    /// 왜 필요한가
    ///  - 하늘은 무한히 멀리 있어야 한다. 월드에 고정해 두면 원근 카메라가 옆으로 움직일 때 일식이 같이 흘러가 버린다.
    ///  - 그래서 매 프레임 사각형의 x, y 를 카메라 위치에 맞춘다(follow = 1 이면 완전히 고정, 0.97 이면 아주 약간만 흐름).
    ///  - z 는 고정(아주 멀리). 카메라 거리(줌)가 바뀌어도 화면을 다 덮도록 사각형을 넉넉하게 만든다.
    ///
    /// 부가 기능: 일식의 "화면 x 좌표"를 전역 셰이더 값 _TW_EclipseScreenX 로 넘긴다
    ///  → 거울 물(CATest_MirrorWater)이 일식 바로 아래에 반사줄을 그린다.
    /// 이 구역(areaMinX~areaMaxX) 밖에서는 하늘을 끈다(다른 챕터 배경을 가리지 않게).
    /// </summary>
    [ExecuteAlways]
    public sealed class CATestSkyDome : MonoBehaviour {
        [SerializeField] private float follow = 0.985f;
        [SerializeField] private Vector2 eclipseUV = new(0.62f, 0.72f);
        [SerializeField] private float areaMinX = 1300f;
        [SerializeField] private float areaMaxX = 2200f;
        [SerializeField] private float baseY = 20f;

        private SpriteRenderer _sr;
        private static readonly int EclipseScreenX = Shader.PropertyToID("_TW_EclipseScreenX");

        private void OnEnable() => _sr = GetComponent<SpriteRenderer>();

        private void LateUpdate() {
            var cam = Camera.main;
            if (cam == null) return;
            var cp = cam.transform.position;
            if (Application.isPlaying && _sr != null) _sr.enabled = cp.x > areaMinX - 120f && cp.x < areaMaxX + 120f;
            var p = transform.position;
            // 카메라를 거의 그대로 따라감 (y 는 기준 높이에서 조금만 따라가도록 — 땅이 낮아지면 하늘도 살짝 내려감)
            p.x = cp.x * follow + areaMinX * (1f - follow);
            p.y = baseY + (cp.y - baseY) * follow;
            transform.position = p;

            // 일식의 월드 위치 → 화면 x
            if (_sr != null && _sr.sprite != null) {
                var b = _sr.bounds;
                var world = new Vector3(Mathf.Lerp(b.min.x, b.max.x, eclipseUV.x), Mathf.Lerp(b.min.y, b.max.y, eclipseUV.y), b.center.z);
                var vp = cam.WorldToViewportPoint(world);
                Shader.SetGlobalFloat(EclipseScreenX, vp.x);
            }
        }
    }
}
