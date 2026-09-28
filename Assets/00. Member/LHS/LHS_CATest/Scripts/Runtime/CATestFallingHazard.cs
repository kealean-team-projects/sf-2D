using _02._Script._01_Players;
using _02._Script._01_Players.Components.DamageCompo;
using _02._Script._05_Managers;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LHS_CATest {
    /// <summary>
    /// 떨어지는 위험물(나뭇가지/바위/종유석). 플레이어가 trigger 영역에 들어오면
    ///  흔들림(예고) → 낙하 → 바닥에서 부서짐 → 잠시 뒤 제자리 복구.
    /// 낙하 중에만 판정이 있다(바닥에 떨어진 뒤에는 안전).
    /// </summary>
    public sealed class CATestFallingHazard : MonoBehaviour {
        public Rect triggerArea = new(0, 0, 6, 8);
        [SerializeField] private Transform hazard;
        [SerializeField] private float hitRadius = 0.7f;
        [SerializeField] private float warnTime = 0.55f;
        [SerializeField] private float gravity = 40f;
        [SerializeField] private float groundY;
        [SerializeField] private float respawnTime = 3f;
        [SerializeField] private ParticleSystem warnDust;
        [SerializeField] private ParticleSystem shatter;

        private Vector3 _home;
        private bool _busy;

        private void Awake() {
            if (hazard != null) _home = hazard.position;
        }

        private void OnEnable() => GameManager.OnRespawnReset += OnRespawn;
        private void OnDisable() => GameManager.OnRespawnReset -= OnRespawn;

        private UniTask OnRespawn() {
            if (hazard != null) {
                hazard.position = _home;
                hazard.gameObject.SetActive(true);
            }
            _busy = false;
            return UniTask.CompletedTask;
        }

        private void Update() {
            if (_busy || hazard == null) return;
            var p = CATestHUD.PlayerTransform;
            if (p != null && triggerArea.Contains(p.position)) Run().Forget();
        }

        private async UniTaskVoid Run() {
            _busy = true;
            if (warnDust != null) {
                warnDust.transform.position = _home;
                warnDust.Play(true);
            }
            var t = 0f;
            while (t < warnTime) {
                if (this == null || hazard == null) return;
                t += Time.deltaTime;
                hazard.position = _home + new Vector3(Mathf.Sin(t * 60f) * 0.08f, 0f, 0f);
                await UniTask.Yield();
            }
            var v = 0f;
            // 주의: await 뒤에는 씬이 언로드(사망 → 다른 씬 체크포인트로 부활)되어 이 오브젝트가 파괴됐을 수 있다.
            // while 조건에서 hazard.position 을 먼저 읽으면 MissingReferenceException 이 나므로 파괴 여부부터 확인한다.
            while (this != null && hazard != null && hazard.position.y > groundY) {
                v += gravity * Time.deltaTime;
                hazard.position += Vector3.down * v * Time.deltaTime;
                var hit = Physics2D.OverlapCircle(hazard.position, hitRadius, 1 << 7);
                if (hit != null) {
                    var player = hit.GetComponentInParent<Player>();
                    if (player != null && !player.IsDead && player.TryGetComponent(out DamageModule dmg)) dmg.TakeDamage();
                }
                await UniTask.Yield();
            }
            if (this == null || hazard == null) return;
            if (shatter != null) {
                shatter.transform.position = new Vector3(hazard.position.x, groundY + 0.3f, hazard.position.z - 0.3f);
                shatter.Play(true);
            }
            hazard.gameObject.SetActive(false);
            await UniTask.Delay(System.TimeSpan.FromSeconds(respawnTime));
            if (this == null || hazard == null) return;
            hazard.position = _home;
            hazard.gameObject.SetActive(true);
            _busy = false;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos() {
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.5f);
            Gizmos.DrawWireCube(triggerArea.center, triggerArea.size);
            if (hazard != null) Gizmos.DrawLine(hazard.position, new Vector3(hazard.position.x, groundY));
        }
#endif
    }
}
