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

        private void Awake() {
            rb = transform.root.GetComponent<Rigidbody2D>();
            _originGravityScale = rb.gravityScale;
        }

        private void FixedUpdate() {
            IsGround = CheckGround();
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

        public void RestorePosition(Vector2 position) {
            _restoreVersion++;

            _isClimbCancelPending = false;
            _isWallDashing = false;
            _timeInAir = 0f;

            rb.position = position;
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = _originGravityScale;

            Physics2D.SyncTransforms();
            IsGround = CheckGround();
        }

        #region Apply_States

        public void ApplyManualMove(float moveSpeed) {
            rb.gravityScale = _originGravityScale;
            rb.linearVelocityX = moveSpeed;
            ApplyExtraGravity();
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

        #endregion"

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

        private bool CheckGround() {
            return Physics2D.OverlapBox(transform.position + checker, checkerSize, 0f, whatIsGround) != null;
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