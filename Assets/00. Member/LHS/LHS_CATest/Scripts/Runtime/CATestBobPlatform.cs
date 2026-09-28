using UnityEngine;

namespace LHS_CATest {
    /// <summary>
    /// 물 위에 떠 있는 발판(통나무 / 연잎).
    ///  - bob: 위아래로 천천히 출렁임 (사인파)
    ///  - sinkWhenStood: 플레이어가 올라서 있으면 점점 가라앉고(최대 maxSink), 내려오면 다시 떠오름
    ///    → 연잎 위에 오래 서 있으면 물에 빠진다 = "멈추지 말고 건너라"를 가르치는 장치
    ///
    /// 움직임은 Kinematic Rigidbody2D.MovePosition 으로 준다. 그래야 위에 선 플레이어가 물리적으로 함께 밀려 올라간다
    /// (Transform 을 직접 옮기면 충돌 계산 없이 순간이동하므로 플레이어가 발판에 파묻힐 수 있다).
    /// 발판 콜라이더는 Ground 레이어 → 플레이어의 접지 판정(레이캐스트)에 걸린다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class CATestBobPlatform : MonoBehaviour {
        [SerializeField] private float bobAmplitude = 0.25f;
        [SerializeField] private float bobSpeed = 1.2f;
        [SerializeField] private float phase;
        [SerializeField] private bool sinkWhenStood;
        [SerializeField] private float sinkSpeed = 0.6f;
        [SerializeField] private float riseSpeed = 1.2f;
        [SerializeField] private float maxSink = 1.6f;
        [SerializeField] private Vector2 standCheckSize = new(2f, 0.5f);

        private Rigidbody2D _rb;
        private Vector2 _home;
        private float _sink;
        private BoxCollider2D _box;

        private void Awake() {
            _rb = GetComponent<Rigidbody2D>();
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _home = _rb.position;
            _box = GetComponent<BoxCollider2D>();
        }

        private void FixedUpdate() {
            if (sinkWhenStood) {
                var stood = false;
                if (_box != null) {
                    var top = (Vector2)_box.bounds.center + Vector2.up * (_box.bounds.extents.y + standCheckSize.y * 0.5f);
                    stood = Physics2D.OverlapBox(top, standCheckSize, 0f, 1 << 7) != null; // Player 레이어(7)
                }
                _sink = stood ? Mathf.MoveTowards(_sink, maxSink, sinkSpeed * Time.fixedDeltaTime)
                              : Mathf.MoveTowards(_sink, 0f, riseSpeed * Time.fixedDeltaTime);
            }
            var y = Mathf.Sin(Time.time * bobSpeed + phase) * bobAmplitude - _sink;
            _rb.MovePosition(_home + new Vector2(0f, y));
        }
    }
}
