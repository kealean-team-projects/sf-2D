using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LHS_CATest {
    /// <summary>
    /// 룬 순서 퍼즐 (백색 성역). 문 위 벽화에 룬 3개가 "순서대로" 새겨져 있고, 바닥의 룬 발판을 그 순서대로 밟으면 문이 열린다.
    ///
    /// ■ 규칙
    ///   - 맞는 발판을 밟으면 그 발판이 켜지고 벽화의 해당 칸도 켜진다.
    ///   - 틀린 발판을 밟으면 모든 발판이 붉게 번쩍이며 처음부터 다시(켜진 것 모두 꺼짐) + 짧은 화면 흔들림.
    ///   - 순서를 다 맞추면 IsOn = true → CATestGate(성역 문)가 올라간다. 이후 계속 열린 상태.
    /// ■ 힌트 연출: 벽화의 룬이 hintInterval 초마다 순서대로 하나씩 반짝인다(처음 보는 플레이어도 순서를 읽을 수 있게).
    /// </summary>
    public sealed class CATestRuneSequence : MonoBehaviour, ICATestSignal {
        [SerializeField] private int[] order = { 1, 2, 0 };
        [SerializeField] private CATestRunePlate[] plates;
        [SerializeField] private SpriteRenderer[] muralGlyphs; // 벽화: order 순서대로 놓인 룬 그림
        [SerializeField] private float hintInterval = 0.7f;
        [SerializeField] private string solvedLineKey = "sanctum_rune_solved";
        [SerializeField] private string wrongLineKey = "sanctum_rune_wrong";

        public bool IsOn { get; private set; }
        private int _progress;
        private float _hintTimer;
        private bool _wrongSpoken;

        public void Stepped(CATestRunePlate plate) {
            if (IsOn || plate == null) return;
            if (plate.RuneId == order[_progress]) {
                plate.SetLit(true);
                _progress++;
                if (_progress >= order.Length) Solve();
            }
            else {
                _progress = 0;
                foreach (var p in plates) { if (p == null) continue; p.SetLit(false); p.FlashWrong(); }
                CATestCutscene.Shake(0.2f, 0.25f);
                if (!_wrongSpoken) { _wrongSpoken = true; CATestHUD.SayLines(CATestLines.Get(wrongLineKey)).Forget(); }
            }
        }

        private void Solve() {
            IsOn = true;
            foreach (var p in plates) if (p != null) p.SetSolved();
            CATestCutscene.Shake(0.35f, 0.8f);
            CATestHUD.SayLines(CATestLines.Get(solvedLineKey)).Forget();
        }

        private void Update() {
            if (muralGlyphs == null || muralGlyphs.Length == 0) return;
            _hintTimer += Time.deltaTime;
            var cycle = hintInterval * (muralGlyphs.Length + 1.5f);
            var t = _hintTimer % cycle;
            var active = Mathf.FloorToInt(t / hintInterval);
            for (var i = 0; i < muralGlyphs.Length; i++) {
                var g = muralGlyphs[i];
                if (g == null) continue;
                var solvedPart = i < _progress || IsOn;
                var hint = i == active ? 1f : 0f;
                var a = solvedPart ? 1f : Mathf.Lerp(0.25f, 0.95f, hint);
                var col = solvedPart ? new Color(1f, 0.85f, 0.65f) : new Color(0.85f, 0.82f, 1f);
                g.color = Color.Lerp(g.color, new Color(col.r, col.g, col.b, a), Time.deltaTime * 8f);
            }
        }
    }
}
