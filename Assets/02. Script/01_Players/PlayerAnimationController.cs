using _02._Script._01_Players.FSM;
using _02._Script._01_Players.FSM.MoveState;
using UnityEngine;

namespace _02._Script._01_Players {
    public sealed class PlayerAnimationController : MonoBehaviour {
        private static readonly int MoveSpeedHash =
            Animator.StringToHash("MoveSpeed");

        private static readonly int VerticalSpeedHash =
            Animator.StringToHash("VerticalSpeed");

        private static readonly int ClimbSpeedHash =
            Animator.StringToHash("ClimbSpeed");

        private static readonly int IsGroundedHash =
            Animator.StringToHash("IsGrounded");

        private static readonly int IsClimbingHash =
            Animator.StringToHash("IsClimbing");

        private static readonly int IsCrouchingHash =
            Animator.StringToHash("IsCrouching");

        private static readonly int JumpHash =
            Animator.StringToHash("Jump");

        private static readonly int WallJumpHash =
            Animator.StringToHash("WallJump");

        private static readonly int WallDashHash =
            Animator.StringToHash("WallDash");

        [SerializeField] private Player player;
        [SerializeField] private Animator animator;


#if UNITY_EDITOR
        private void Reset() {
            player = transform.root.GetComponent<Player>();
            animator = GetComponent<Animator>();
        }
#endif

        private void Update() {
            if (player == null || animator == null)
                return;

            var state = player.CurrentMoveState;

            var moveSpeed = state switch {
                RunState => 2f,
                WalkState => 1f,

                CrouchState when player.IsMoving => 1f,

                _ => 0f
            };

            animator.SetFloat(MoveSpeedHash, moveSpeed);
            animator.SetFloat(VerticalSpeedHash, player.VerticalSpeed);
            animator.SetFloat(
                ClimbSpeedHash,
                Mathf.Abs(player.ClimbInput));

            animator.SetBool(
                IsGroundedHash,
                player.IsGrounded);

            animator.SetBool(
                IsClimbingHash,
                state is ClimbState);

            animator.SetBool(
                IsCrouchingHash,
                state is CrouchState);
        }

        private void OnEnable() {
            if (player != null)
                player.MoveStateChanged += HandleStateChanged;
        }

        private void OnDisable() {
            if (player != null)
                player.MoveStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(PlayerMoveState previousState, PlayerMoveState currentState) {
            if (animator == null)
                return;

            if (currentState is JumpState &&
                previousState is GroundState)
                animator.SetTrigger(JumpHash);

            if (currentState is WallJumpState) animator.SetTrigger(WallJumpHash);

            if (currentState is WallDashState) animator.SetTrigger(WallDashHash);
        }
    }
}