using UnityEngine;

namespace LHS_CATest {
    /// <summary>
    /// 카메라 쪽(전경) 실루엣 나무가 플레이어를 가릴 때 반투명하게 만든다.
    ///
    /// 왜 필요한가: 전경 나무는 z &lt; 0(카메라에 더 가까움) + Architecture 정렬 레이어라 플레이어보다 앞에 그려진다.
    /// 입체감은 좋지만 플레이어가 줄기 뒤를 지나갈 때 캐릭터가 완전히 안 보이게 된다.
    ///
    /// 동작
    ///  - 매 프레임 줄기(첫 번째 자식 SpriteRenderer)의 월드 경계(bounds)를 화면 좌표로 투영한 사각형을 구하고
    ///    (원근 카메라라 z 가 다르면 화면 위치가 달라지므로 반드시 "화면"에서 비교해야 한다)
    ///  - 플레이어 몸 중심의 화면 좌표가 그 사각형 안(+여유 margin 픽셀)에 있으면 목표 알파 = hiddenAlpha
    ///  - 알파는 부드럽게 따라감(fadeSpeed)
    /// </summary>
    public sealed class CATestFgOccluder : MonoBehaviour {
        [SerializeField] private float hiddenAlpha = 0.28f;
        [SerializeField] private float fadeSpeed = 4f;
        [SerializeField] private float marginPx = 40f;

        private SpriteRenderer[] _renderers;
        private float[] _baseAlpha;
        private float _k = 1f;

        private void Awake() {
            _renderers = GetComponentsInChildren<SpriteRenderer>(true);
            _baseAlpha = new float[_renderers.Length];
            for (var i = 0; i < _renderers.Length; i++) _baseAlpha[i] = _renderers[i].color.a;
        }

        private void LateUpdate() {
            var cam = Camera.main;
            var p = CATestHUD.CurrentPlayer;
            var target = 1f;
            if (cam != null && p != null && _renderers.Length > 0) {
                var col = p.GetComponent<Collider2D>();
                var center = col != null ? col.bounds.center : p.transform.position;
                var ps = cam.WorldToScreenPoint(center);
                var min = new Vector2(float.MaxValue, float.MaxValue);
                var max = new Vector2(float.MinValue, float.MinValue);
                // 판정은 첫 번째 렌더러(줄기)만 — 잎 뭉치까지 넣으면 나무 아래를 지나기만 해도 흐려진다
                {
                    var r = _renderers[0];
                    var b = r.bounds;
                    for (var cx = 0; cx < 2; cx++)
                    for (var cy = 0; cy < 2; cy++) {
                        var s = cam.WorldToScreenPoint(new Vector3(cx == 0 ? b.min.x : b.max.x, cy == 0 ? b.min.y : b.max.y, b.center.z));
                        min = Vector2.Min(min, s);
                        max = Vector2.Max(max, s);
                    }
                }
                if (ps.z > 0f && ps.x > min.x - marginPx && ps.x < max.x + marginPx &&
                    ps.y > min.y - marginPx && ps.y < max.y + marginPx) target = hiddenAlpha;
            }
            if (Mathf.Approximately(_k, target)) return;
            _k = Mathf.MoveTowards(_k, target, fadeSpeed * Time.deltaTime);
            for (var i = 0; i < _renderers.Length; i++) {
                var r = _renderers[i];
                if (r == null) continue;
                var c = r.color;
                c.a = _baseAlpha[i] * _k;
                r.color = c;
            }
        }
    }
}
