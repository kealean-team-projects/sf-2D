using UnityEngine;
using UnityEngine.Events;

namespace ClamTrapArt {
    [RequireComponent(typeof(Animator))]
    public sealed class ClamTrapPlayer : MonoBehaviour {
        public const float CloseSeconds = 0.12f;
        public const float SettleSeconds = 0.08f;
        public const float OpenSeconds = 0.52f;
        private static readonly float[] GhostAlpha = { 0.20f, 0.12f, 0.065f };
        public SpriteRenderer leftShell;
        public SpriteRenderer rightShell;
        public SpriteRenderer closedExterior;
        public bool closeOnContact = true;
        public LayerMask activationLayers = ~0;
        public bool autoReopen = true;
        [Min(0f)] public float closedHoldSeconds = 0.6f;
        public UnityEvent onSnap = new();
        public UnityEvent onReady = new();
        private Animator animator;
        private float elapsed;
        private SpriteRenderer[,] ghosts;
        private int phase;
        private bool snapSent;
        public bool IsReady => phase == 0;
        public int VisibleGhostCount { get; private set; }

        private void Awake() {
            animator = GetComponent<Animator>();
            if (!leftShell || !rightShell || !closedExterior) {
                enabled = false;
                return;
            }

            ghosts = new SpriteRenderer[3, 3];
            SpriteRenderer[] originals = { leftShell, rightShell, closedExterior };
            for (var i = 0; i < 3; i++)
            for (var j = 0; j < 3; j++) {
                var go = new GameObject("Afterimage_" + i + "_" + j);
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = originals[j].sprite;
                sr.sharedMaterial = originals[j].sharedMaterial;
                sr.sortingLayerID = originals[j].sortingLayerID;
                sr.sortingOrder = originals[j].sortingOrder - 20 - i * 3;
                sr.color = new Color(1, 1, 1, 0);
                ghosts[i, j] = sr;
            }
        }

        private void Update() {
            if (phase == 0) return;
            elapsed += Time.deltaTime;
            if (phase == 1) {
                if (!snapSent && elapsed >= CloseSeconds) {
                    snapSent = true;
                    onSnap.Invoke();
                }

                if (elapsed >= CloseSeconds + SettleSeconds) {
                    phase = 2;
                    elapsed = 0;
                }
            }
            else if (phase == 2 && autoReopen && elapsed >= closedHoldSeconds) {
                Reopen();
            }
            else if (phase == 3 && elapsed >= OpenSeconds) {
                phase = 0;
                elapsed = 0;
                onReady.Invoke();
            }
        }

        private void LateUpdate() {
            VisibleGhostCount = 0;
            if (ghosts == null) return;
            for (var i = 0; i < 3; i++) {
                var oldTime = elapsed - (i + 1) * 0.018f;
                var fade = Mathf.Clamp01(1f - (elapsed - CloseSeconds) / 0.06f);
                var alpha = phase == 1 && oldTime > 0 ? GhostAlpha[i] * fade : 0;
                var progress = Mathf.Pow(Mathf.Clamp01(oldTime / CloseSeconds), 3);
                var blend = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.6f, 1, progress));
                ghosts[i, 0].transform.localRotation = Quaternion.Euler(0, 0, -77 * progress);
                ghosts[i, 1].transform.localRotation = Quaternion.Euler(0, 0, 77 * progress);
                ghosts[i, 0].color = new Color(1, 1, 1, alpha * (1 - blend));
                ghosts[i, 1].color = new Color(1, 1, 1, alpha * (1 - blend));
                ghosts[i, 2].color = new Color(1, 1, 1, alpha * blend);
                if (alpha > 0.001f) VisibleGhostCount++;
            }
        }

        private void OnDisable() {
            if (ghosts == null) return;
            for (var i = 0; i < 3; i++)
            for (var j = 0; j < 3; j++)
                if (ghosts[i, j])
                    ghosts[i, j].color = new Color(1, 1, 1, 0);
        }

        private void OnTriggerEnter2D(Collider2D other) {
            if (closeOnContact && (activationLayers.value & (1 << other.gameObject.layer)) != 0)
                Close();
        }

        public void Close() {
            if (!enabled || !IsReady) return;
            phase = 1;
            elapsed = 0;
            snapSent = false;
            animator.Play("Close", 0, 0);
        }

        public void Reopen() {
            if (!enabled || phase != 2) return;
            phase = 3;
            elapsed = 0;
            animator.Play("Open", 0, 0);
        }
    }
}