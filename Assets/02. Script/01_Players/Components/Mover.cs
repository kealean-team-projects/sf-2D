using System;
using _02._Script._01_Players.Interface;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _02._Script._01_Players.Components {
    public class Mover : MonoBehaviour, IAgentModule, IMover {
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private float jumpForce;
        [SerializeField] private Vector3 checker;
        [SerializeField] private Vector2 checkerSize;
        [SerializeField] private LayerMask whatIsGround;

        [Header("ExtraGravity Settings")] [SerializeField]
        private float extraGravity = 15f;

        [SerializeField] private float gravityDelay = 0.15f;

        private bool _isClimbCancelPending;
        private bool _isWallDashing;

        private float _originGravityScale;

        private int _restoreVersion;
        private float _timeInAir;

        public event Action GroundUpdated;
        public int RestoreVersion => _restoreVersion;

        // Audio observes the supporting surface without changing movement's ground check.
        public Collider2D GroundCollider { get; private set; }
        private readonly RaycastHit2D[] _surfaceHits = new RaycastHit2D[8];

        private void Awake() {
            rb = transform.root.GetComponent<Rigidbody2D>();
            _originGravityScale = rb.gravityScale;
        }

        private void FixedUpdate()
        {
            GroundCollider = FindSupportingSurface();
            IsGround = GroundCollider != null;

            GroundUpdated?.Invoke();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected() {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position + checker, checkerSize);
            Gizmos.color = Color.yellow;
        }
#endif

        public Type Type => typeof(IMover);

        public void Initialize(Agent owner) { }

        [Header("CheckGround Settings")]
        [field: SerializeField]
        public bool IsGround { get; private set; }

        public bool CanClimb => !_isWallDashing && !_isClimbCancelPending;
        public float VerticalSpeed => rb.linearVelocityY;

        public void RestorePosition(Vector2 position) {
            _restoreVersion++;

            _isClimbCancelPending = false;
            _isWallDashing = false;
            _timeInAir = 0f;

            rb.position = position;
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = _originGravityScale;

            Physics2D.SyncTransforms();
            GroundCollider = FindSupportingSurface();
            IsGround = GroundCollider != null;
        }

        #region Apply_States

        public void ApplyManualMove(float moveSpeed) {
            rb.gravityScale = _originGravityScale;
            rb.linearVelocityX = moveSpeed;
            ApplyExtraGravity();
        }

        public void ApplyManualMoveY(float moveSpeed) {
            if (Mathf.Approximately(moveSpeed, 0f)) return;
            // Preserve faster jumps/falls in the current's direction without accumulating speed.
            rb.linearVelocityY = moveSpeed > 0f
                ? Mathf.Max(rb.linearVelocityY, moveSpeed)
                : Mathf.Min(rb.linearVelocityY, moveSpeed);
        }

        public void Jump(float multiplier = 1) {
            _timeInAir = 0;
            StopImmediately(false, true);
            rb.AddForceY(jumpForce * multiplier, ForceMode2D.Impulse);
        }

        public void ApplyWallDash(float impulse) {
            rb.AddForceY(impulse, ForceMode2D.Impulse);
        }

        public void ApplyWallJump(Vector2 walljumpDir) {
            _timeInAir = 0f;
            rb.gravityScale = _originGravityScale;
            rb.linearVelocity = walljumpDir;
        }

        public void ApplyClimb(float climbSpeed) {
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(0f, climbSpeed);
        }

        #region WallDash Control

        public void WallDash() {
            _isWallDashing = true;
        }

        public void EndWallDash() {
            _isWallDashing = false;
        }

        #endregion

        #region Climb Control

        public void CancelClimb() {
            if (_isClimbCancelPending) return;

            _isClimbCancelPending = true;
            CancelClimbUniTask().Forget();
        }

        private async UniTaskVoid CancelClimbUniTask() {
            var version = _restoreVersion;

            await UniTask.Delay(TimeSpan.FromSeconds(2f));

            if (version != _restoreVersion) return;

            _isClimbCancelPending = false;
        }

        #endregion

        #endregion

        #region Rigidbody


        public Collider2D FindSoundSurface(float extraDistance) {
            // Only extend the sound query; the movement ground check is unchanged.
            return extraDistance > 0f ? FindSupportingSurface(extraDistance) : GroundCollider;
        }

        private Collider2D FindSupportingSurface(float extraDistance = 0f) {
            var filter = new ContactFilter2D();
            filter.SetLayerMask(whatIsGround);
            filter.useTriggers = false;
            var center = (Vector2)(transform.position + checker);
            var origin = center + Vector2.up * (checkerSize.y * 0.5f + 0.05f);
            var distance = checkerSize.y + 0.1f + Mathf.Max(0f, extraDistance);
            // Prefer the surface directly under the feet, then either edge on a ledge.
            for (var sample = 0; sample < 3; sample++) {
                var offset = sample == 0 ? 0f : checkerSize.x * (sample == 1 ? -0.4f : 0.4f);
                var count = Physics2D.Raycast(origin + Vector2.right * offset, Vector2.down,
                    filter, _surfaceHits, distance);
                Collider2D closest = null;
                var closestDistance = float.PositiveInfinity;
                for (var i = 0; i < count; i++) {
                    var hit = _surfaceHits[i];
                    if (hit.fraction == 0f || hit.normal.y < 0.35f || hit.distance >= closestDistance)
                        continue;
                    closest = hit.collider;
                    closestDistance = hit.distance;
                }
                if (closest != null) return closest;
            }
            return null;
        }

        public void CalculateAirTime(bool isClimbing) {
            if (isClimbing) return;
            if (!IsGround)
                _timeInAir += Time.deltaTime;
            else
                _timeInAir = 0;
        }

        private void ApplyExtraGravity() {
            if (_timeInAir > gravityDelay)
                rb.AddForceY(-extraGravity);
        }

        private void StopImmediately(bool isXStop, bool isYStop) {
            if (isXStop)
                rb.linearVelocityX = 0;
            if (isYStop)
                rb.linearVelocityY = 0;
        }

        #endregion
    }
}
