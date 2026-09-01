using _01.Script.Player.Components;
using _01.Script.Player.Interface;
using UnityEngine;

namespace _01.Script.Player {
    public class Player : Agent {
        [SerializeField] private float useStaminaInRun;
        [SerializeField] private float useStaminaInWall;
        [SerializeField] private float useStaminaInWallDash;
        [SerializeField] private float useStaminaInWallJump;
        
        private bool _canDoubleJump;
        private CapsuleCollider2D _collider;
        private IInputReader _inputReader;
        private IInteractor _interactor;
        private bool _isCrouch;
        private bool _isSprint;
        private IMover _mover;
        private bool IsGrounded => _mover.IsGround;
        private IStats _stats;
        private ICheckClimbWall _checkClimbWall;

        private void Update() {
            FlipCheck();
            _mover.Climb(_checkClimbWall);
            _mover.CalculateAirTime(_checkClimbWall);
            _stats.StaminaUpdate(IsGrounded, _inputReader.MoveInput != 0);
            if (_isSprint) {
                _stats.UseStamina(useStaminaInRun);
            }

            if (_checkClimbWall.IsClimbed) {
                _stats.UseStamina(useStaminaInWall);
            }
        }

        private void FixedUpdate() {
            _mover.SetMoveInput(_inputReader.MoveInput * (_isSprint ? 2f : 1f) * (_isCrouch ? 0.5f : 1f));
            _mover.ClimbInput(_inputReader.ClimbInput);
        }

        protected override void AfterInitialize() {
            base.AfterInitialize();
            _inputReader = GetModule<IInputReader>();
            _mover = GetModule<IMover>();
            _interactor = GetModule<IInteractor>();
            _stats = GetModule<IStats>();
            _checkClimbWall = GetModule<ICheckClimbWall>();
            _collider = GetComponent<CapsuleCollider2D>();
            _inputReader.OnJumpPressed += HandleJumpInput;
            _inputReader.OnInteractPressed += HandleInteractInput;
            _inputReader.OnSprintPressed += HandleSprintInput;
            _inputReader.OnSprintReleased += HandleSprintRelease;
            _inputReader.OnCrouchPressed += HandleCrouchPressed;
            _inputReader.OnCrouchReleased += HandleCrouchRelease;
        }

        private void HandleCrouchRelease() {
            _collider.offset = Vector2.zero;
            _collider.size = new Vector2(1, 2);
            _isCrouch = false;
        }

        private void HandleCrouchPressed() {
            _collider.size = new Vector2(1, 1);
            _collider.offset = new Vector2(0, -0.5f);
            _isCrouch = true;
        }

        private void HandleSprintRelease() {
            _isSprint = false;
        }

        private void HandleSprintInput() {
            _isSprint = true;
        }

        private void HandleJumpInput() {
            if (_checkClimbWall.IsClimbed) {
                if (_inputReader.MoveInput != 0f) {
                    _mover.WallJump(IsFlipX ? 1f : -1f);
                    _stats.UseStamina(useStaminaInWallJump);
                }
                else if(_inputReader.ClimbInput > 0f) {
                    _mover.WallDash();
                    _stats.UseStamina(useStaminaInWallDash);
                }
                else if (_inputReader.ClimbInput < 0f)
                    _mover.CancelClimb();
                return;
            }

            if (!IsGrounded) return;
            _mover.Jump();
            _canDoubleJump = true;
        }

        protected override void OnDispose() {
            base.OnDispose();
            _inputReader.OnJumpPressed -= HandleJumpInput;
            _inputReader.OnInteractPressed -= HandleInteractInput;
            _inputReader.OnSprintPressed -= HandleSprintInput;
            _inputReader.OnSprintReleased -= HandleSprintRelease;
        }

        private void HandleInteractInput() {
            _interactor.Interact(this);
        }

        #region FlipController

        public bool IsFlipX { get; private set; }

        private void FlipCheck() {
            if (_checkClimbWall.IsClimbed) return;
            if (_inputReader.MoveInput == 0) return;

            var checkFlipX = _inputReader.MoveInput < 0;

            if (checkFlipX == IsFlipX) return;
            IsFlipX = checkFlipX;
            FlipX();
        }

        private void FlipX() {
            var targetRot = IsFlipX ? 180f : 0f;
            transform.rotation = Quaternion.Euler(0f, targetRot, 0f);
        }

        #endregion
    }
}