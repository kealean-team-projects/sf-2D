using System.Collections.Generic;
using _02._Script._01_Players;
using UnityEngine;

namespace LHS_CATest {
    /// <summary>
    /// 수중 영역. 플레이어가 안에 있으면 "약한 부력"을 준다.
    ///
    /// 동작 원리
    ///  - Mover는 매 물리 프레임 gravityScale을 원래 값으로 되돌리기 때문에 gravityScale을 바꾸는 방식은 쓸 수 없다.
    ///  - 대신 떨어지는 중(속도 y &lt; 0)일 때만 위쪽으로 힘을 더하고, 낙하 속도의 최대값을 제한한다.
    ///  - 올라갈 때는 힘을 주지 않으므로 점프 높이는 지상과 같고, 내려올 때만 느려진다.
    ///    → 체공 시간이 길어져 지상보다 더 먼 거리를 건널 수 있다(수중에서만 가능한 이동).
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class CATestWaterVolume : MonoBehaviour {
        [SerializeField] private float buoyancy = 32f;
        [SerializeField] private float maxFallSpeed = 7f;
        [SerializeField] private ParticleSystem bubblesFollow;
        [SerializeField] private ParticleSystem splash;
        [SerializeField] private float splashSpeed = 8f;

        private readonly HashSet<Collider2D> _inside = new();
        private Player _player;
        private Rigidbody2D _body;

        public static int PlayerInsideCount { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => PlayerInsideCount = 0;

        private void Awake() {
            GetComponent<BoxCollider2D>().isTrigger = true;
            if (bubblesFollow != null) bubblesFollow.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void OnTriggerEnter2D(Collider2D other) {
            var player = other.GetComponentInParent<Player>();
            if (player == null) return;
            var first = _inside.Count == 0;
            _inside.Add(other);
            if (!first) return;
            _player = player;
            _body = player.GetComponent<Rigidbody2D>();
            PlayerInsideCount++;
            if (bubblesFollow != null) bubblesFollow.Play(true);
            if (splash != null && _body != null && _body.linearVelocityY < -splashSpeed) {
                splash.transform.position = new Vector3(player.transform.position.x, splash.transform.position.y, splash.transform.position.z);
                splash.Play(true);
            }
        }

        private void OnTriggerExit2D(Collider2D other) {
            if (!_inside.Remove(other) || _inside.Count > 0) return;
            Release();
        }

        private void OnDisable() {
            if (_player != null) Release();
            _inside.Clear();
        }

        private void Release() {
            _player = null;
            _body = null;
            PlayerInsideCount = Mathf.Max(0, PlayerInsideCount - 1);
            if (bubblesFollow != null) bubblesFollow.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        private void FixedUpdate() {
            if (_body == null || _player == null || _player.IsDead || _player.IsClimb) return;
            if (_body.linearVelocityY >= 0f) return;
            _body.AddForce(Vector2.up * (buoyancy * _body.mass));
            if (_body.linearVelocityY < -maxFallSpeed) _body.linearVelocityY = -maxFallSpeed;
        }

        private void LateUpdate() {
            if (_player != null && bubblesFollow != null)
                bubblesFollow.transform.position = _player.transform.position + new Vector3(0.2f, 0.7f, -0.2f);
        }
    }
}
