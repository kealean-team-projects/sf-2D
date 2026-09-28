using _02._Script._01_Players.FSM.MoveState;
using _02._Script._05_Managers;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LHS_CATest {
    /// <summary>
    /// 붙잡으면 부서지는 덩굴 벽. 플레이어가 이 면에 매달리면(ClimbState) crumbleDelay 뒤 등반 판정(EdgeCollider, ClimbWall 레이어)이 사라진다.
    /// 일반 등반 속도로는 시간 안에 못 올라가므로 "벽 등반 대쉬(벽에서 W + 점프)"로 빠르게 올라가야 하는 퍼즐.
    /// </summary>
    public sealed class CATestCrumbleClimb : MonoBehaviour {
        [SerializeField] private EdgeCollider2D climbEdge;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private SpriteRenderer[] fadeTargets;
        [SerializeField] private float crumbleDelay = 1.1f;
        [SerializeField] private float respawnTime = 3.5f;
        [SerializeField] private ParticleSystem debris;
        [SerializeField] private float grabRange = 1.2f;

        private bool _busy;
        private Vector3 _visualRest;

        private void Awake() {
            if (visualRoot != null) _visualRest = visualRoot.localPosition;
        }

        private void OnEnable() => GameManager.OnRespawnReset += OnRespawn;
        private void OnDisable() => GameManager.OnRespawnReset -= OnRespawn;

        private UniTask OnRespawn() {
            Restore();
            _busy = false;
            return UniTask.CompletedTask;
        }

        private void Update() {
            if (_busy || climbEdge == null || !climbEdge.enabled) return;
            var player = CATestHUD.CurrentPlayer;
            if (player == null || !(player.CurrentMoveState is ClimbState)) return;
            var b = climbEdge.bounds;
            var p = player.transform.position;
            if (p.y < b.min.y - 1f || p.y > b.max.y + 1f || Mathf.Abs(p.x - b.center.x) > grabRange) return;
            Crumble().Forget();
        }

        private async UniTaskVoid Crumble() {
            _busy = true;
            var t = 0f;
            while (t < crumbleDelay) {
                if (this == null) return;
                t += Time.deltaTime;
                var k = t / crumbleDelay;
                if (visualRoot != null) visualRoot.localPosition = _visualRest + new Vector3(Mathf.Sin(t * 55f) * 0.06f * (0.3f + k), 0f, 0f);
                SetAlpha(1f - k * 0.35f);
                await UniTask.Yield();
            }
            if (this == null) return; // 마지막 Yield 사이에 씬이 언로드됐을 수 있음
            climbEdge.enabled = false;
            if (debris != null) debris.Play(true);
            SetAlpha(0.12f);
            await UniTask.Delay(System.TimeSpan.FromSeconds(respawnTime));
            if (this == null) return;
            Restore();
            _busy = false;
        }

        private void Restore() {
            if (climbEdge != null) climbEdge.enabled = true;
            if (visualRoot != null) visualRoot.localPosition = _visualRest;
            SetAlpha(1f);
        }

        private void SetAlpha(float a) {
            if (fadeTargets == null) return;
            foreach (var sr in fadeTargets) {
                if (sr == null) continue;
                var c = sr.color;
                c.a = a;
                sr.color = c;
            }
        }
    }
}
