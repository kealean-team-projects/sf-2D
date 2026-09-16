using _02._Script.FSM;
using _02._Script.FSM.MoveState;
using _02._Script.Players.Interface;
using _02._Script.Players.Sprint;
using _02._Script.UI;
using UnityEngine;

namespace _02._Script.Players {
    public class Player : Agent {
        [SerializeField] private float useStaminaInRun;
        [SerializeField] private float useStaminaInWall;
        [SerializeField] private float useStaminaInWallDash;
        [SerializeField] private float useStaminaInWallJump;
        [SerializeField] private StaminaHUD staminaHUD;

        private bool _canSJ = true;

        public bool CanSJ {
            get => _canSJ;
            set {
                _canSJ = value;
                if (!_canSJ && SprintControl != null) {
                    SprintControl.StopSprint();
                }
            }
        }

        private ICheckClimbWall _checkClimbWall;

        private IFacingController _facingController;

        private IInputReader _inputReader;
        private IInteractor _interactor;

        private MoveStateMachine _moveStateMachine;

        private IMover _mover;
        private IStats _stats;

        public ICrouchController CrouchControl { get; private set; }
        public SprintController SprintControl { get; private set; }
        public bool IsMoving => _inputReader.MoveInput != 0;
        public bool IsGrounded => _mover.IsGround;

        public bool IsClimb => _checkClimbWall.IsClimbed
                               && _mover.CanClimb
                               && _stats.Stamina > 0f;

        public bool IsWallJump => _moveStateMachine.CurrentState is WallJumpState;
        public bool IsWallDash => _moveStateMachine.CurrentState is WallDashState;

        public float MoveInput => _inputReader.MoveInput;

        public float ClimbInput => _inputReader.ClimbInput;

        private float pushSpeed;

        private void Update() {
            if (!IsClimb) _facingController.UpdateFacing(MoveInput);

            _mover.Climb(_checkClimbWall);
            _mover.CalculateAirTime(_checkClimbWall);

            _stats.StaminaUpdate(IsGrounded, IsMoving, IsClimb);
            SprintControl.Tick(IsMoving);

            if (IsClimb && ClimbInput != 0f)
                _stats.UseStamina(useStaminaInWall, false);

            if (_stats.Stamina <= 0f) _mover.CancelClimb();
        }

        private void FixedUpdate() {
            _moveStateMachine.Tick();
        }

        private void LateUpdate() {
            staminaHUD?.UpdateStamina(_stats.Stamina);
        }

        private void OnDestroy() { }

        protected override void AfterInitialize() {
            CanSJ = true;
            base.AfterInitialize();

            GetModules();
            SubscribeInputEvents();

            SprintControl = new SprintController(_stats, useStaminaInRun);
            _moveStateMachine = PlayerMoveStateFactory.Create(this);

            var viewerObject = new GameObject("StateMachineViewer");
            viewerObject.transform.SetParent(transform, false);

            var viewer = viewerObject.AddComponent<StateMachineViewer>();
            viewer.Initialize(_moveStateMachine);
        }

        protected override void OnDispose() {
            base.OnDispose();
            UnsubscribeInputEvents();
        }

        public float WalkSpeed => _moveStateMachine != null && _moveStateMachine.TryGetState<WalkState>(out var walkState)
            ? walkState.walkSpeed
            : 10f;

        public void ChangeSpeed(float speed) {
            if (_moveStateMachine != null && _moveStateMachine.TryGetState<WalkState>(out var walkState)) {
                walkState.walkSpeed = speed;
            }
        }

        #region ModulesGet

        private void GetModules() {
            _inputReader = GetModule<IInputReader>();
            _mover = GetModule<IMover>();
            _interactor = GetModule<IInteractor>();
            _stats = GetModule<IStats>();
            _checkClimbWall = GetModule<ICheckClimbWall>();
            CrouchControl = GetModule<ICrouchController>();
            _facingController = GetModule<IFacingController>();
        }

        #endregion

        public bool TryWallJump() {
            if (!CanStartWallAction(useStaminaInWallJump)) return false;

            _mover.WallJump(_facingController.IsFacingLeft ? 1f : -1f);
            _stats.UseStamina(useStaminaInWallJump, true);
            return true;
        }

        public bool TryWallDash() {
            if (!CanStartWallAction(useStaminaInWallDash)) return false;

            _mover.WallDash();
            _stats.UseStamina(useStaminaInWallDash, true);
            return true;
        }

        private bool CanStartWallAction(float staminaCost) {
            return IsClimb
                   && !IsWallJump
                   && !IsWallDash
                   && _stats.Stamina >= staminaCost;
        }

        #region Handle

        private void HandleCrouchRelease() {
            CrouchControl.Stand();
        }

        private void HandleCrouchPressed() {
            CrouchControl.Crouch();
        }

        private void HandleSprintInput() {
            if (!CanSJ) return;
            SprintControl.StartSprint();
        }

        private void HandleSprintRelease() {
            SprintControl.StopSprint();
        }

        private void HandleInteractInput() {
            _interactor.Interact(this);
        }

        private void HandleJumpInput() {
            if (!CanSJ) return;
            _moveStateMachine.HandleJumpInput();
        }

        #endregion

        #region SubScribe

        private void SubscribeInputEvents() {
            _inputReader.OnJumpPressed += HandleJumpInput;
            _inputReader.OnInteractPressed += HandleInteractInput;
            _inputReader.OnSprintPressed += HandleSprintInput;
            _inputReader.OnSprintReleased += HandleSprintRelease;
            _inputReader.OnCrouchPressed += HandleCrouchPressed;
            _inputReader.OnCrouchReleased += HandleCrouchRelease;
        }

        private void UnsubscribeInputEvents() {
            _inputReader.OnJumpPressed -= HandleJumpInput;
            _inputReader.OnInteractPressed -= HandleInteractInput;
            _inputReader.OnSprintPressed -= HandleSprintInput;
            _inputReader.OnSprintReleased -= HandleSprintRelease;
            _inputReader.OnCrouchPressed -= HandleCrouchPressed;
            _inputReader.OnCrouchReleased -= HandleCrouchRelease;
        }

        #endregion

        #region ApplyMover

        public void ApplyManualMove(float input) {
            _mover.ApplyManualMove(input * SprintControl.MoveSpeedMultiplier
                                         * CrouchControl.MoveSpeedMultiplier + pushSpeed);
        }

        public void ApplyClimb(float climbSpeed) {
            _mover.ApplyClimb(climbSpeed);
        }

        public void ApplyWallJump(float xSpeed, float ySpeed) {
            _mover.ApplyWallJump(xSpeed, ySpeed);
        }

        public void ApplyWallDash(float impulse) {
            _mover.ApplyWallDash(impulse);
        }

        public void EndWallDash() {
            _mover.EndWallDash();
        }

        public void Jump() {
            if (!CanSJ) return;
            _mover.Jump();
        }

        public void CancelClimb() {
            _mover.CancelClimb();
        }

        public void SetPushSpeed(float speed)
        {
            pushSpeed = speed;
        }

        #endregion
    }
}