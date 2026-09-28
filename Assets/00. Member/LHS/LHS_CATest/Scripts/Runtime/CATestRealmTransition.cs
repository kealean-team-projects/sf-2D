using _02._Script._01_Players;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LHS_CATest {
    /// <summary>
    /// 심해 "빛이 드는 틈" → 마지막 챕터(황혼의 성역)로 넘어가는 연출.
    ///
    /// 흐름
    ///  1) 틈 아래 빛 웅덩이(트리거)에 들어가면 조작 잠금 + 검은 띠
    ///  2) 화면이 "하얗게" 차오른다(검정 페이드 대신 흰색 → 빛에 삼켜지는 느낌)
    ///  3) 흰 화면 동안 도착 지점의 맵을 불러오고 플레이어를 옮김(위에서 천천히 내려앉게 낙하 속도 제한)
    ///  4) 대사 → 흰 화면이 걷히며 황혼 하늘 → 착지 → 지역 제목 + 세이브
    /// 페이드 색은 끝나면 다시 검정으로 돌려 둔다(사망/다른 연출용).
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class CATestRealmTransition : MonoBehaviour {
        [SerializeField] private Vector2 arrival = new(1412f, 18f);
        [SerializeField] private float maxFallSpeed = 5f;
        [SerializeField] private string lineKey = "realm_enter";
        [SerializeField] private string arriveLineKey = "realm_arrive";
        [SerializeField] private string areaTitle = "백색 협곡";
        [SerializeField] private string areaSubtitle = "마지막 챕터 · 황혼의 성역";

        private bool _running;

        private void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;

        private void OnTriggerEnter2D(Collider2D other) {
            if (_running) return;
            var p = other.GetComponentInParent<Player>();
            if (p == null || p.IsDead || !p.isActiveAndEnabled) return;
            Run(p).Forget();
        }

        private async UniTaskVoid Run(Player p) {
            _running = true;
            CATestCutscene.Begin();
            await CATestHUD.SayLines(CATestLines.Get(lineKey), 0.2f);
            CATestHUD.SetFadeColor(new Color(1f, 0.97f, 0.94f));
            CATestCutscene.Shake(0.15f, 1.6f);
            await CATestHUD.FadeTo(1f, 2.2f);
            if (p == null) return;
            var streamer = CATestSceneStreamer.Instance;
            if (streamer != null) await streamer.EnsureLoadedAt(arrival);
            if (p == null) return;
            p.RestoreState(arrival, p.CurrentStamina);
            await UniTask.Delay(System.TimeSpan.FromSeconds(0.8f), true);
            CATestHUD.FadeTo(0f, 3.0f).Forget();
            var rb = p.GetComponent<Rigidbody2D>();
            for (var t = 0f; t < 6f && p != null && !p.IsGrounded; t += Time.deltaTime) {
                if (rb != null && rb.linearVelocityY < -maxFallSpeed) rb.linearVelocityY = -maxFallSpeed;
                await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
            }
            await CATestCutscene.Wait(1.2f);
            CATestHUD.SetFadeColor(Color.black);
            await CATestHUD.SayLines(CATestLines.Get(arriveLineKey), 0.3f);
            CATestCutscene.End();
            CATestHUD.ShowTitle(areaTitle, areaSubtitle);
            await CATestCutscene.Wait(0.5f);
            if (p != null && !p.IsDead) CATestSave.SaveHere(areaTitle);
            if (this != null) _running = false;
        }
    }
}
