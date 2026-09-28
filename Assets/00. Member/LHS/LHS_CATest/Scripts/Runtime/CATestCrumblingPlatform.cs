using _02._Script._01_Players;
using _02._Script._05_Managers;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LHS_CATest {
    /// <summary>
    /// 밟으면 잠시 흔들리다가 무너지는 발판(무너지는 다리의 한 칸).
    /// 위에서 밟았을 때만 반응하도록 플레이어 발 위치가 발판 윗면 근처인지 확인한다.
    /// 무너진 뒤 respawnTime 후 다시 생기며, 플레이어가 죽으면 즉시 원상복구된다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class CATestCrumblingPlatform : MonoBehaviour {
        [Tooltip("무너지기 시작할 때 효과음 Key (CATestAudioLibrary)")]
        [SerializeField] private string sfxKey = "crumble";
        [SerializeField] private Transform visual;
        [SerializeField] private float shakeTime = 0.55f;
        [SerializeField] private float fallTime = 0.8f;
        [SerializeField] private float respawnTime = 3.5f;
        [SerializeField] private ParticleSystem dust;

        private Collider2D _col;
        private SpriteRenderer[] _renderers;
        private Vector3 _visualBase;
        private bool _triggered;
        private int _version;

        private void Awake() {
            _col = GetComponent<Collider2D>();
            if (visual == null) visual = transform;
            _visualBase = visual.localPosition;
            _renderers = visual.GetComponentsInChildren<SpriteRenderer>(true);
        }

        private void OnEnable() => GameManager.OnRespawnReset += OnRespawn;
        private void OnDisable() => GameManager.OnRespawnReset -= OnRespawn;

        private UniTask OnRespawn() {
            Restore();
            return UniTask.CompletedTask;
        }

        private void OnCollisionEnter2D(Collision2D collision) {
            if (_triggered) return;
            var p = collision.collider.GetComponentInParent<Player>();
            if (p == null) return;
            // 플레이어가 발판 윗면보다 위에 있을 때만 (옆에서 부딪힌 것 무시)
            if (p.transform.position.y < _col.bounds.max.y + 0.2f) return;
            Crumble().Forget();
        }

        private async UniTaskVoid Crumble() {
            _triggered = true;
            var v = ++_version;
            if (dust != null) dust.Play(true);
            CATestAudio.PlaySfx(sfxKey, transform.position);
            var t = 0f;
            while (t < shakeTime) {
                await UniTask.Yield(PlayerLoopTiming.Update);
                if (v != _version || this == null) return;
                t += Time.deltaTime;
                var k = t / shakeTime;
                visual.localPosition = _visualBase + (Vector3)(Random.insideUnitCircle * 0.06f * (0.4f + k));
            }
            _col.enabled = false;
            t = 0f;
            while (t < fallTime) {
                await UniTask.Yield(PlayerLoopTiming.Update);
                if (v != _version || this == null) return;
                t += Time.deltaTime;
                var k = t / fallTime;
                visual.localPosition = _visualBase + Vector3.down * (k * k * 6f);
                visual.localRotation = Quaternion.Euler(0, 0, k * 25f * (_visualBase.x > 0 ? 1 : -1));
                SetAlpha(1f - k);
            }
            await UniTask.Delay(System.TimeSpan.FromSeconds(respawnTime));
            if (v != _version || this == null) return;
            Restore();
        }

        private void Restore() {
            _version++;
            _triggered = false;
            if (_col != null) _col.enabled = true;
            if (visual != null) {
                visual.localPosition = _visualBase;
                visual.localRotation = Quaternion.identity;
            }
            SetAlpha(1f);
        }

        private void SetAlpha(float a) {
            if (_renderers == null) return;
            foreach (var r in _renderers) {
                var c = r.color;
                c.a = a;
                r.color = c;
            }
        }
    }
}
