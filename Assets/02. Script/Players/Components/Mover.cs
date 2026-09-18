using System;
using _02._Script.Players.Interface;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _02._Script.Players.Components {
    public class Mover : MonoBehaviour, IAgentModule, IMover {
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private float jumpForce;

        [SerializeField] private Vector3 checker;
        [SerializeField] private Vector2 checkerSize;
        [SerializeField] private LayerMask whatIsGround;

        [Header("ExtraGravity Settings")] 
        [SerializeField] private float extraGravity = 15f;
        [SerializeField] private float gravityDelay = 0.15f;

        private float _originGravityScale;

        private float _timeInAir;
        
        private bool _isClimbCancelPending;
        
        private bool _isWallDashing;
        
        public bool CanClimb => !_isWallDashing && !_isClimbCancelPending;
        
        private void Awake() {
            _originGravityScale = rb.gravityScale;
        }

        private void Reset() {
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


        public void ApplyManualMove(float moveSpeed) {
            rb.gravityScale = _originGravityScale;
            rb.linearVelocityX = moveSpeed;
            ApplyExtraGravity();
        }

        [Header("CheckGround")]
        [field: SerializeField]
        public bool IsGround { get; private set; }

        public void Jump(float multiplier = 1) {
            _timeInAir = 0;
            StopImmediately(false, true);
            rb.AddForceY(jumpForce * multiplier, ForceMode2D.Impulse);
        }

        public void WallDash() {
            _isWallDashing = true;
        }

        public void CancelClimb() {
            if (_isClimbCancelPending) return;
            
            _isClimbCancelPending = true;
            CancelClimbUniTask().Forget();
        }

        public void CalculateAirTime(bool isClimbing) {
            if (isClimbing) return;
            if (!IsGround) {
                _timeInAir += Time.deltaTime;
            }
            else {
                _timeInAir = 0;
            }
        }

        public void ApplyWallDash(float impulse) {
            rb.AddForceY(impulse, ForceMode2D.Impulse);
        }

        public void EndWallDash() {
            _isWallDashing = false;
        }

        public void ApplyWallJump(Vector2 walljumpDir) {
            _timeInAir = 0f;
            rb.gravityScale = _originGravityScale;
            rb.linearVelocity = walljumpDir;
        }

        #region Climb Settings

        public void ApplyClimb(float climbSpeed) {
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(0f, climbSpeed);
        }

        #endregion

        public void PushForce(Vector2 pushDir, float power, ForceMode2D forceMode) {
            rb.AddForce(pushDir * power, forceMode);
        }

        private async UniTaskVoid CancelClimbUniTask() {
            await UniTask.Delay(TimeSpan.FromSeconds(2f));

            _isClimbCancelPending = false;
        }


        private void ApplyExtraGravity() {
            if (_timeInAir > gravityDelay)
                rb.AddForceY(-extraGravity);
        }


        private bool CheckGround() {
            var col = Physics2D.OverlapBox(transform.position + checker, checkerSize, 0f, whatIsGround);
            return col != null;
        }

        private void StopImmediately(bool isXStop, bool isYStop) {
            if (isXStop)
                rb.linearVelocityX = 0;
            if (isYStop)
                rb.linearVelocityY = 0;
        }
    }
}