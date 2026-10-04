using _02._Script._01_Players;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace LHS_CATest {
    /// <summary>
    /// 압력판. 위(detectSize 영역)에 "움직이는 무거운 물체(Dynamic Rigidbody2D, mass ≥ minMass)"나 플레이어가 있으면 눌린다.
    /// 플레이어가 내려오면 풀리므로, 문을 계속 열어 두려면 상자/바위를 올려놔야 한다 → 밀기 퍼즐.
    /// </summary>
    public sealed class CATestPressurePlate : MonoBehaviour, ICATestSignal {
        [SerializeField] private Vector2 detectSize = new(2.4f, 1.2f);
        [SerializeField] private Vector2 detectOffset = new(0f, 0.6f);
        [SerializeField] private LayerMask detectMask = ~0;
        [SerializeField] private float minMass = 1.5f;
        [SerializeField] private bool playerCanPress = true;
        [SerializeField] private Transform plateVisual;
        [SerializeField] private float pressDepth = 0.14f;
        [SerializeField] private Light2D indicator;
        [SerializeField] private Color offColor = new(1f, 0.45f, 0.3f);
        [SerializeField] private Color onColor = new(0.45f, 1f, 0.6f);

        private readonly Collider2D[] _hits = new Collider2D[16];
        private Vector3 _visualRest;
        public bool IsOn { get; private set; }

        private void Awake() {
            if (plateVisual != null) _visualRest = plateVisual.localPosition;
        }

        private void FixedUpdate() {
            var center = (Vector2)transform.position + detectOffset;
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(detectMask);
            var n = Physics2D.OverlapBox(center, detectSize, 0f, filter, _hits);
            var on = false;
            for (var i = 0; i < n && !on; i++) {
                var h = _hits[i];
                var rb = h.attachedRigidbody;
                if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic && rb.mass >= minMass && rb.GetComponent<Player>() == null) on = true;
                else if (playerCanPress && h.GetComponentInParent<Player>() != null) on = true;
            }
            if (on && !IsOn) CATestAudio.PlaySfx("plate_press", transform.position);
            IsOn = on;
        }

        private void Update() {
            if (plateVisual != null)
                plateVisual.localPosition = Vector3.Lerp(plateVisual.localPosition, _visualRest + (IsOn ? Vector3.down * pressDepth : Vector3.zero), Time.deltaTime * 12f);
            if (indicator != null) {
                indicator.color = Color.Lerp(indicator.color, IsOn ? onColor : offColor, Time.deltaTime * 6f);
                indicator.intensity = Mathf.Lerp(indicator.intensity, IsOn ? 1.2f : 0.5f, Time.deltaTime * 6f);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos() {
            Gizmos.color = IsOn ? Color.green : new Color(1f, 0.6f, 0.2f);
            Gizmos.DrawWireCube(transform.position + (Vector3)detectOffset, detectSize);
        }
#endif
    }
}
