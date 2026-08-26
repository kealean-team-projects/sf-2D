using _01.Script.Player.Interface;

namespace _01.Script.Player {
    public class Player : Agent {
        public bool IsGrounded;
        private bool _canDoubleJump;
        private IInputReader _inputReader;
        private IInteractor _interactor;
        private IMover _mover;

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

        private void HandleJumpInput() {
            _mover.Jump();

            // 이단 점프할 방법 근데 IMover인데 어케 IsGround 가져오지
            //근데 왜 isGround를 Player에서 이건 Mover로 가야지
            //if (IsGrounded)
            //{
            //    _mover.Jump();
            //    _canDoubleJump = true;
            //}
            //
            //if (_canDoubleJump)
            //{
            //    _mover.Jump();
            //    _canDoubleJump = false;
            //}
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