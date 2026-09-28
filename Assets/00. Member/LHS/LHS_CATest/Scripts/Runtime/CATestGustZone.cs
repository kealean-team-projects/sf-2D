using _02._Script._01_Players;
using UnityEngine;

namespace LHS_CATest {
    /// <summary>
    /// 주기적으로 부는 돌풍 구역. 잠잠함 → 예고(잎이 흩날림) → 돌풍 순서로 반복한다.
    /// 돌풍이 부는 동안 서 있으면 Player.SetPushSpeed 로 밀려나고, 웅크리고 있으면 버틸 수 있다.
    /// (기존 WaterCurrent/Fan 과 같은 SetPushSpeed 경로를 쓰므로 이동 코드와 자연스럽게 합쳐진다)
    /// </summary>
    public sealed class CATestGustZone : MonoBehaviour {
        public Rect area = new(0, 0, 20, 8);
        [SerializeField] private Vector2 push = new(-9f, 0f);
        [SerializeField] private float calmTime = 2.2f;
        [SerializeField] private float warnTime = 0.9f;
        [SerializeField] private float blowTime = 1.8f;
        [SerializeField] private float phaseOffset;
        [SerializeField] private ParticleSystem warnParticles;
        [SerializeField] private ParticleSystem gustParticles;
        [SerializeField] private Transform[] swayTargets;

        private Player _pushed;
        private float _t;
        private Quaternion[] _swayRest;

        public enum Phase { Calm, Warn, Blow }
        public Phase Current { get; private set; }

        private void Awake() {
            _t = phaseOffset;
            if (swayTargets != null) {
                _swayRest = new Quaternion[swayTargets.Length];
                for (var i = 0; i < swayTargets.Length; i++) if (swayTargets[i] != null) _swayRest[i] = swayTargets[i].localRotation;
            }
        }

        private void OnDisable() => Release();

        private void Update() {
            var cycle = calmTime + warnTime + blowTime;
            _t = (_t + Time.deltaTime) % cycle;
            var phase = _t < calmTime ? Phase.Calm : _t < calmTime + warnTime ? Phase.Warn : Phase.Blow;
            if (phase != Current) {
                Current = phase;
                SetEmit(warnParticles, phase == Phase.Warn || phase == Phase.Blow);
                SetEmit(gustParticles, phase == Phase.Blow);
            }

            if (swayTargets != null) {
                var bend = phase == Phase.Blow ? 1f : phase == Phase.Warn ? 0.35f : 0f;
                for (var i = 0; i < swayTargets.Length; i++) {
                    if (swayTargets[i] == null) continue;
                    var ang = Mathf.Sign(push.x) * -12f * bend + Mathf.Sin(Time.time * 7f + i) * 3f * bend;
                    swayTargets[i].localRotation = Quaternion.Slerp(swayTargets[i].localRotation, _swayRest[i] * Quaternion.Euler(0, 0, ang), Time.deltaTime * 5f);
                }
            }

            var player = CATestHUD.CurrentPlayer;
            var inside = player != null && player.isActiveAndEnabled && area.Contains(player.transform.position);
            var shouldPush = inside && phase == Phase.Blow && !player.IsCrouching && !player.IsClimb;
            if (shouldPush) {
                player.SetPushSpeed(push);
                _pushed = player;
            }
            else Release();
        }

        private void Release() {
            if (_pushed == null) return;
            _pushed.SetPushSpeed(Vector2.zero);
            _pushed = null;
        }

        private static void SetEmit(ParticleSystem ps, bool on) {
            if (ps == null) return;
            if (on && !ps.isEmitting) ps.Play(true);
            else if (!on && ps.isEmitting) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos() {
            Gizmos.color = new Color(0.8f, 1f, 1f, 0.5f);
            Gizmos.DrawWireCube(area.center, area.size);
            Gizmos.DrawRay(area.center, (Vector3)push.normalized * 4f);
        }
#endif
    }
}
