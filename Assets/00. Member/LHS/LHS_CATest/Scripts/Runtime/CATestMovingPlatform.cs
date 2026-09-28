using UnityEngine;

namespace LHS_CATest {
    /// <summary>
    /// 떠다니는 성역 석판. 두 지점(a → b) 사이를 부드럽게 왕복한다.
    ///
    /// ■ 움직임: Kinematic Rigidbody2D.MovePosition — 물리 단계에서 이동해 위에 선 플레이어와 충돌 계산이 맞는다.
    ///   끝점에서 pause 초 멈춤, 가속/감속은 smoothstep 곡선.
    /// ■ 태워 나르기: 플레이어 콜라이더에 마찰이 없어서(NotFriction) 석판이 옆으로 움직이면 발밑이 빠져나간다.
    ///   그래서 석판 윗면에 서 있는 플레이어를 찾아 석판이 움직인 만큼(delta) 플레이어 위치도 같이 옮겨 준다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class CATestMovingPlatform : MonoBehaviour {
        [SerializeField] private Vector2 offset = new(8f, 0f);   // a = 시작 위치, b = a + offset
        [SerializeField] private float travelTime = 3f;
        [SerializeField] private float pause = 0.8f;
        [SerializeField] private float phase;
        [SerializeField] private Vector2 riderCheckSize = new(3.4f, 0.6f);
        [SerializeField] private float topY = 0.1f;               // 석판 윗면(로컬 y)

        private Rigidbody2D _rb;
        private Vector2 _a;
        private readonly Collider2D[] _hits = new Collider2D[4];

        private void Awake() {
            _rb = GetComponent<Rigidbody2D>();
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _a = _rb.position;
        }

        private Vector2 PosAt(float time) {
            var cycle = (travelTime + pause) * 2f;
            var t = Mathf.Repeat(time + phase, cycle);
            float k;
            if (t < travelTime) k = t / travelTime;
            else if (t < travelTime + pause) k = 1f;
            else if (t < travelTime * 2f + pause) k = 1f - (t - travelTime - pause) / travelTime;
            else k = 0f;
            k = k * k * (3f - 2f * k);
            return _a + offset * k;
        }

        private void FixedUpdate() {
            var next = PosAt(Time.time);
            var delta = next - _rb.position;
            // 윗면에 서 있는 플레이어(레이어 7)를 같이 옮김
            var center = _rb.position + new Vector2(0f, topY + riderCheckSize.y * 0.5f);
            var n = Physics2D.OverlapBoxNonAlloc(center, riderCheckSize, 0f, _hits, 1 << 7);
            for (var i = 0; i < n; i++) {
                var rb = _hits[i].attachedRigidbody;
                if (rb == null || rb == _rb || rb.bodyType != RigidbodyType2D.Dynamic) continue;
                if (rb.linearVelocityY > 0.5f) continue; // 점프 중이면 태우지 않음
                rb.position += delta;
                break;
            }
            _rb.MovePosition(next);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos() {
            var a = Application.isPlaying ? _a : (Vector2)transform.position;
            Gizmos.color = new Color(0.8f, 0.8f, 1f, 0.6f);
            Gizmos.DrawLine(a, a + offset);
            Gizmos.DrawWireCube(a + offset, new Vector3(riderCheckSize.x, 0.3f, 0f));
        }
#endif
    }
}
