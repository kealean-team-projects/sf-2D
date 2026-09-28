using System.Collections.Generic;
using _02._Script._04_Interaction;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace LHS_CATest {
    /// <summary>
    /// 빛에 반응하는 발판(발광 말미잘 패드).
    /// 연결된 InteractLight(상호작용으로 켜는 등불) 중 하나라도 켜져 있으면 실체화(콜라이더 ON, 밝게),
    /// 모두 꺼져 있으면 반투명 유령 상태(콜라이더 OFF)가 된다.
    /// </summary>
    public sealed class CATestLightBloomPlatform : MonoBehaviour {
        [SerializeField] private List<InteractLight> sources = new();
        [SerializeField] private Collider2D solid;
        [SerializeField] private SpriteRenderer[] visuals;
        [SerializeField] private Light2D glow;
        [SerializeField] private float ghostAlpha = 0.18f;
        [SerializeField] private float blendSpeed = 3f;
        [SerializeField] private float delay;

        private float _t;
        private float _litTime;

        public void AddSource(InteractLight l) {
            if (l != null && !sources.Contains(l)) sources.Add(l);
        }

        private void Update() {
            var lit = false;
            foreach (var s in sources) if (s != null && s.isActiveAndEnabled && s.IsActive) { lit = true; break; }
            _litTime = lit ? _litTime + Time.deltaTime : 0f;
            var target = lit && _litTime >= delay ? 1f : 0f;
            _t = Mathf.MoveTowards(_t, target, Time.deltaTime * blendSpeed);
            if (solid != null) solid.enabled = _t > 0.6f;
            var a = Mathf.Lerp(ghostAlpha, 1f, _t);
            if (visuals != null)
                foreach (var v in visuals) {
                    if (v == null) continue;
                    var c = v.color;
                    c.a = a;
                    v.color = c;
                }
            if (glow != null) glow.intensity = Mathf.Lerp(0.15f, 1.1f, _t) * (1f + Mathf.Sin(Time.time * 2.3f + transform.position.x) * 0.1f);
        }
    }
}
