using _01.Script.Player.Interface;
using UnityEngine;

namespace _01.Script.Player {
    public class Player : Agent {
        private bool _canDoubleJump;
        private IInputReader _inputReader;
        private IInteractor _interactor;
        private IMover _mover;
        private bool IsGrounded => _mover.IsGround;
        private bool _isSprint = false;
        private bool _isCrouch = false;
        private CapsuleCollider2D _collider;

        private void FixedUpdate() {
            _mover.SetMoveInput(_inputReader.MoveInput * (_isSprint ? 2f : 1f) * (_isCrouch ? 0.5f : 1f));
        }

        protected override void Afterinitialize() {
            base.Afterinitialize();
            _inputReader = GetModule<IInputReader>();
            _mover = GetModule<IMover>();
            _interactor = GetModule<IInteractor>();
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

        private void HandleJumpInput() 
        {
            if (IsGrounded)
            {
                _mover.Jump();
                _canDoubleJump = true;
            }
            
            else if (_canDoubleJump)
            {
                _mover.Jump(1.3f);
                _canDoubleJump = false;
            }
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
    }
}