using System.Collections.Generic;
using _02._Script._01_Players;
using UnityEngine;

namespace LHS_CATest {
    /// <summary>
    /// 산 정상 구덩이 → 해저 동굴 입구로 떨어지는 긴 수직 낙하 구간 연출.
    ///  - 낙하 속도 상한(maxFallSpeed)을 걸어 떨어지는 과정을 3초 남짓 "감상"하게 만든다.
    ///    (Mover에는 종단 속도가 없어 그대로 두면 80유닛 낙하가 1초대에 끝나 버린다)
    ///  - 구간 안에서는 좌우 입력을 약하게 줄여 벽에 붙어 멈추는 일을 막는다.
    ///  - 카메라 주변에 위로 스쳐 지나가는 줄기(속도선) 파티클을 켜서 속도감을 준다.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class CATestFallSequence : MonoBehaviour {
        [SerializeField] private float maxFallSpeed = 24f;
        [SerializeField] private ParticleSystem speedLines;
        [SerializeField] private ParticleSystem debris;
        [SerializeField] private Vector3 effectOffset = new(0f, -6f, -4f);

        private readonly HashSet<Collider2D> _inside = new();
        private Player _player;
        private Rigidbody2D _body;

        private void Awake() {
            GetComponent<BoxCollider2D>().isTrigger = true;
            if (speedLines != null) speedLines.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (debris != null) debris.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void OnTriggerEnter2D(Collider2D other) {
            var p = other.GetComponentInParent<Player>();
            if (p == null) return;
            var first = _inside.Count == 0;
            _inside.Add(other);
            if (!first) return;
            _player = p;
            _body = p.GetComponent<Rigidbody2D>();
            if (speedLines != null) speedLines.Play(true);
            if (debris != null) debris.Play(true);
        }

        private void OnTriggerExit2D(Collider2D other) {
            if (!_inside.Remove(other) || _inside.Count > 0) return;
            _player = null;
            _body = null;
            if (speedLines != null) speedLines.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (debris != null) debris.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        private void FixedUpdate() {
            if (_body == null) return;
            if (_body.linearVelocityY < -maxFallSpeed) _body.linearVelocityY = -maxFallSpeed;
        }

        private void LateUpdate() {
            if (_player == null) return;
            var p = _player.transform.position + effectOffset;
            if (speedLines != null) speedLines.transform.position = p;
            if (debris != null) debris.transform.position = p + Vector3.back * 3f;
        }
    }
}
