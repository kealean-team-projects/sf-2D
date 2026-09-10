using System;
using _02._Script.Players.Interface;
using _02._Script.Players.MoveState;
using _02._Script.Players.Sprint;
using _02._Script.UI;
using PrimeTween;
using UnityEngine;

namespace _02._Script.Players {
    public class Player : Agent, IPlayerMoveContext {
        [SerializeField] private float useStaminaInRun;
        [SerializeField] private float useStaminaInWall;
        [SerializeField] private float useStaminaInWallDash;
        [SerializeField] private float useStaminaInWallJump;
        [SerializeField] private StaminaHUD staminaHUD;

        private ICheckClimbWall _checkClimbWall;

        private ICrouchController _crouchController;
        private IFacingController _facingController;

        private IInputReader _inputReader;
        private IInteractor _interactor;

        //private MoveStateMachine _moveStateMachine;

        private IMover _mover;
        private SprintController _sprintController;
        private IStats _stats;
        private bool IsGrounded => _mover.IsGround;

        public bool canJump = true;

        private void Update() {
            var isClimbing = _checkClimbWall.IsClimbed;
            var isMoving = _inputReader.MoveInput != 0f;

            if (!isClimbing)
                _facingController.UpdateFacing(_inputReader.MoveInput);

            _mover.Climb(_checkClimbWall);
            _mover.CalculateAirTime(_checkClimbWall);

            _stats.StaminaUpdate(IsGrounded, isMoving, isClimbing);
            _sprintController.Tick(isMoving);

            if (isClimbing && _inputReader.ClimbInput != 0f)
                _stats.UseStamina(useStaminaInWall, false);

            if (_stats.Stamina <= 0) _mover.CancelClimb();
        }

        private void FixedUpdate() {
            //if (IsGrounded && !_checkClimbWall.IsClimbed)
                //_moveStateMachine.Tick();


                ApplyMoveInput(MoveInput);
            
                if (_checkClimbWall.IsClimbed)
                    _mover.ClimbInput(_inputReader.ClimbInput);
        }

        private void LateUpdate() {
            staminaHUD?.UpdateStamina(_stats.Stamina);
        }

        private void OnDestroy() { }

        public float MoveInput => _inputReader.MoveInput;

        public void ApplyMoveInput(float input) {
            _mover.SetMoveInput(
                input * _sprintController.MoveSpeedMultiplier
                      * _crouchController.MoveSpeedMultiplier
            );
        }

        protected override void AfterInitialize() {
            canJump = true;
            base.AfterInitialize();

            GetModules();
            SubscribeInputEvents();

            _sprintController = new SprintController(_stats, useStaminaInRun);
            //_moveStateMachine = PlayerMoveStateFactory.Create(this);
        }

        private void HandleCrouchRelease() {
            _crouchController.Stand();
        }

        private void HandleCrouchPressed() {
            _crouchController.Crouch();
        }


        private void HandleSprintInput() {
            _sprintController.StartSprint();
        }

        private void HandleSprintRelease() {
            _sprintController.StopSprint();
        }

        private void HandleJumpInput() {
            if (!canJump) return;
            if (_checkClimbWall.IsClimbed) {
                if (_inputReader.MoveInput != 0f) {
                    if (_stats.Stamina < useStaminaInWallJump) return;
                    _mover.WallJump(_facingController.IsFacingLeft ? 1f : -1f);
                    _stats.UseStamina(useStaminaInWallJump, true);
                }
                else {
                    switch (_inputReader.ClimbInput) {
                        case > 0f:
                            if (_stats.Stamina < useStaminaInWallDash) return;
                            _mover.WallDash();
                            _stats.UseStamina(useStaminaInWallDash, true);
                            break;
                        case < 0f:
                            _mover.CancelClimb();
                            break;
                    }
                }

                return;
            }

            if (!IsGrounded) return;
            _mover.Jump();
        }

        protected override void OnDispose() {
            base.OnDispose();
            UnsubscribeInputEvents();
        }

        private void HandleInteractInput() {
            _interactor.Interact(this);
        }

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

        public void ChangeSpeed(float speed) {
            _mover.SpeedControl(speed);
        }
        
        private void GetModules() {
            _inputReader = GetModule<IInputReader>();
            _mover = GetModule<IMover>();
            _interactor = GetModule<IInteractor>();
            _stats = GetModule<IStats>();
            _checkClimbWall = GetModule<ICheckClimbWall>();
            _crouchController = GetModule<ICrouchController>();
            _facingController = GetModule<IFacingController>();
        }
    }
}
