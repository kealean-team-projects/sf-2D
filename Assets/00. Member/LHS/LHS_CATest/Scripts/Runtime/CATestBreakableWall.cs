using _02._Script._01_Players;
using _02._Script._04_Interaction;
using UnityEngine;

namespace LHS_CATest {
    /// <summary>
    /// 금 간 돌벽. 가까이 가면(Interactor) 테두리가 강조되고, 상호작용 키를 누르면 무너져 숨은 길이 열린다.
    /// 기존 InteractBase(테두리 하이라이트)를 상속하므로 Interactable 레이어(8)에 콜라이더가 있어야 한다.
    /// </summary>
    public sealed class CATestBreakableWall : InteractBase {
        [SerializeField] private Collider2D[] blockers;
        [SerializeField] private GameObject[] hideOnBreak;
        [SerializeField] private GameObject[] showOnBreak;
        [SerializeField] private ParticleSystem debris;
        [SerializeField] private int hitsToBreak = 2;

        private int _hits;
        private bool _broken;
        private float _shake;
        private Vector3 _base;

        private void Awake() {
            _base = transform.localPosition;
            foreach (var go in showOnBreak) if (go != null) go.SetActive(false);
        }

        public override void Interact(Player owner) {
            if (_broken) return;
            _hits++;
            _shake = 0.25f;
            if (debris != null) debris.Emit(10);
            if (_hits < hitsToBreak) return;
            Break();
        }

        private void Break() {
            _broken = true;
            SetHighlight(false);
            if (debris != null) debris.Emit(40);
            CATestAudio.PlaySfx("wall_break", transform.position);
            foreach (var c in blockers) if (c != null) c.enabled = false;
            foreach (var go in hideOnBreak) if (go != null) go.SetActive(false);
            foreach (var go in showOnBreak) if (go != null) go.SetActive(true);
            foreach (var c in GetComponents<Collider2D>()) c.enabled = false;
            enabled = true;
        }

        private void Update() {
            if (_shake <= 0f) return;
            _shake -= Time.deltaTime;
            transform.localPosition = _base + (Vector3)(Random.insideUnitCircle * 0.08f);
            if (_shake <= 0f) transform.localPosition = _base;
        }
    }
}
