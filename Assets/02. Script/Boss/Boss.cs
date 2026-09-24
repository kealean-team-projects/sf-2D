using System;
using System.Threading;
using _02._Script._01_Players.Components.DamageCompo;
using _02._Script._04_Interaction;
using _02._Script.Boss.BossPatterns;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _02._Script.Boss {
    [RequireComponent(typeof(Rigidbody2D))]
    public class Boss : MonoBehaviour {
        [SerializeField] private BossPattern[] patterns;
        [SerializeField] private BossFOV fov;
        [SerializeField] private SpriteRenderer playerRenderer;
        [SerializeField] private Material detectedMaterial;

        private Material originalMaterial;

        private CancellationTokenSource patternCts;

        public Rigidbody2D RbCompo { get; private set; }
        public BossNavigation Navigation { get; set; }
        public Transform Target { get; private set; }
        public Vector2 ReturnPosition { get; private set; }

        public BossPattern CurrentPattern { get; private set; }

        private void Awake() {
            RbCompo = GetComponent<Rigidbody2D>();
            // Rigidbody2D owns movement; NavMesh is used only to calculate routes.
            if (TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var agent)) agent.enabled = false;
            
            if (playerRenderer != null)
                originalMaterial = playerRenderer.sharedMaterial;
            
            ShowDetection(false);
        }

        private void OnDisable() {
            Stop();
            ShowDetection(false);
        }

        private void OnTriggerEnter2D(Collider2D other) {
            if (CurrentPattern is not AttackPattern attack) return;
            if (!attack.IsAttacking) return;

            var light = other.GetComponentInParent<InteractLight>();
            if (light != null) {
                if (light.transform == Target) {
                    light.Break();
                    attack.Finish();
                }
                return;
            }

            if (other.isTrigger) return;
            var damage = other.GetComponentInParent<DamageModule>();

            if (damage != null && Target != null &&
                damage.transform.root == Target.root) {
                damage.TakeDamage();
                attack.Finish();
            }
        }

        public async UniTask RunPatterns(CancellationToken token) {
            if (patternCts != null || !isActiveAndEnabled)
                throw new InvalidOperationException("보스가 비활성화됐거나 이미 행동 중입니다.");
            if (patterns == null || patterns.Length == 0 || Array.Exists(patterns, p => p == null))
                throw new InvalidOperationException("보스의 Chase, Attack 패턴을 연결하세요.");

            patternCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            try {
                foreach (var pattern in patterns) {
                    patternCts.Token.ThrowIfCancellationRequested();
                    CurrentPattern = pattern;
                    await pattern.Execute(this, patternCts.Token);
                    patternCts.Token.ThrowIfCancellationRequested();
                    CurrentPattern = null;
                }
            }
            finally {
                CurrentPattern = null;
                Target = null;
                patternCts.Dispose();
                patternCts = null;
            }
        }


        public void SetTarget(Transform target) {
            Target = target;
        }

        public void SetReturnPosition(Vector2 position) {
            ReturnPosition = position;
        }

        public bool CanSee(Transform target) {
            return fov.CanSee(target);
        }

        public void ShowVision(bool visible) {
            fov.Show(visible);
        }

        public UniTask OpenVision(CancellationToken token) => fov.Open(token);
        

        public void ShowDetection(bool detected)
        {
            fov.SetDetected(detected);
            if (playerRenderer == null) return;
            if (detected && detectedMaterial == null) return;

            playerRenderer.sharedMaterial =
                detected ? detectedMaterial : originalMaterial;
        }


        public void Stop() {
            patternCts?.Cancel();
        }

        public void ResetForRetry(Vector3 position, Quaternion rotation)
        {
            SetTarget(null);
            ShowDetection(false);
            gameObject.SetActive(false);
            transform.SetPositionAndRotation(position, rotation);
            RbCompo.position = position;
            RbCompo.rotation = rotation.eulerAngles.z;
            RbCompo.linearVelocity = Vector2.zero;
            RbCompo.angularVelocity = 0f;
            SetReturnPosition(position);
            Navigation.ResetPath();
        }
        
        public UniTask CloseVision(CancellationToken token, Action scan = null)
        {
            return fov.Close(token, scan);
        }
    }
}
