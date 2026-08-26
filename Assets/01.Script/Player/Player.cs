using _01.Script.Player.Interface;

namespace _01.Script.Player {
    public class Player : Agent {
        private bool _canDoubleJump;
        private IInputReader _inputReader;
        private IInteractor _interactor;
        private IMover _mover;
        private bool IsGrounded => _mover.IsGround;

        private void FixedUpdate() {
            _mover.SetMoveInput(_inputReader.MoveInput);
        }

        protected override void Afterinitialize() {
            base.Afterinitialize();
            _inputReader = GetModule<IInputReader>();
            _mover = GetModule<IMover>();
            _interactor = GetModule<IInteractor>();
            _inputReader.OnJumpPressed += HandleJumpInput;
            _inputReader.OnInteractPressed += HandleInteractInput;
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
        }

        private void HandleInteractInput() {
            _interactor.Interact(this);
        }
    }
}