using _02._Script._01_Players.Components.DamageCompo;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace LHS_CATest {
    /// <summary>
    /// "심판의 빛" 함정 (백색 성역). 하늘에서 내리꽂히는 빛기둥이 일정한 박자로 반복된다.
    ///
    /// ■ 박자 (phaseOffset 으로 기둥마다 엇갈리게)
    ///   쉼(offTime)  : 아무것도 없음
    ///   경고(warnTime): 바닥에 룬 원이 떠오르며 점점 밝아지고, 가느다란 빛줄기가 먼저 내려온다 → 피할 시간
    ///   발동(activeTime): 굵고 밝은 빛기둥. 이때 기둥 안에 있으면 사망
    /// ■ 판정: 기둥 폭(width) × 높이(height) 사각형과 플레이어 콜라이더가 겹치는지(Physics2D.OverlapBox, Player 레이어)
    /// ■ 비주얼: 빛기둥 = GodRay 셰이더 스프라이트(세로), 바닥 = 룬 원 스프라이트, Light2D 로 주변을 비춤
    /// </summary>
    public sealed class CATestJudgmentBeam : MonoBehaviour {
        [SerializeField] private float width = 3f;
        [SerializeField] private float height = 26f;
        [SerializeField] private float offTime = 1.4f;
        [SerializeField] private float warnTime = 0.9f;
        [SerializeField] private float activeTime = 0.8f;
        [SerializeField] private float phaseOffset;
        [SerializeField] private SpriteRenderer beam;
        [SerializeField] private SpriteRenderer warnBeam;
        [SerializeField] private SpriteRenderer runeCircle;
        [SerializeField] private Light2D glow;
        [SerializeField] private Color warnColor = new(1f, 0.55f, 0.45f, 1f);
        [SerializeField] private Color activeColor = new(1f, 0.93f, 0.85f, 1f);

        private readonly Collider2D[] _hits = new Collider2D[4];
        private int _phase = -1;

        private void Update() {
            var cycle = offTime + warnTime + activeTime;
            var t = Mathf.Repeat(Time.time + phaseOffset, cycle);
            float warnK = 0f, activeK = 0f;
            if (t >= offTime && t < offTime + warnTime) warnK = (t - offTime) / warnTime;
            else if (t >= offTime + warnTime) {
                warnK = 1f;
                var a = (t - offTime - warnTime) / activeTime;
                activeK = Mathf.Clamp01(Mathf.Min(a * 6f, (1f - a) * 4f)); // 빠르게 켜지고 조금 천천히 꺼짐
            }
            var lethal = t >= offTime + warnTime + 0.05f && t < cycle - 0.08f;
            // 효과음: 경고가 시작되는 순간 / 빛이 떨어지는 순간 (단계가 바뀌는 프레임에 한 번)
            var phase = t < offTime ? 0 : t < offTime + warnTime ? 1 : 2;
            if (phase != _phase) {
                if (phase == 1) CATestAudio.PlaySfx("beam_warn", transform.position);
                else if (phase == 2) CATestAudio.PlaySfx("beam_fire", transform.position);
                _phase = phase;
            }

            if (runeCircle != null) {
                var c = Color.Lerp(warnColor, activeColor, activeK);
                c.a = Mathf.Max(warnK * 0.9f, activeK) * (t < offTime ? 0f : 1f);
                runeCircle.color = c;
                runeCircle.transform.localRotation = Quaternion.Euler(0f, 0f, Time.time * 25f);
            }
            if (warnBeam != null) {
                var c = warnColor;
                c.a = warnK * (1f - activeK) * 0.6f * (t < offTime ? 0f : 1f);
                warnBeam.color = c;
            }
            if (beam != null) {
                var c = activeColor;
                c.a = activeK;
                beam.color = c;
                beam.enabled = activeK > 0.001f;
            }
            if (glow != null) {
                glow.color = Color.Lerp(warnColor, activeColor, activeK);
                glow.intensity = warnK * 0.6f * (t < offTime ? 0f : 1f) + activeK * 2.2f;
            }

            if (!lethal) return;
            var center = (Vector2)transform.position + new Vector2(0f, height * 0.5f);
            var n = Physics2D.OverlapBoxNonAlloc(center, new Vector2(width * 0.8f, height), 0f, _hits, 1 << 7);
            for (var i = 0; i < n; i++) {
                var p = _hits[i].GetComponentInParent<_02._Script._01_Players.Player>();
                if (p == null || p.IsDead) continue;
                if (p.TryGetComponent(out DamageModule dmg)) dmg.TakeDamage();
                break;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos() {
            Gizmos.color = new Color(1f, 0.5f, 0.4f, 0.5f);
            Gizmos.DrawWireCube(transform.position + new Vector3(0f, height * 0.5f, 0f), new Vector3(width * 0.8f, height, 0f));
        }
#endif
    }
}
