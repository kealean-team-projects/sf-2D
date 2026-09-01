using System;
using _01.Script.Player.Interface;
using Cysharp.Threading.Tasks;
using UnityEngine;

public enum MotionType {
    ManualMove,
    Dash,
    Fall,
    Climb,
    WallJump,
    WallDash
}

namespace _01.Script.Player.Components {
    public class Mover : MonoBehaviour, IAgentModule, IMover {
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private float speed;
        [SerializeField] private float jumpForce;

        [SerializeField] private Vector3 checker;
        [SerializeField] private Vector2 checkerSize;
        [SerializeField] private LayerMask whatIsGround;

        [Header("ExtraGravity Settings")] 
        [SerializeField] private float extraGravity = 15f;

        [SerializeField] private float gravityDelay = 0.15f;

        [Header("Climb Settings")] 
        [SerializeField] private float climbUpSpeed = 5f;
    
        [SerializeField] private float climbDownSpeed = 18f;
        [SerializeField] private float wallDashSpeed;


        [Header("WallJump Settings")] [SerializeField]
        private float wallJumpXForce = 8f;

        [SerializeField] private float wallJumpYForce = 12f;

        [SerializeField] private float wallJumpDuration = 0.2f;
        private float _moveInput;

        [Header("CheckWall Settings")] private float _originGravityScale;

        private float _timeInAir;
        private UniTask _wallJump;

        private float _wallJumpDir;
        private MotionType type = MotionType.ManualMove;
        private bool _climbable = true;

        private void Awake() {
            _originGravityScale = rb.gravityScale;
        }

        private void Reset() {
            rb = transform.root.GetComponent<Rigidbody2D>();
            _originGravityScale = rb.gravityScale;
        }

        private void FixedUpdate() {
            IsGround = CheckGround();

            switch (type) {
                case MotionType.ManualMove:
                    rb.gravityScale = _originGravityScale;
                    rb.linearVelocityX = _moveInput * speed;
                    ApplyExtraGravity();
                    break;

                case MotionType.Climb:
                    ApplyClimb();
                    break;

                case MotionType.WallJump:
                    ApplyWallJump();
                    break;

                case MotionType.WallDash:
                    ApplyWallDash();
                    break;
                case MotionType.Dash:
                case MotionType.Fall:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ApplyWallDash() {
            WallDashUniTask().Forget();
        }

        private async UniTaskVoid WallDashUniTask() {
            
            rb.AddForceY(wallDashSpeed, ForceMode2D.Impulse);
            await UniTask.Delay(TimeSpan.FromSeconds(0.2f));

            _climbable = true;
            ChangeMotion(MotionType.Climb);
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

        [Header("CheckGround")]
        [field: SerializeField]
        public bool IsGround { get; private set; }

        public void SetMoveInput(float moveInput) {
            _moveInput = moveInput;
        }

        public void Jump(float multiplier = 1) {
            _timeInAir = 0;
            StopImmediately(false, true);
            rb.AddForceY(jumpForce * multiplier, ForceMode2D.Impulse);
        }

        public void WallJump(float xDirection) {
            if (!_wallJump.Status.IsCompleted())
                return;

            _wallJumpDir = xDirection;
            ChangeMotion(MotionType.WallJump);
        }

        public void WallDash() {
            _climbable = false;
            ChangeMotion(MotionType.WallDash);
        }

        public void CancelClimb() {
            _climbable = false;
            ChangeMotion(MotionType.ManualMove);
            CancelClimbUniTask().Forget();
        }

        private async UniTaskVoid CancelClimbUniTask() {
            await UniTask.Delay(TimeSpan.FromSeconds(2f));
            _climbable = true;
        }

        private void ApplyWallJump() {
            if (!_wallJump.Status.IsCompleted())
                return;

            _timeInAir = 0f;
            rb.gravityScale = _originGravityScale;

            var x = _wallJumpDir * wallJumpXForce;

            rb.linearVelocity = new Vector2(x, wallJumpYForce);
            _wallJump = WallJumpUniTask();
        }

        private async UniTask WallJumpUniTask() {
            await UniTask.Delay(TimeSpan.FromSeconds(wallJumpDuration));

            ChangeMotion(MotionType.ManualMove);
        }

        public void CalculateAirTime(ICheckClimbWall checkClimbWall) {
            if (checkClimbWall.IsClimbed) return;
            if (!IsGround) {
                _timeInAir += Time.deltaTime;
            }
            else {
                _timeInAir = 0;
                ChangeMotion(MotionType.ManualMove);
            }
        }


        private void ApplyExtraGravity() {
            if (_timeInAir > gravityDelay)
                rb.AddForceY(-extraGravity);
        }

        public void Climb(ICheckClimbWall check) {
            if(!_climbable) return;
            if (type == MotionType.WallJump)
                return;

            if (!check.IsClimbed) {
                ChangeMotion(MotionType.ManualMove);
                return;
            }

            ChangeMotion(MotionType.Climb);
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

        private void ChangeMotion(MotionType motion) {
            type = motion;
        }

        #region Climb Settings

        private float _climbInput;

        public void ClimbInput(float climbInput) {
            _climbInput = climbInput;
        }

        private void ApplyClimb() {
            rb.gravityScale = 0f;

            var climbSpeed = 0f;

            if (_climbInput != 0)
                climbSpeed = _climbInput > 0f ? climbUpSpeed : climbDownSpeed;

            rb.linearVelocity = new Vector2(0f, _climbInput * climbSpeed);
        }

        #endregion
    }
}