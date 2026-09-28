using System.Collections.Generic;
using _02._Script._01_Players;
using UnityEngine;

namespace LHS_CATest {
    /// <summary>
    /// 숨을 수 있는 영역(해초 덤불 등). 보스 눈의 시야 판정에서 사용한다.
    /// requireCrouch 가 켜져 있으면 웅크리고 있어야 숨은 것으로 친다.
    /// 바위 아치처럼 "지붕"이 있는 엄폐물은 이 컴포넌트 대신 Ground 레이어 콜라이더가 레이캐스트를 막아서 처리된다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class CATestCover : MonoBehaviour {
        [SerializeField] private bool requireCrouch = true;
        [SerializeField] private SpriteRenderer[] rustleTargets;

        private static readonly List<CATestCover> Active = new();
        private readonly HashSet<Collider2D> _inside = new();
        private Player _player;
        private float _rustle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Active.Clear();

        private void Awake() => GetComponent<Collider2D>().isTrigger = true;
        private void OnEnable() => Active.Add(this);

        private void OnDisable() {
            Active.Remove(this);
            _inside.Clear();
            _player = null;
        }

        private void OnTriggerEnter2D(Collider2D other) {
            var p = other.GetComponentInParent<Player>();
            if (p == null) return;
            _inside.Add(other);
            _player = p;
            _rustle = 1f;
        }

        private void OnTriggerExit2D(Collider2D other) {
            if (_inside.Remove(other) && _inside.Count == 0) _player = null;
        }

        private void Update() {
            if (rustleTargets == null) return;
            _rustle = Mathf.MoveTowards(_rustle, 0f, Time.deltaTime * 1.5f);
            var hidden = _player != null && (!requireCrouch || _player.IsCrouching);
            for (var i = 0; i < rustleTargets.Length; i++) {
                var sr = rustleTargets[i];
                if (sr == null) continue;
                sr.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 14f + i * 1.7f) * 4f * _rustle);
                var c = sr.color;
                c.a = Mathf.MoveTowards(c.a, hidden ? 0.75f : 1f, Time.deltaTime * 2f);
                sr.color = c;
            }
        }

        public static bool IsHidden(Player p) {
            foreach (var c in Active)
                if (c._player == p && (!c.requireCrouch || p.IsCrouching))
                    return true;
            return false;
        }
    }
}
