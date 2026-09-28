using _02._Script._01_Players;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace LHS_CATest {
    /// <summary>
    /// 룬 발판 하나. 플레이어가 위에 "올라서면" 부모 CATestRuneSequence 에 알린다(같은 발판은 한 번 내려왔다 다시 올라야 다시 알림).
    /// 발판의 룬 문자(glyph)는 켜짐/꺼짐/틀림 상태에 따라 색과 밝기가 바뀐다.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class CATestRunePlate : MonoBehaviour {
        [SerializeField] private int runeId;
        [SerializeField] private SpriteRenderer glyph;
        [SerializeField] private Light2D glow;
        [SerializeField] private CATestRuneSequence sequence;

        public int RuneId => runeId;
        private bool _inside;
        private float _lit;        // 0 = 꺼짐, 1 = 켜짐
        private float _target;
        private Color _litColor = new(1f, 0.85f, 0.65f, 1f);
        private readonly Color _offColor = new(0.75f, 0.72f, 0.9f, 0.25f);
        private float _flashWrong;

        private void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;

        private void OnTriggerEnter2D(Collider2D other) {
            if (_inside) return;
            var p = other.GetComponentInParent<Player>();
            if (p == null || p.IsDead) return;
            _inside = true;
            if (sequence != null) sequence.Stepped(this);
        }

        private void OnTriggerExit2D(Collider2D other) {
            if (other.GetComponentInParent<Player>() != null) _inside = false;
        }

        public void SetLit(bool on) => _target = on ? 1f : 0f;
        public void SetSolved() { _litColor = new Color(0.75f, 0.95f, 1f, 1f); _target = 1f; }
        public void FlashWrong() => _flashWrong = 1f;

        private void Update() {
            _lit = Mathf.MoveTowards(_lit, _target, Time.deltaTime * 4f);
            _flashWrong = Mathf.MoveTowards(_flashWrong, 0f, Time.deltaTime * 1.5f);
            var pulse = 0.9f + Mathf.Sin(Time.time * 3f + runeId) * 0.1f;
            var c = Color.Lerp(_offColor, _litColor, _lit);
            c = Color.Lerp(c, new Color(1f, 0.25f, 0.25f, 1f), _flashWrong);
            if (glyph != null) glyph.color = new Color(c.r, c.g, c.b, c.a * pulse);
            if (glow != null) {
                glow.color = c;
                glow.intensity = Mathf.Max(_lit, _flashWrong) * 1.3f * pulse;
            }
        }
    }
}
