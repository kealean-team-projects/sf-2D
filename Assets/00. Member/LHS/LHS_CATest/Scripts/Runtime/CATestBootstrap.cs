using System;
using _02._Script._01_Players;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LHS_CATest {
    /// <summary>
    /// CATest CoreScene 시작 순서를 잡아주는 스크립트.
    ///
    /// 왜 필요한가
    ///  - Player는 Awake(AgentInitializer)에서 UIManager.Instance 에 자신을 등록한다.
    ///    같은 씬 안에서는 Awake 순서가 보장되지 않으므로, Player 오브젝트를 비활성으로 저장해 두고
    ///    모든 매니저의 Awake가 끝난 뒤(Start) 활성화한다.
    ///  - 발밑 지형이 들어있는 맵 씬이 로드되기 전에 플레이어가 켜지면 바로 떨어진다.
    ///    그래서 스트리머로 시작 지점의 씬을 먼저 로드한 뒤 플레이어를 켠다.
    /// </summary>
    [DefaultExecutionOrder(-150)]
    public sealed class CATestBootstrap : MonoBehaviour {
        [Serializable]
        public class StartPoint {
            public string label;
            public Vector2 position;
        }

        [SerializeField] private GameObject playerRoot;
        [SerializeField] private CATestSceneStreamer streamer;
        [Tooltip("테스트용: 어느 지점에서 시작할지 고르세요.")]
        [SerializeField] private int startIndex;
        [SerializeField] private StartPoint[] startPoints = Array.Empty<StartPoint>();
        [Tooltip("0번 지점(화창한 숲 굴 위)에서 시작할 때 오프닝 컷신을 재생")]
        [SerializeField] private bool playIntro = true;
        [Tooltip("새로 시작할 때 오프닝 전에 프롤로그(현실의 방)부터 시작")]
        [SerializeField] private bool playPrologue = true;
        private static Vector2 prologueStart => CATestWorld.PrologueStart; // 방 위치는 CATestWorld 한 곳에서 관리

        public static Vector2? StartPosition { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => StartPosition = null;

        private void Awake() {
            if (playerRoot != null && playerRoot.activeSelf) playerRoot.SetActive(false);
        }

        private async void Start() {
            if (playerRoot == null) {
                Debug.LogError("[CATestBootstrap] Player가 연결되지 않았습니다.", this);
                return;
            }

            // 타이틀에서 넘어온 경우: 새로 시작 = 0번(오프닝) / 이어하기 = 저장 파일 위치
            var mode = CATestSceneFlow.ConsumeMode();
            var index = mode == CATestStartMode.NewGame ? 0 : startIndex;
            var cont = mode == CATestStartMode.Continue ? CATestSave.LoadContinue() : null;
            var pos = startPoints.Length > 0
                ? startPoints[Mathf.Clamp(index, 0, startPoints.Length - 1)].position
                : (Vector2)playerRoot.transform.position;
            if (cont != null) pos = new Vector2(cont.positionX, cont.positionY);
            var prologue = cont == null && playIntro && playPrologue && index == 0;
            if (prologue) pos = prologueStart;
            StartPosition = pos;
            playerRoot.transform.position = new Vector3(pos.x, pos.y, playerRoot.transform.position.z);
            if (cont != null) {
                CATestHUD.FadeTo(1f, 0f).Forget(); // 맵이 로드되고 자리를 잡을 때까지 검은 화면
            }
            else if (playIntro && index == 0) {
                // 첫 프레임부터 검은 화면 + 영화 모드 띠 (굴 꼭대기가 잠깐 보이지 않게)
                if (prologue) CATestPrologue.Pending = true; // 프롤로그(방) → 끝나면 프롤로그가 오프닝을 이어서 켬
                else CATestIntroCutscene.Pending = true;
                CATestHUD.FadeTo(1f, 0f).Forget();
                CATestHUD.SetLetterbox(true, 0.01f);
            }

            if (streamer != null) {
                streamer.SetTarget(playerRoot.transform);
                await streamer.EnsureLoadedAt(pos);
            }

            // 한 프레임 쉬어서 새로 로드된 씬의 Awake/OnEnable(존 등록 등)이 끝나게 한다.
            await UniTask.Yield();
            if (this == null) return;
            playerRoot.SetActive(true);

            if (cont != null) {
                // 원본 저장 시스템의 복원 경로 그대로 사용: SaveManager.RestoreProgress() → Save.sf2d 읽기
                // → PlayerProgress.RestoreProgress() → Player.RestoreState(위치, 기력). 부활 위치도 이 지점이 된다.
                await UniTask.DelayFrame(2);
                if (this == null) return;
                if (_02._Script._05_Managers.SaveManager.Instance != null) _02._Script._05_Managers.SaveManager.Instance.RestoreProgress();
                await CATestHUD.FadeTo(0f, 1.2f);
                var place = CATestSave.LastPlaceName;
                if (!string.IsNullOrEmpty(place)) CATestHUD.ShowTitle(place, "이어하기");
            }
        }

        [Header("Debug")]
        [Tooltip("플레이 중 F1~F12 (Shift+F1~F12) 키로 시작 지점 목록의 위치로 순간이동(테스트용). 빌드에서 막으려면 끄세요.")]
        [SerializeField] private bool debugTeleportKeys = true;

        private void Update() {
            if (!debugTeleportKeys || playerRoot == null || !playerRoot.activeInHierarchy) return;
            var kb = Keyboard.current;
            if (kb == null) return;
            // F1~F12 → 0~11번, Shift + F1~F12 → 12~23번, Ctrl + F1~F12 → 24~35번 시작 지점
            var shift = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
            var ctrl = kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed;
            var alt = kb.leftAltKey.isPressed || kb.rightAltKey.isPressed;
            // Alt+F1 = 프롤로그(현실의 방)부터 다시
            if (alt && kb.f1Key.wasPressedThisFrame) { ReplayPrologue().Forget(); return; }
            for (var i = 0; i < 12; i++) {
                if (!kb[Key.F1 + i].wasPressedThisFrame) continue;
                var index = i + (ctrl ? 24 : shift ? 12 : 0);
                if (index < startPoints.Length) Teleport(index).Forget();
            }
        }

        private async UniTaskVoid ReplayPrologue() {
            CATestIntroCutscene.Cancel();
            if (streamer != null) await streamer.EnsureLoadedAt(prologueStart);
            if (this == null) return;
            var player = playerRoot.GetComponent<Player>();
            if (player == null || player.IsDead) return;
            if (CATestCutscene.IsPlaying) CATestCutscene.ForceUnlock();
            var body = player.GetComponent<Rigidbody2D>();
            if (body != null && !body.simulated) body.simulated = true;
            player.RestoreState(prologueStart, player.CurrentStamina);
            CATestPrologue.Pending = true;
            Debug.Log("[CATest] 프롤로그 다시 재생 (Alt+F1)");
        }

        private async UniTaskVoid Teleport(int i) {
            var pos = startPoints[i].position;
            CATestIntroCutscene.Cancel(); // 진행 중인 오프닝 컷신부터 즉시 멈춤(스트리밍 대기 중에 계속 굴러가지 않게)
            if (streamer != null) await streamer.EnsureLoadedAt(pos);
            if (this == null) return;
            var player = playerRoot.GetComponent<Player>();
            if (player != null && !player.IsDead) {
                CATestIntroCutscene.Cancel();
                if (CATestHUD.Instance != null) CATestHUD.FadeTo(0f, 0.3f).Forget();
                if (CATestCutscene.IsPlaying) CATestCutscene.ForceUnlock();
                var body = player.GetComponent<Rigidbody2D>();
                if (body != null && !body.simulated) body.simulated = true;
                player.RestoreState(pos, player.CurrentStamina);
                // F1(0번 = 오프닝 지점)은 컷신을 다시 재생한다. 틈 안에 그냥 두면 물리로 끼일 수 있음.
                if (i == 0 && playIntro) CATestIntroCutscene.Pending = true;
                else {
                    // 테스트 편의: 순간이동한 곳을 부활 위치로 저장(물리 위치가 반영되도록 2프레임 뒤)
                    await UniTask.DelayFrame(2);
                    if (player != null && !player.IsDead) {
                        // 라벨 "C+F12 용의 둥지 (엔딩 직전)" → 앞의 키 이름을 떼고 장소 이름으로 저장(타이틀 '이어하기' 옆에 표시됨)
                        var label = startPoints[i].label ?? string.Empty;
                        var sp = label.IndexOf(' ');
                        CATestSave.SaveHere(sp >= 0 ? label.Substring(sp + 1) : label);
                    }
                }
                Debug.Log($"[CATest] 순간이동 [{i}] {startPoints[i].label} {pos}");
            }
            else Debug.Log($"[CATest] 순간이동 [{i}] 무시됨 (player={(player != null)}, dead={(player != null && player.IsDead)})");
        }

#if UNITY_EDITOR
        private void OnDrawGizmos() {
            for (var i = 0; i < startPoints.Length; i++) {
                Gizmos.color = i == startIndex ? Color.green : new Color(0.3f, 1f, 0.3f, 0.35f);
                Gizmos.DrawWireSphere(startPoints[i].position, 1f);
                UnityEditor.Handles.Label(startPoints[i].position + Vector2.up * 1.5f, $"[{i}] {startPoints[i].label}");
            }
        }
#endif
    }
}
