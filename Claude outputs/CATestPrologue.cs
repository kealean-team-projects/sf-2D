using System.Reflection;
using _02._Script._01_Players;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LHS_CATest {
    /// <summary>
    /// 프롤로그 — 현실 세계의 방 (CATest_Prologue 씬, x -2402 ~ -2382.5, CATestWorld 참고).
    ///
    /// 흐름
    ///  0) 새로 시작 → CATestBootstrap 이 플레이어를 방에 두고 Pending 을 켬. 화면은 검정 + 영화 띠.
    ///  1) 침대에 누운 채(그림만 90° 눕힘, 물리 끔) 눈을 깜빡이듯 화면이 두어 번 열렸다 닫혔다 밝아진다.
    ///     비몽사몽 효과: 전용 Volume(비네트·색수차·렌즈 왜곡·채도↓)을 세게 → 이후 약하게 숨 쉬듯 출렁임.
    ///  2) 대사 → 이불이 흘러내리며 몸을 일으켜 침대 옆에 선다 → 조작 가능. 이동 속도는 느리게(비틀비틀).
    ///  3) 방문 앞에서 E → 문짝이 열리며(경첩 쪽으로 폭이 줄어듦) 문 너머에서 흰 금빛이 쏟아진다.
    ///     빛 입자가 문 쪽으로 빨려 들어가고, 플레이어도 조작이 잠긴 채 문 안으로 끌려 들어간다.
    ///  4) 화면이 흰색으로 덮였다가 검정으로 바뀌는 동안 화창한 숲 맵을 불러 두고,
    ///     기존 오프닝(CATestIntroCutscene: 틈을 굴러 떨어짐)으로 이어진다.
    /// </summary>
    public sealed class CATestPrologue : MonoBehaviour {
        [Header("위치")]
        [SerializeField] private Vector2 bedCenter = new(-2395.6f, 1.95f);   // 누운 몸(콜라이더 중심)이 놓일 곳
        [SerializeField] private Vector2 standPoint = new(-2391.2f, 0.2f);   // 일어난 뒤 서는 곳(플레이어 루트)
        [SerializeField] private float doorX = -2376f;
        [SerializeField] private Vector2 crevicePoint = new(-1928f, 84f);    // 다음(오프닝) 지점 — 미리 로드

        [Header("연출 대상")]
        [SerializeField] private CATestRoomDoor door;
        [SerializeField] private Transform doorPanel;       // 경첩(왼쪽)이 피벗인 문짝
        [SerializeField] private SpriteRenderer doorPanelSr;
        [SerializeField] private SpriteRenderer doorLight;  // 문 너머 흰 빛(문짝 뒤)
        [SerializeField] private SpriteRenderer doorGlow;   // 가산 번짐
        [SerializeField] private Light2D doorLight2D;
        [SerializeField] private Light2D gapLight;          // 문 아래 틈으로 새는 빛(처음부터 희미하게)
        [SerializeField] private ParticleSystem pullMotes;  // 문 쪽으로 빨려 드는 빛 입자
        [SerializeField] private SpriteRenderer blanket;
        [SerializeField] private Volume groggyVolume;

        [Header("속도")]
        [SerializeField] private float groggySpeed = 4.2f;

        public static bool Pending;
        public bool CanOpenDoor { get; private set; }

        private static readonly FieldInfo MoveSpeedField = typeof(Player).GetField("moveSpeed", BindingFlags.Instance | BindingFlags.NonPublic);
        private float _normalSpeed = 10f;
        private bool _groggy;
        private int _runId;
        private Vector3 _blanketHome;
        private Color _blanketColor;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Pending = false;

        private void Awake() {
            if (blanket != null) { _blanketHome = blanket.transform.localPosition; _blanketColor = blanket.color; }
            ResetRoom();
        }

        private void ResetRoom() {
            CanOpenDoor = false;
            if (door != null) door.ResetDoor();
            if (doorPanel != null) doorPanel.localScale = Vector3.one;
            if (doorPanelSr != null) doorPanelSr.color = Color.white;
            SetAlpha(doorLight, 0f);
            SetAlpha(doorGlow, 0f);
            if (doorLight2D != null) doorLight2D.intensity = 0f;
            if (pullMotes != null) pullMotes.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (blanket != null) { blanket.transform.localPosition = _blanketHome; blanket.color = _blanketColor; }
            if (groggyVolume != null) groggyVolume.weight = 0f;
        }

        private void Update() {
            if (Pending) {
                var p = CATestHUD.CurrentPlayer;
                if (p != null && p.isActiveAndEnabled) {
                    Pending = false;
                    Run(p).Forget();
                }
            }
            // 비몽사몽: 효과가 숨 쉬듯 약해졌다 강해졌다
            if (_groggy && groggyVolume != null)
                groggyVolume.weight = 0.55f + 0.2f * Mathf.Sin(Time.time * 1.3f) + 0.08f * Mathf.Sin(Time.time * 3.1f);
            // 문 아래 틈의 빛이 아주 천천히 일렁임(무언가 이상하다는 힌트)
            if (gapLight != null && !CanOpenDoorUsed) gapLight.intensity = 0.55f + 0.25f * Mathf.Sin(Time.time * 0.9f);
        }
        private bool CanOpenDoorUsed => door != null && door.Opened;

        private static Transform FindVisual(Component p) {
            var t = p.transform.Find("Visual");
            if (t != null) return t;
            var anim = p.GetComponentInChildren<Animator>();
            if (anim != null && anim.transform != p.transform) return anim.transform;
            var sr = p.GetComponentInChildren<SpriteRenderer>();
            return sr != null && sr.transform != p.transform ? sr.transform : null;
        }

        private async UniTaskVoid Run(Player p) {
            var id = ++_runId;
            ResetRoom();
            CATestCutscene.Begin();
            CATestHUD.SetLetterbox(true, 0.01f);
            CATestHUD.SetFadeColor(Color.black);
            await CATestHUD.FadeTo(1f, 0f);
            if (p == null) return;
            if (MoveSpeedField != null) _normalSpeed = (float)MoveSpeedField.GetValue(p);
            p.FaceDirection(1f);

            // ── 누운 자세 만들기: 그림(Visual)만 몸 중심을 기준으로 90° 회전 (머리가 왼쪽 베개 쪽) ──
            var root = p.transform;
            var rb = p.GetComponent<Rigidbody2D>();
            var col = p.GetComponent<Collider2D>();
            var centerOff = col != null ? (Vector2)(col.bounds.center - root.position) : new Vector2(0f, 1f);
            var visual = FindVisual(p);
            var visPos = visual != null ? visual.localPosition : Vector3.zero;
            var visRot = visual != null ? visual.localRotation : Quaternion.identity;
            var cLocal = (Vector2)root.InverseTransformPoint((Vector2)root.position + centerOff);
            var sign = root.lossyScale.x < 0f ? -1f : 1f;
            void Pose(float deg) {
                if (visual == null) return;
                var q = Quaternion.Euler(0f, 0f, deg * sign);
                visual.localPosition = (Vector3)cLocal + q * ((Vector3)visPos - (Vector3)cLocal);
                visual.localRotation = q * visRot;
            }
            void PlaceCenter(Vector2 c) => root.position = new Vector3(c.x - centerOff.x, c.y - centerOff.y, root.position.z);
            bool Cancelled() {
                if (id == _runId && p != null) return false;
                if (visual != null) { visual.localPosition = visPos; visual.localRotation = visRot; }
                if (rb != null) rb.simulated = true;
                return true;
            }

            if (rb != null) { rb.linearVelocity = Vector2.zero; rb.simulated = false; }
            PlaceCenter(bedCenter);
            Pose(90f);
            if (groggyVolume != null) groggyVolume.weight = 1f;

            // ── 1) 눈 깜빡임: 살짝 열림 → 감김 → 조금 더 열림 → 감김 → 뜸 ──
            await CATestCutscene.Wait(1.4f);
            if (Cancelled()) return;
            await CATestHUD.FadeTo(0.62f, 1.3f);
            await CATestHUD.FadeTo(0.97f, 0.35f);
            await CATestCutscene.Wait(0.5f);
            await CATestHUD.FadeTo(0.35f, 1.0f);
            await CATestHUD.FadeTo(0.9f, 0.3f);
            await CATestCutscene.Wait(0.3f);
            CATestHUD.FadeTo(0f, 1.8f).Forget();
            await CATestCutscene.Wait(1.0f);
            if (Cancelled()) return;
            await CATestHUD.SayLines(CATestLines.Get("prologue_wake"), 0.4f, root);
            if (Cancelled()) return;

            // ── 2) 일어나기: 이불이 흘러내리고 몸을 세워 침대 옆으로 ──
            CATestAudio.PlaySfx("room_wake", root.position);
            var startC = bedCenter;
            var endC = standPoint + centerOff;
            var blanketFrom = blanket != null ? blanket.transform.localPosition : Vector3.zero;
            for (var t = 0f; t < 1.4f; t += Time.deltaTime) {
                if (Cancelled()) return;
                var k = Mathf.SmoothStep(0f, 1f, t / 1.4f);
                Pose(Mathf.Lerp(90f, 0f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 1.0f))));
                // 몸 중심이 위로 살짝 솟았다가 침대 옆 바닥으로 내려오는 곡선
                var c = Vector2.Lerp(startC, endC, k) + Vector2.up * Mathf.Sin(k * Mathf.PI) * 0.9f;
                PlaceCenter(c);
                if (blanket != null) {
                    blanket.transform.localPosition = blanketFrom + new Vector3(k * 0.6f, -k * 0.35f, 0f);
                    var bc = _blanketColor; bc.a = Mathf.Lerp(_blanketColor.a, 0f, Mathf.Clamp01((t - 0.6f) / 0.8f));
                    blanket.color = bc;
                }
                await UniTask.Yield();
            }
            Pose(0f);
            if (visual != null) { visual.localPosition = visPos; visual.localRotation = visRot; }
            if (rb != null) { rb.simulated = true; rb.linearVelocity = Vector2.zero; }
            p.RestoreState(standPoint, p.CurrentStamina);
            p.FaceDirection(1f);
            await CATestCutscene.Wait(0.5f);
            await CATestHUD.SayLines(CATestLines.Get("prologue_up"), 0.35f, root);
            if (Cancelled()) return;

            // ── 비틀거리며 직접 걷기 ──
            _groggy = true;
            p.ChangeSpeed(groggySpeed);
            CATestCutscene.End();
            CanOpenDoor = true;
            CATestHUD.ShowTitle("새벽 5시 47분", "프롤로그");
        }

        /// <summary>CATestRoomDoor 가 호출 — 문이 열리고 빛에 빨려 들어간다.</summary>
        public void OnDoorOpened(Player p) => DoorSequence(p).Forget();

        private async UniTaskVoid DoorSequence(Player p) {
            var id = _runId;
            CanOpenDoor = false;
            CATestCutscene.Begin();
            if (p != null) p.FaceDirection(1f);
            CATestAudio.SetOverrideBgm("__silence"); // 배경음은 잦아들고 문 너머 소리만
            CATestAudio.PlaySfx("door_open", new Vector3(doorX, 2f, 0f));

            // 문짝이 경첩(왼쪽) 쪽으로 돌아가며 폭이 줄어듦 + 문 너머 빛이 드러남
            if (pullMotes != null) pullMotes.Play(true);
            for (var t = 0f; t < 1.6f; t += Time.deltaTime) {
                if (id != _runId) return;
                var k = Mathf.SmoothStep(0f, 1f, t / 1.6f);
                if (doorPanel != null) doorPanel.localScale = new Vector3(Mathf.Lerp(1f, 0.16f, k), 1f, 1f);
                if (doorPanelSr != null) doorPanelSr.color = Color.Lerp(Color.white, new Color(0.55f, 0.5f, 0.5f), k);
                SetAlpha(doorLight, k);
                SetAlpha(doorGlow, k * 0.7f);
                if (doorLight2D != null) doorLight2D.intensity = k * 2.2f;
                if (gapLight != null) gapLight.intensity = 0.8f + k * 1.5f;
                await UniTask.Yield();
            }
            await CATestHUD.SayLines(CATestLines.Get("prologue_door"), 0.35f, p != null ? p.transform : null);
            if (id != _runId || p == null) return;

            // 빨려 들어감: 빛이 더 강해지고, 몸이 문 쪽으로 끌려감
            CATestAudio.PlaySfx("realm_whoosh");
            CATestCutscene.Shake(0.25f, 2.4f);
            CATestHUD.Say(CATestLines.Get("prologue_pulled")[0], 0.8f).Forget();
            var rb = p.GetComponent<Rigidbody2D>();
            if (rb != null) { rb.linearVelocity = Vector2.zero; rb.simulated = false; }
            var from = p.transform.position;
            var to = new Vector3(doorX, from.y + 0.3f, from.z);
            CATestHUD.SetFadeColor(new Color(1f, 0.98f, 0.94f));
            CATestHUD.FadeTo(1f, 2.4f).Forget();
            for (var t = 0f; t < 2.4f; t += Time.deltaTime) {
                if (id != _runId || p == null) return;
                var k = t / 2.4f;
                p.transform.position = Vector3.Lerp(from, to, k * k);
                if (doorLight2D != null) doorLight2D.intensity = 2.2f + k * 6f;
                if (doorGlow != null) {
                    SetAlpha(doorGlow, 0.7f + 0.3f * k);
                    doorGlow.transform.localScale = Vector3.one * Mathf.Lerp(1f, 3.2f, k);
                }
                if (groggyVolume != null) groggyVolume.weight = Mathf.Lerp(groggyVolume.weight, 0f, Time.deltaTime * 2f);
                await UniTask.Yield();
            }
            _groggy = false;
            if (groggyVolume != null) groggyVolume.weight = 0f;
            if (p != null) p.ChangeSpeed(_normalSpeed);
            await CATestCutscene.Wait(0.6f);

            // 흰색 → 검정으로 천천히 (다음 컷신은 검은 화면에서 시작)
            for (var t = 0f; t < 1.2f; t += Time.unscaledDeltaTime) {
                CATestHUD.SetFadeColor(Color.Lerp(new Color(1f, 0.98f, 0.94f), Color.black, t / 1.2f));
                await UniTask.Yield();
            }
            CATestHUD.SetFadeColor(Color.black);
            if (pullMotes != null) pullMotes.Stop();

            // 다음 맵(화창한 숲) 미리 로드 → 오프닝 컷신으로 이어짐
            var streamer = CATestSceneStreamer.Instance;
            if (streamer != null) await streamer.EnsureLoadedAt(crevicePoint);
            if (p == null) return;
            if (rb != null) rb.simulated = true;
            p.RestoreState(crevicePoint, p.CurrentStamina);
            CATestAudio.ClearOverrideBgm();
            CATestCutscene.End();
            CATestIntroCutscene.Pending = true;
        }

        private static void SetAlpha(SpriteRenderer sr, float a) {
            if (sr == null) return;
            var c = sr.color; c.a = a; sr.color = c;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos() {
            Gizmos.color = new Color(1f, 0.8f, 0.4f, 0.8f);
            Gizmos.DrawWireSphere(bedCenter, 0.4f);
            Gizmos.DrawWireSphere(standPoint, 0.3f);
            Gizmos.DrawLine(new Vector3(doorX, 0f), new Vector3(doorX, 5f));
        }
#endif
    }
}
