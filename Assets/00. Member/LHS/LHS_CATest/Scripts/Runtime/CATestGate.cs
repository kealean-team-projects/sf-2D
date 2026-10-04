using System.Collections.Generic;
using UnityEngine;

namespace LHS_CATest {
    /// <summary>
    /// 신호(ICATestSignal: 레버/압력판)에 따라 열리고 닫히는 장치. 문(위로 올라가는 말뚝)과 도개교(회전하는 통나무) 모두 이걸로 만든다.
    ///  - 닫힘 자세 = 배치된 위치/회전, 열림 자세 = openOffset / openAngle 만큼 이동·회전.
    ///  - Kinematic Rigidbody2D 를 MovePosition/MoveRotation 으로 움직여서 위에 선 플레이어·물체와 물리적으로 자연스럽게 상호작용한다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class CATestGate : MonoBehaviour {
        [SerializeField] private List<MonoBehaviour> sources = new();
        [Tooltip("true: 모든 신호가 켜져야 열림 / false: 하나라도 켜지면 열림")]
        [SerializeField] private bool requireAll;
        [SerializeField] private bool invert;
        [SerializeField] private Vector2 openOffset = new(0f, 4.5f);
        [SerializeField] private float openAngle;
        [SerializeField] private float openSpeed = 6f;
        [SerializeField] private float closeSpeed = 3f;
        [SerializeField] private ParticleSystem moveDust;

        private Rigidbody2D _rb;
        private Vector2 _closedPos;
        private float _closedRot;
        private float _t;

        public bool IsOpen => _t > 0.95f;

        private void Awake() {
            _rb = GetComponent<Rigidbody2D>();
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _closedPos = _rb.position;
            _closedRot = _rb.rotation;
        }

        private bool SignalOn() {
            var any = false;
            var all = true;
            var count = 0;
            foreach (var m in sources) {
                if (m is not ICATestSignal s) continue;
                count++;
                if (s.IsOn) any = true;
                else all = false;
            }
            var on = count > 0 && (requireAll ? all : any);
            return invert ? !on : on;
        }

        private void FixedUpdate() {
            var target = SignalOn() ? 1f : 0f;
            if (target > 0.5f && _t <= 0.001f) CATestAudio.PlaySfx("gate_open", transform.position); // 닫힌 상태에서 열리기 시작하는 순간
            var speed = target > _t ? openSpeed : closeSpeed;
            var prev = _t;
            _t = Mathf.MoveTowards(_t, target, Time.fixedDeltaTime * speed / Mathf.Max(0.1f, openOffset.magnitude + Mathf.Abs(openAngle) * 0.05f));
            var k = Mathf.SmoothStep(0f, 1f, _t);
            _rb.MovePosition(_closedPos + openOffset * k);
            _rb.MoveRotation(_closedRot + openAngle * k);
            if (moveDust != null) {
                var moving = !Mathf.Approximately(prev, _t);
                if (moving && !moveDust.isEmitting) moveDust.Play();
                else if (!moving && moveDust.isEmitting) moveDust.Stop();
            }
        }
    }
}
