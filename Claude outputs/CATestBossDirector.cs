using _02._Script._05_Managers;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LHS_CATest {
    /// <summary>
    /// 보스 구역 연출 감독. 보스 행동(CATestEyeHunter)의 단계를 컷신과 함께 넘긴다.
    ///
    /// 흐름
    ///  1) 입장 컷신 (introArea 에 처음 발을 디디면)
    ///     검은 띠 + 조작 잠금 → 카메라가 멀리 떠 있는 눈으로 이동 → 눈이 천천히 뜨며 빛을 훑음
    ///     → 카메라가 플레이어로 돌아옴 → 대사 → 조작 복구, 수색 시작
    ///  2) 수색 구간: 보스가 앵커를 따라 플레이어를 찾아다님 (CATestEyeHunter)
    ///  3) 분노 컷신 (rageArea 에 도달하면)
    ///     눈이 붉게 변하며 가까이 다가옴 + 화면 흔들림 → 대사 → 추격전 시작
    ///  4) 추격전: 웅크려야만 들어갈 수 있는 굴(tunnel)까지 도망
    ///  5) 탈출 컷신 (굴 안쪽 escapeArea 에 들어가면)
    ///     눈이 굴 입구를 몇 번 들이받다 포기하고 어둠 속으로 사라짐 → 보스전 끝
    ///
    /// 실패(사망) 시
    ///  보스 구역 입구 세이브 포인트에서 다시 시작 → OnRespawnReset 에서 보스를 첫 앵커 + 수색 상태로 되돌린다.
    ///  입장 컷신은 다시 보여주지 않고, 분노 컷신은 두 번째부터 짧게(카메라 이동 생략).
    /// </summary>
    public sealed class CATestBossDirector : MonoBehaviour {
        [SerializeField] private CATestEyeHunter hunter;
        [SerializeField] private Rect introArea = new(520, -92, 8, 12);
        [SerializeField] private Rect rageArea = new(880, -100, 6, 40);
        [SerializeField] private Rect escapeArea = new(1024, -100, 8, 10);
        [SerializeField] private Vector2 tunnelMouth = new(1010, -94);
        [SerializeField] private float tunnelSafeX = 1011f;

        private static bool _introDone;
        private static bool _escaped;
        private int _rageCount;
        private bool _raging;
        private bool _busy;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() {
            _introDone = false;
            _escaped = false;
        }

        private void OnEnable() => GameManager.OnRespawnReset += OnRespawn;
        private void OnDisable() => GameManager.OnRespawnReset -= OnRespawn;

        private void Start() {
            if (hunter == null) return;
            hunter.SetSafeX(tunnelSafeX);
            if (_escaped) hunter.gameObject.SetActive(false);
            else hunter.ResetToStart(_introDone);
        }

        private UniTask OnRespawn() {
            if (hunter != null && !_escaped) {
                _raging = false;
                _busy = false;
                hunter.ResetToStart(_introDone);
            }
            return UniTask.CompletedTask;
        }

        private void Update() {
            if (_busy || _escaped || hunter == null) return;
            var p = CATestHUD.CurrentPlayer;
            if (p == null || !p.isActiveAndEnabled || p.IsDead) return;
            Vector2 pos = p.transform.position;
            if (!_introDone && introArea.Contains(pos) && p.IsGrounded) Intro().Forget();
            else if (!_introDone && pos.x > introArea.xMax + 6f && pos.y < introArea.yMax + 20f) {
                // 테스트용 순간이동으로 입구를 건너뛴 경우: 컷신 없이 바로 수색 시작
                _introDone = true;
                hunter.ResetToStart(true);
            }
            else if (_introDone && !_raging && rageArea.Contains(pos)) Rage().Forget();
            else if (_raging && escapeArea.Contains(pos)) Escape().Forget();
        }

        private async UniTaskVoid Intro() {
            _busy = true;
            _introDone = true;
            CATestCutscene.Begin();
            await CATestCutscene.Wait(0.6f);
            CATestCutscene.Focus(hunter.EyeWorldPosition + new Vector3(0f, -6f, 0f), 1.8f, 105f);
            await CATestCutscene.Wait(1.6f);
            hunter.Wake();
            CATestCutscene.Shake(0.2f, 0.8f);
            await CATestCutscene.Wait(3.0f);
            CATestCutscene.Release(1.5f);
            await CATestCutscene.Wait(1.5f);
            await CATestHUD.SayLines(CATestLines.Get("boss_intro"), 0.15f);
            CATestCutscene.End();
            if (CATestHUD.Instance != null) CATestHUD.ShowTitle("심연의 눈", "빛에 닿지 말 것");
            _busy = false;
        }

        private async UniTaskVoid Rage() {
            _busy = true;
            _raging = true;
            _rageCount++;
            CATestCutscene.Begin();
            hunter.EnterRage();
            CATestCutscene.Shake(0.6f, 1.2f);
            if (_rageCount == 1) {
                CATestCutscene.Focus(hunter.EyeWorldPosition + new Vector3(0f, -4f, 0f), 1.2f, 95f);
                await CATestCutscene.Wait(2.2f);
                CATestCutscene.Release(0.9f);
                await CATestCutscene.Wait(0.8f);
                await CATestHUD.Say(CATestLines.Get("boss_rage")[0], 0.6f);
            }
            else await CATestCutscene.Wait(1.0f);
            CATestCutscene.End();
            hunter.StartChase(15f);
            var rest = CATestLines.Get("boss_rage");
            if (rest.Length > 1) CATestHUD.Say(rest[1]).Forget();
            _busy = false;
        }

        private async UniTaskVoid Escape() {
            _busy = true;
            _escaped = true;
            CATestCutscene.Begin();
            hunter.BlockAt(tunnelMouth);
            CATestCutscene.Focus(new Vector3(tunnelMouth.x + 2f, tunnelMouth.y + 3f, 0f), 1.2f, 80f);
            await CATestCutscene.Wait(1.0f);
            for (var i = 0; i < 3; i++) {
                await hunter.Lunge();
                await CATestCutscene.Wait(0.35f + i * 0.2f);
            }
            CATestCutscene.Release(1.2f);
            await CATestCutscene.Wait(1.0f);
            await CATestHUD.SayLines(CATestLines.Get("boss_escape"), 0.2f);
            hunter.Leave();
            await CATestCutscene.Wait(2.2f);
            await CATestHUD.SayLines(CATestLines.Get("boss_gone"), 0.2f);
            CATestCutscene.End();
            _busy = false;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos() {
            Gizmos.color = new Color(0.4f, 1f, 1f, 0.6f); Gizmos.DrawWireCube(introArea.center, introArea.size);
            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.6f); Gizmos.DrawWireCube(rageArea.center, rageArea.size);
            Gizmos.color = new Color(0.4f, 1f, 0.4f, 0.6f); Gizmos.DrawWireCube(escapeArea.center, escapeArea.size);
            Gizmos.DrawWireSphere(tunnelMouth, 0.6f);
        }
#endif
    }
}
