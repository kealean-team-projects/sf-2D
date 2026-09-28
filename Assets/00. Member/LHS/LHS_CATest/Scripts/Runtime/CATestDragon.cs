using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace LHS_CATest {
    /// <summary>
    /// 수호룡(DragonGuardian PSB + 스켈레탈 애니메이션) 제어.
    ///
    /// ■ 뼈 애니메이션: 빌더(CATestBuilder.Dragon)가 뼈 35개짜리 리그에 키프레임을 찍어 만든 클립들을 Animator 로 재생.
    ///   Idle(공중에 떠서 몸을 물결치듯) / Fly(빠르게 헤엄치듯 날기) / Speak(고개를 들고 말하듯 끄덕임) / Roar(목을 치켜들고 포효) / Strike(머리를 내리꽂는 일격)
    ///   → Play("Speak") 처럼 상태 이름으로 부드럽게 전환(CrossFade).
    /// ■ 위치 이동은 이 스크립트가 직접(뼈 애니메이션과 분리): FlyTo(목표, 시간) 로 곡선을 그리며 날아오고,
    ///   도착 후에는 hover 로 위아래로 천천히 떠 있다.
    /// ■ 눈빛(Light2D) 색을 바꿔 감정 표현(평소 금빛 → 분노 붉은빛 → 귀환 엔딩 흰빛).
    /// </summary>
    public sealed class CATestDragon : MonoBehaviour {
        [SerializeField] private Animator animator;
        [SerializeField] private Light2D eyeGlow;
        [SerializeField] private Light2D bodyRim;
        [SerializeField] private Transform head; // head_02 뼈 — 일격 때 "머리가 플레이어에게 닿도록" 거리 계산에 사용

        /// <summary>머리 뼈(없으면 루트).</summary>
        public Transform Head => head != null ? head : transform;
        [SerializeField] private float hoverAmp = 0.6f;
        [SerializeField] private float hoverSpeed = 0.8f;

        private Vector3 _anchor;
        private bool _hover;
        private float _hoverT;

        private void Awake() {
            _anchor = transform.position;
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        public void Play(string state, float fade = 0.3f) {
            if (animator != null && animator.isActiveAndEnabled) animator.CrossFadeInFixedTime(state, fade);
            if (state == "Roar") CATestAudio.PlaySfx("dragon_roar", Head.position);
        }

        public void SetEye(Color c, float intensity) {
            if (eyeGlow == null) return;
            eyeGlow.color = c;
            eyeGlow.intensity = intensity;
        }

        public void Teleport(Vector3 p) {
            transform.position = p;
            _anchor = p;
        }

        /// <summary>현재 위치에서 목표까지 위로 볼록한 곡선(베지어)을 그리며 난다. 끝나면 제자리 비행(hover).</summary>
        public async UniTask FlyTo(Vector3 target, float duration, float arcHeight = 6f) {
            _hover = false;
            Play("Fly", 0.2f);
            CATestAudio.PlaySfx("dragon_fly", Head.position);
            var start = transform.position;
            var mid = (start + target) * 0.5f + Vector3.up * arcHeight;
            for (var t = 0f; t < duration; t += Time.deltaTime) {
                if (this == null) return;
                var k = t / duration;
                k = 1f - (1f - k) * (1f - k); // 끝에서 감속
                var a = Vector3.Lerp(start, mid, k);
                var b = Vector3.Lerp(mid, target, k);
                transform.position = Vector3.Lerp(a, b, k);
                await UniTask.Yield();
            }
            if (this == null) return;
            transform.position = target;
            _anchor = target;
            _hoverT = 0f;
            _hover = true;
            Play("Idle", 0.5f);
        }

        /// <summary>일격이 목표에 닿기까지 걸리는 시간(뒤로 젖힘 + 돌진). 연출 쪽에서 섬광 타이밍을 맞출 때 사용.</summary>
        public const float LungeImpactTime = WindUp + StrikeTime;
        private const float WindUp = 0.45f;
        private const float StrikeTime = 0.22f;

        /// <summary>
        /// 머리를 목표 쪽으로 내리꽂는 일격.
        ///  1) 뒤로 살짝 젖힘(0.45초, 반대 방향으로 거리의 12%) — 큰 동작의 "예비 동작"이 있어야 무게감이 생김
        ///  2) 돌진(0.22초) — 빠르게
        ///  3) 그 자리에서 잠시 멈춤(0.8초) — 플레이어가 무엇에 당했는지 보이도록
        ///  4) 천천히 제자리로(1.4초)
        /// </summary>
        public async UniTask Lunge(Vector3 toward, float distance = 8f) {
            _hover = false;
            Play("Strike", 0.1f);
            var start = transform.position;
            var dir = (toward - start);
            dir.z = 0f;
            dir = dir.normalized;
            var back = start - dir * (distance * 0.12f);
            var hit = start + dir * distance;
            for (var t = 0f; t < WindUp; t += Time.deltaTime) {
                if (this == null) return;
                var k = t / WindUp;
                transform.position = Vector3.Lerp(start, back, 1f - (1f - k) * (1f - k));
                await UniTask.Yield();
            }
            CATestAudio.PlaySfx("dragon_strike", Head.position);
            for (var t = 0f; t < StrikeTime; t += Time.deltaTime) {
                if (this == null) return;
                var k = t / StrikeTime;
                transform.position = Vector3.Lerp(back, hit, k * k);
                await UniTask.Yield();
            }
            if (this == null) return;
            transform.position = hit;
            await UniTask.Delay(800);
            for (var t = 0f; t < 1.4f; t += Time.deltaTime) {
                if (this == null) return;
                var k = t / 1.4f;
                transform.position = Vector3.Lerp(hit, start, k * k * (3f - 2f * k));
                await UniTask.Yield();
            }
            if (this == null) return;
            transform.position = start;
            _anchor = start;
            _hover = true;
            Play("Idle", 0.4f);
        }

        public async UniTask FlyAway(Vector3 target, float duration) {
            _hover = false;
            Play("Fly", 0.3f);
            CATestAudio.PlaySfx("dragon_fly", Head.position);
            var start = transform.position;
            for (var t = 0f; t < duration; t += Time.deltaTime) {
                if (this == null) return;
                var k = t / duration;
                transform.position = Vector3.Lerp(start, target, k * k);
                await UniTask.Yield();
            }
        }

        private void Update() {
            if (!_hover) return;
            _hoverT += Time.deltaTime;
            transform.position = _anchor + new Vector3(Mathf.Sin(_hoverT * hoverSpeed * 0.5f) * hoverAmp * 0.4f,
                Mathf.Sin(_hoverT * hoverSpeed) * hoverAmp, 0f);
            if (bodyRim != null) bodyRim.intensity = 0.8f + Mathf.Sin(_hoverT * 1.3f) * 0.15f;
        }
    }
}
