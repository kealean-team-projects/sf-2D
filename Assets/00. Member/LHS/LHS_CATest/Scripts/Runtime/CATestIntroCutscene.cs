using _02._Script._01_Players;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LHS_CATest {
    /// <summary>
    /// 게임 시작 컷신 (화창한 숲 씬에 배치).
    ///
    /// 연출 순서
    ///  0) 시작 전: 화면 완전 검정 + 검은 띠(영화 모드) + 조작 잠금 (CATestBootstrap 이 Pending 을 켜 둠)
    ///  1) 플레이어를 바위 언덕 속 "비스듬한 좁은 틈"의 맨 위(path[0])에 둔다
    ///  2) 검은 화면이 천천히 걷히며, 틈을 따라 대각선으로 데굴데굴 굴러 내려간다 ("……으윽.")
    ///     - 물리 대신 경로(path)를 따라 직접 움직인다: 틈이 몸보다 조금 넓은 정도라
    ///       물리로 굴리면 벽에 걸리거나 멈출 수 있기 때문. (Rigidbody2D.simulated 를 잠시 끔)
    ///     - 굴러가는 느낌: 그림(Visual)만 몸 중심을 기준으로 회전. 이동 거리 1m 당 rollDegPerUnit 도.
    ///     - 꺾이는 지점을 지날 때 카메라를 살짝 흔들어 "부딪히는" 느낌
    ///  3) 틈 끝(절벽 면의 구멍)에서 숲 쪽으로 튕겨 나가 포물선을 그리며 떨어진다
    ///     - 중력 가속을 직접 계산하고, 매 프레임 발 아래로 레이캐스트해서 땅에 닿는 순간 착지
    ///  4) 착지: 회전을 원래대로, 물리 복구, 카메라 흔들림 → 잠시 정적 → 머리 위 대사
    ///  5) 검은 띠가 빠지고 조작 가능 → 화면 상단에 "화창한 숲" 제목
    /// </summary>
    public sealed class CATestIntroCutscene : MonoBehaviour {
        [Header("틈 경로 (위 → 아래, 몸 중심이 지나가는 선)")]
        [SerializeField] private Vector2[] path = { new(-1928f, 84f), new(-1909f, 55f), new(-1884f, 21f) };

        [Header("타이밍")]
        [SerializeField] private float blackHold = 1.0f;
        [SerializeField] private float revealTime = 2.4f;

        [Header("구르기")]
        [SerializeField] private float startSpeed = 3f;
        [SerializeField] private float accel = 4.5f;
        [SerializeField] private float maxSpeed = 13f;
        [SerializeField] private float rollDegPerUnit = 75f;

        [Header("튕겨 나가기")]
        [SerializeField] private Vector2 launchVelocity = new(8f, 2.5f);
        [SerializeField] private float gravity = 29.43f;
        [SerializeField] private LayerMask groundMask = 1 << 6;

        /// <summary>CATestBootstrap 이 "처음부터 시작"일 때 켠다.</summary>
        public static bool Pending;

        private static int _runId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Pending = false; _runId = 0; }

        /// <summary>테스트 순간이동 등으로 컷신을 중단할 때 호출(진행 중인 루프가 멈추고 물리/그림을 원상복구).</summary>
        public static void Cancel() {
            _runId++;
            Pending = false;
        }

        private void Update() {
            if (!Pending) return;
            var p = CATestHUD.CurrentPlayer;
            if (p == null || !p.isActiveAndEnabled) return;
            Pending = false;
            Run(p).Forget();
        }

        /// <summary>회전시킬 그림 Transform: "Visual" 자식 → Animator 자식 → SpriteRenderer 자식 순으로 찾는다.</summary>
        private static Transform FindVisual(Component p) {
            var t = p.transform.Find("Visual");
            if (t != null) return t;
            var anim = p.GetComponentInChildren<Animator>();
            if (anim != null && anim.transform != p.transform) return anim.transform;
            var sr = p.GetComponentInChildren<SpriteRenderer>();
            return sr != null && sr.transform != p.transform ? sr.transform : null;
        }

        private async UniTaskVoid Run(Player p) {
            if (path == null || path.Length < 2) return;
            var id = ++_runId;
            CATestCutscene.Begin();
            CATestHUD.SetLetterbox(true, 0.01f);
            await CATestHUD.FadeTo(1f, 0f);
            if (p == null) return;

            var root = p.transform;
            var rb = p.GetComponent<Rigidbody2D>();
            var col = p.GetComponent<Collider2D>();
            // 몸 중심 / 발끝 까지의 거리 (회전 0, 물리 켜진 상태에서 미리 잰다)
            var centerOff = col != null ? (Vector2)(col.bounds.center - root.position) : new Vector2(0f, 1f);
            var halfH = col != null ? col.bounds.extents.y : 1f;
            var visual = FindVisual(p);
            var visPos = visual != null ? visual.localPosition : Vector3.zero;
            var visRot = visual != null ? visual.localRotation : Quaternion.identity;
            var cLocal = (Vector2)root.InverseTransformPoint((Vector2)root.position + centerOff);
            var angle = 0f;

            bool Cancelled() {
                if (id == _runId && p != null) return false;
                if (visual != null) { visual.localPosition = visPos; visual.localRotation = visRot; }
                if (rb != null) rb.simulated = true;
                return true;
            }
            void Place(Vector2 center) => root.position = new Vector3(center.x - centerOff.x, center.y - centerOff.y, root.position.z);
            void Roll(float deg) {
                if (visual == null) return;
                angle += deg;
                var sign = root.lossyScale.x < 0f ? 1f : -1f; // 오른쪽으로 구를 때 시계 방향
                var q = Quaternion.Euler(0f, 0f, angle * sign);
                visual.localPosition = (Vector3)cLocal + q * ((Vector3)visPos - (Vector3)cLocal);
                visual.localRotation = q * visRot;
            }

            if (rb != null) { rb.linearVelocity = Vector2.zero; rb.simulated = false; }
            Place(path[0]);

            // 1) 검은 화면에서 잠깐 정지
            await CATestCutscene.Wait(blackHold);
            if (Cancelled()) return;

            // 2) 화면이 걷히며 틈을 따라 굴러 내려감
            CATestHUD.FadeTo(0f, revealTime).Forget();
            CATestAudio.PlaySfx("crevice_roll");
            var said = false;
            var t = 0f;
            var seg = 0;
            var along = 0f;
            var speed = startSpeed;
            while (seg < path.Length - 1) {
                if (Cancelled()) return;
                var dt = Time.deltaTime;
                t += dt;
                speed = Mathf.Min(maxSpeed, speed + accel * dt);
                along += speed * dt;
                var a = path[seg];
                var b = path[seg + 1];
                var len = Vector2.Distance(a, b);
                while (along >= len && seg < path.Length - 1) {
                    along -= len;
                    seg++;
                    if (seg < path.Length - 1) {
                        CATestCutscene.Shake(0.18f, 0.2f); // 꺾이는 곳에 부딪힘
                        a = path[seg];
                        b = path[seg + 1];
                        len = Vector2.Distance(a, b);
                    }
                }
                if (seg >= path.Length - 1) { Place(path[^1]); break; }
                Place(Vector2.Lerp(a, b, along / len));
                Roll(speed * dt * rollDegPerUnit);
                if (!said && t > 1.3f) {
                    said = true;
                    CATestHUD.Say(CATestLines.Get("intro_fall")[0], 0.7f).Forget();
                }
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
            if (p == null) return;

            // 3) 절벽 구멍에서 튕겨 나가 포물선 낙하
            var c = path[^1];
            var v = launchVelocity;
            var airT = 0f;
            while (airT < 4f) {
                if (Cancelled()) return;
                var dt = Time.deltaTime;
                airT += dt;
                v.y -= gravity * dt;
                var next = c + v * dt;
                if (v.y < 0f) {
                    // 발끝에서 이번 프레임 이동량만큼 아래로 레이캐스트 → 땅에 닿으면 착지
                    var feet = new Vector2(next.x, c.y - halfH + 0.05f);
                    var hit = Physics2D.Raycast(feet, Vector2.down, Mathf.Abs(next.y - c.y) + 0.1f, groundMask);
                    if (hit.collider != null) { c = new Vector2(next.x, hit.point.y + halfH + 0.02f); break; }
                }
                c = next;
                Place(c);
                Roll(v.magnitude * dt * rollDegPerUnit * 0.8f);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
            if (p == null) return;

            // 4) 착지
            if (Cancelled()) return;
            Place(c);
            if (visual != null) { visual.localPosition = visPos; visual.localRotation = visRot; }
            if (rb != null) { rb.simulated = true; rb.linearVelocity = Vector2.zero; }
            CATestCutscene.Shake(0.4f, 0.35f);
            CATestAudio.PlaySfx("crevice_land", c);
            // 첫 부활 위치를 착지 지점으로 저장. (저장 안 하면 게임 시작 위치 = 틈 꼭대기에서 부활해 틈에 끼인다)
            await UniTask.DelayFrame(2);
            CATestSave.SaveHere("화창한 숲");
            await CATestCutscene.Wait(1.6f);
            if (id != _runId) return;
            await CATestHUD.SayLines(CATestLines.Get("intro_land"), 0.35f);
            await CATestCutscene.Wait(0.3f);
            CATestCutscene.End();
            await CATestCutscene.Wait(0.7f);
            CATestHUD.ShowTitle("화창한 숲", "숲 챕터 · 시작");
        }

        private void OnDisable() {
            // 컷신 도중 씬이 내려가도 플레이어 물리가 꺼진 채 남지 않도록
            var p = CATestHUD.CurrentPlayer;
            if (p == null) return;
            var rb = p.GetComponent<Rigidbody2D>();
            if (rb != null && !rb.simulated) rb.simulated = true;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos() {
            if (path == null) return;
            Gizmos.color = new Color(1f, 0.8f, 0.3f, 0.8f);
            for (var i = 0; i < path.Length - 1; i++) Gizmos.DrawLine(path[i], path[i + 1]);
        }
#endif
    }
}
