using _02._Script._01_Players;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LHS_CATest {
    /// <summary>
    /// 용의 둥지 — 엔딩 연출 감독.
    ///
    /// 흐름
    ///  1) 플레이어가 둥지 한가운데(triggerArea)에 들어서면: 조작 잠금 + 검은 띠 + 카메라가 둥지 전체를 비춤
    ///  2) 수호룡이 오른쪽 화면 밖에서 곡선을 그리며 날아와 제자리 비행 → 포효(화면 흔들림)
    ///  3) 대화창에 용의 대사(한 줄씩, 진행 키로 넘김)
    ///  4) 선택지 3개 → 엔딩 분기
    ///     A 귀환 : 용이 문을 열어 줌 → 흰 빛에 삼켜짐 → 흰 엔딩 화면
    ///     B 잔류 : 용이 받아들이고 떠남 → 황혼 속으로 천천히 어두워짐
    ///     C 도전 : 용에게 맞섬 → 용의 일격에 속수무책으로 튕겨 나감 → 붉게 → 검게 (배드 엔딩)
    ///  5) 엔딩 기록 저장 → 엔딩 화면(CATestEndingCard) → 아무 키 → 타이틀
    /// 이어하기를 누르면 둥지 앞 세이브 지점(엔딩 직전)에서 다시 시작하므로 다른 엔딩도 볼 수 있다.
    ///
    /// 대사/선택지 문장은 CATestLines 에 모여 있다(dragon_*, ending_*).
    /// </summary>
    public sealed class CATestEndingDirector : MonoBehaviour {
        [SerializeField] private Rect triggerArea = new(1924, 12, 8, 12);
        [SerializeField] private CATestDragon dragon;
        [SerializeField] private Vector3 dragonStart = new(2010f, 50f, 4f);
        [SerializeField] private Vector3 dragonHover = new(1958f, 24f, 4f);
        [SerializeField] private Vector3 cameraPoint = new(1944f, 22f, 0f);
        [SerializeField] private float cameraDistance = 165f;
        [SerializeField] private Vector3 lungeHeadOffset = new(10f, 2.5f, 0f); // 일격 때 머리 뼈가 멈출 위치(플레이어 기준). 주둥이가 머리 뼈보다 약 9.5유닛 앞(왼쪽)이라 +10 이면 주둥이가 플레이어에 닿음
        [SerializeField] private string dragonName = "수호룡";

        public static bool IsRunning { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => IsRunning = false;

        private bool _done;

        private void Start() {
            if (dragon != null) dragon.gameObject.SetActive(false);
        }

        private void OnDestroy() {
            if (IsRunning) IsRunning = false;
        }

        private void Update() {
            if (_done || IsRunning) return;
            var p = CATestHUD.CurrentPlayer;
            if (p == null || !p.isActiveAndEnabled || p.IsDead || !p.IsGrounded) return;
            if (!triggerArea.Contains(p.transform.position)) return;
            _done = true;
            Run(p).Forget();
        }

        private async UniTaskVoid Run(Player p) {
            IsRunning = true;
            var box = FindAnyObjectByType<CATestDialogueBox>(FindObjectsInactive.Include);
            var card = FindAnyObjectByType<CATestEndingCard>(FindObjectsInactive.Include);
            CATestCutscene.Begin();
            await CATestCutscene.Wait(0.6f);
            await CATestHUD.SayLines(CATestLines.Get("nest_arrive"), 0.3f);

            // 카메라: 둥지 전체
            CATestCutscene.Focus(cameraPoint, 2.2f, cameraDistance);
            CATestCutscene.Shake(0.12f, 2.5f);
            await CATestCutscene.Wait(1.4f);

            // 용 등장 — 플레이어는 오른쪽(용이 나타나는 쪽)을 바라본다
            if (p != null) p.FaceDirection(1f);
            if (dragon != null) {
                dragon.gameObject.SetActive(true);
                dragon.Teleport(dragonStart);
                dragon.SetEye(new Color(1f, 0.8f, 0.45f), 1.4f);
                await dragon.FlyTo(dragonHover, 3.4f, 10f);
                dragon.Play("Roar", 0.2f);
                CATestCutscene.Shake(0.7f, 1.4f);
                await CATestCutscene.Wait(1.8f);
                dragon.Play("Idle", 0.5f);
            }
            await CATestCutscene.Wait(0.6f);

            // 대사
            foreach (var line in CATestLines.Get("dragon_intro")) {
                if (dragon != null) dragon.Play("Speak", 0.25f);
                if (box != null) await box.Say(dragonName, line);
                else await CATestCutscene.Wait(2f);
            }
            if (dragon != null) dragon.Play("Idle", 0.4f);

            // 선택
            var choice = box != null ? await box.Choose(CATestLines.Get("dragon_choices")) : 0;
            switch (choice) {
                case 0: await EndingReturn(p, box, card); break;
                case 1: await EndingStay(p, box, card); break;
                default: await EndingFight(p, box, card); break;
            }
            CATestHUD.SetFadeColor(Color.black);
            CATestSceneFlow.GoToTitle();
        }

        private async UniTask DragonLines(CATestDialogueBox box, string key) {
            foreach (var line in CATestLines.Get(key)) {
                if (dragon != null) dragon.Play("Speak", 0.25f);
                if (box != null) await box.Say(dragonName, line);
            }
            if (dragon != null) dragon.Play("Idle", 0.4f);
            if (box != null) await box.Close();
        }

        // ───────────── A. 귀환 ─────────────
        private async UniTask EndingReturn(Player p, CATestDialogueBox box, CATestEndingCard card) {
            await DragonLines(box, "ending_a_dragon");
            await CATestHUD.SayLines(CATestLines.Get("ending_a_player"), 0.3f);
            if (dragon != null) dragon.SetEye(new Color(0.9f, 0.95f, 1f), 3f);
            CATestCutscene.Shake(0.2f, 3f);
            FadePlayer(p, 3f).Forget();
            CATestHUD.SetFadeColor(new Color(0.97f, 0.96f, 0.98f));
            await CATestHUD.FadeTo(1f, 3.2f);
            CATestSave.MarkEnding(0);
            if (card != null)
                await card.Play("ENDING A", CATestTitleScreen.EndingTitles[0], CATestLines.Get("ending_a_epilogue"), new Color(0.97f, 0.96f, 0.98f), false);
        }

        // ───────────── B. 잔류 ─────────────
        private async UniTask EndingStay(Player p, CATestDialogueBox box, CATestEndingCard card) {
            await DragonLines(box, "ending_b_dragon");
            await CATestHUD.SayLines(CATestLines.Get("ending_b_player"), 0.3f);
            CATestCutscene.Focus(cameraPoint + new Vector3(-14f, 8f, 0f), 6f, cameraDistance * 1.45f); // 천천히 멀어지며 하늘(일식)이 크게. 왼쪽으로 조금 옮겨 오른쪽 끝 벽이 화면에 안 들어오게
            if (dragon != null) dragon.FlyAway(dragonHover + new Vector3(-170f, 75f, 30f), 8f).Forget();
            await CATestCutscene.Wait(3.5f);
            CATestHUD.SetFadeColor(new Color(0.05f, 0.03f, 0.08f));
            await CATestHUD.FadeTo(1f, 3.5f);
            CATestSave.MarkEnding(1);
            if (card != null)
                await card.Play("ENDING B", CATestTitleScreen.EndingTitles[1], CATestLines.Get("ending_b_epilogue"), new Color(0.05f, 0.03f, 0.08f), false);
        }

        // ───────────── C. 도전 (배드 엔딩) ─────────────
        private async UniTask EndingFight(Player p, CATestDialogueBox box, CATestEndingCard card) {
            if (box != null) await box.Close();
            await CATestHUD.SayLines(CATestLines.Get("ending_c_player"), 0.2f);
            await DragonLines(box, "ending_c_dragon");
            if (dragon != null) {
                dragon.SetEye(new Color(1f, 0.15f, 0.12f), 3.5f);
                dragon.Play("Roar", 0.15f);
            }
            CATestCutscene.Shake(0.9f, 1.6f);
            await CATestCutscene.Wait(1.2f);
            // 플레이어가 한 걸음 내딛는 순간 — 용의 일격
            await StepForward(p, 2.2f, 0.35f);
            if (dragon != null) {
                // 거대한 몸이라 "몸 중심 → 플레이어"로 움직이면 머리가 엉뚱한 곳에 간다.
                // 머리 뼈가 (플레이어 + lungeHeadOffset) 에 오도록 몸 전체의 이동량을 계산.
                var target = (p != null ? p.transform.position : dragonHover) + lungeHeadOffset;
                var move = target - dragon.Head.position;
                move.z = 0f;
                dragon.Lunge(dragon.transform.position + move, move.magnitude).Forget();
            }
            await CATestCutscene.Wait(CATestDragon.LungeImpactTime - 0.02f); // 머리가 닿는 순간에 맞춰
            // 짧고 옅은 섬광(용의 머리가 가려지지 않도록 0.45 까지만)
            CATestHUD.SetFadeColor(Color.white);
            CATestHUD.FadeTo(0.45f, 0.04f).Forget();
            CATestCutscene.Shake(1.2f, 0.8f);
            await CATestCutscene.Wait(0.06f);
            CATestHUD.FadeTo(0f, 0.3f).Forget();
            await Knockback(p, new Vector2(-16f, 11f));
            await CATestCutscene.Wait(1.2f); // 용이 멈춰 선 모습 + 쓰러진 플레이어를 잠시 보여줌
            CATestHUD.SetFadeColor(new Color(0.45f, 0.02f, 0.03f));
            await CATestHUD.FadeTo(1f, 1.2f);
            await CATestCutscene.Wait(0.6f);
            CATestHUD.SetFadeColor(Color.black);
            await CATestCutscene.Wait(0.8f);
            CATestSave.MarkEnding(2);
            if (card != null)
                await card.Play("ENDING C  ·  BAD END", CATestTitleScreen.EndingTitles[2], CATestLines.Get("ending_c_epilogue"), Color.black, true);
        }

        // ───────────── 플레이어 연출 도우미 (물리를 잠시 끄고 직접 움직임) ─────────────
        private static async UniTask StepForward(Player p, float dist, float time) {
            if (p == null) return;
            var start = p.transform.position;
            for (var t = 0f; t < time; t += Time.deltaTime) {
                if (p == null) return;
                p.transform.position = start + Vector3.right * (dist * (t / time));
                await UniTask.Yield();
            }
        }

        private static async UniTask Knockback(Player p, Vector2 velocity) {
            if (p == null) return;
            var rb = p.GetComponent<Rigidbody2D>();
            var col = p.GetComponent<Collider2D>();
            var halfH = col != null ? col.bounds.extents.y : 1f;
            var centerOff = col != null ? (Vector2)(col.bounds.center - p.transform.position) : Vector2.up;
            var visual = p.transform.Find("Visual");
            var visPos = visual != null ? visual.localPosition : Vector3.zero;
            var visRot = visual != null ? visual.localRotation : Quaternion.identity;
            if (rb != null) { rb.linearVelocity = Vector2.zero; rb.simulated = false; }
            var c = (Vector2)p.transform.position + centerOff;
            var v = velocity;
            var angle = 0f;
            for (var t = 0f; t < 3f; t += Time.deltaTime) {
                if (p == null) return;
                v.y -= 30f * Time.deltaTime;
                var next = c + v * Time.deltaTime;
                if (v.y < 0f) {
                    var hit = Physics2D.Raycast(new Vector2(next.x, c.y - halfH + 0.05f), Vector2.down, Mathf.Abs(next.y - c.y) + 0.1f, 1 << 6);
                    if (hit.collider != null) { c = new Vector2(next.x, hit.point.y + halfH + 0.02f); break; }
                }
                c = next;
                p.transform.position = (Vector3)(c - centerOff) + new Vector3(0, 0, p.transform.position.z);
                angle += 720f * Time.deltaTime;
                if (visual != null) {
                    var cl = (Vector2)visual.parent.InverseTransformPoint(c);
                    var q = Quaternion.Euler(0, 0, angle);
                    visual.localPosition = (Vector3)cl + q * (visPos - (Vector3)cl);
                    visual.localRotation = q * visRot;
                }
                await UniTask.Yield();
            }
            if (p == null) return;
            p.transform.position = (Vector3)(c - centerOff) + new Vector3(0, 0, p.transform.position.z);
            if (visual != null) {
                // 쓰러진 자세: 옆으로 누운 채로 둔다
                var cl = (Vector2)visual.parent.InverseTransformPoint(c);
                var q = Quaternion.Euler(0, 0, 90f);
                visual.localPosition = (Vector3)cl + q * (visPos - (Vector3)cl) + Vector3.down * (halfH * 0.6f);
                visual.localRotation = q * visRot;
            }
            CATestCutscene.Shake(0.4f, 0.4f);
        }

        private static async UniTaskVoid FadePlayer(Player p, float time) {
            if (p == null) return;
            var rs = p.GetComponentsInChildren<SpriteRenderer>();
            var baseA = new float[rs.Length];
            for (var i = 0; i < rs.Length; i++) baseA[i] = rs[i].color.a;
            for (var t = 0f; t < time; t += Time.unscaledDeltaTime) {
                for (var i = 0; i < rs.Length; i++) {
                    if (rs[i] == null) continue;
                    var c = rs[i].color; c.a = baseA[i] * (1f - t / time); rs[i].color = c;
                }
                await UniTask.Yield();
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos() {
            Gizmos.color = new Color(1f, 0.8f, 0.4f, 0.6f);
            Gizmos.DrawWireCube(triggerArea.center, triggerArea.size);
            Gizmos.DrawWireSphere(dragonHover, 2f);
            Gizmos.DrawLine(dragonStart, dragonHover);
        }
#endif
    }
}
