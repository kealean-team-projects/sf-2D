using _02._Script._01_Players;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LHS_CATest {
    /// <summary>
    /// 산 정상의 구덩이 → 심해 착수 웅덩이 전환.
    ///  1) 구덩이 속 trigger 에 들어오면 떨어지는 동안 화면이 서서히 검게 변한다(fadeOut).
    ///  2) 완전히 검어지면 스트리머로 심해 씬이 로드됐는지 확인하고, 플레이어를 웅덩이 위 도착 지점으로 옮긴다.
    ///  3) 잠시 어둠을 유지한 뒤(holdBlack) 화면이 밝아지며(fadeIn) 웅덩이로 떨어지는 장면이 보인다.
    /// 숲과 심해가 월드상 멀리 떨어져 있어도(스트리밍으로 한쪽만 로드돼 있어도) 이어진 것처럼 느끼게 하는 장치.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class CATestPitTransition : MonoBehaviour {
        [SerializeField] private Vector2 arrivalPosition = new(269f, -28f);
        [SerializeField] private float arrivalFallSpeed = 14f;
        [SerializeField] private float fadeOut = 1.1f;
        [SerializeField] private float holdBlack = 0.7f;
        [SerializeField] private float fadeIn = 1.6f;

        private bool _running;

        private void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;

        private void OnTriggerEnter2D(Collider2D other) {
            if (_running) return;
            var player = other.GetComponentInParent<Player>();
            if (player == null || player.IsDead) return;
            Run(player).Forget();
        }

        private async UniTaskVoid Run(Player player) {
            _running = true;
            CATestAudio.PlaySfx("dive");
            try {
                await CATestHUD.FadeTo(1f, fadeOut);
                var streamer = CATestSceneStreamer.Instance;
                if (streamer != null) await streamer.EnsureLoadedAt(arrivalPosition);
                if (player == null) return;
                player.RestoreState(arrivalPosition, player.CurrentStamina);
                var rb = player.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = new Vector2(0f, -arrivalFallSpeed);
                await UniTask.Delay(System.TimeSpan.FromSeconds(holdBlack), true);
                CATestHUD.FadeTo(0f, fadeIn).Forget();
            }
            finally {
                // 이 오브젝트는 숲 씬과 함께 언로드될 수 있으므로 null 체크
                if (this != null) _running = false;
            }
        }
    }
}
