using _02._Script.Component;
using _02._Script.FSM;
using _02._Script.FSM.MoveState;
using _02._Script.Players.Interface;
using _02._Script.Players.Sprint;
using _02._Script.UI;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace _02._Script.Players {
    public class Player : Agent {
        [SerializeField] private float useStaminaInRun;
        [SerializeField] private float useStaminaInWall;
        [SerializeField] private float useStaminaInWallDash;
        [SerializeField] private float useStaminaInWallJump;
        [SerializeField] private StaminaHUD staminaHUD;
        [SerializeField] private Image fade;
        
        [Header("Walk Settings")]
        [SerializeField] private float moveSpeed = 10f;
        [SerializeField] private float moveSpeedMultiplier;
        
        [Header("Climb Settings")]
        [SerializeField] private float climbUpSpeed = 10f;
        [SerializeField] private float climbDownSpeed = 20f;
        [SerializeField] private float crouchSpeedMultiplier = 2f;
        
        [Header("Jump Settings")]
        [SerializeField] private float jumpXSpeed = 8f;
        [SerializeField] private float jumpYSpeed = 12f;
        [SerializeField] private float jumpDuration = 0.2f;
        [SerializeField] private float jumpDashImpulse = 3f;

        [Header("Push Settings")]
        [SerializeField] private float pushSpeed;
        
        private float jumpDir;

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

        // Components Settings

        #region Components

        private ICheckClimbWall _checkClimbWall;

        private IFacingController _facingController;

        private IInputReader _inputReader;
        private IInteractor _interactor;

        private MoveStateMachine _moveStateMachine;

        public IMover Mover { get; private set; }
        
        private IStats _stats;

        private DamageModule _damage;
        
        public ICrouchController CrouchControl { get; private set; }
        public SprintController SprintControl { get; private set; }

        #endregion

        #region Property FSM Settings

        public float MoveInput => _inputReader.MoveInput * moveSpeed;
        public float ClimbInput => _inputReader.ClimbInput;
        
        public float ClimbSpeed { get; private set; }
        public Vector2 JumpSpeed  => new(jumpXSpeed * jumpDir, jumpYSpeed);
        public float JumpDuration => jumpDuration;
        public float Impulse => jumpDashImpulse;
        public float PushSpeed => pushSpeed;
        
        public float SpeedMultiplier => SprintControl.IsSprinting ? moveSpeedMultiplier : 1f;
        public float CrouchSpeedMultiplier => crouchSpeedMultiplier;

        #endregion

        #region State Settings

        public bool IsMoving => _inputReader.MoveInput != 0;
        public bool IsGrounded => Mover.IsGround;
        public bool IsClimb => _checkClimbWall.IsClimbed
                               && Mover.CanClimb
                               && _stats.Stamina > 0f;

        public bool IsWallJump => _moveStateMachine.CurrentState is WallJumpState;
        public bool IsWallDash => _moveStateMachine.CurrentState is WallDashState;

        #endregion

        private void Update() {
            if (!IsClimb) _facingController.UpdateFacing(MoveInput);

            Mover.Climb(_checkClimbWall);
            Mover.CalculateAirTime(_checkClimbWall);

            _stats.StaminaUpdate(IsGrounded, IsMoving, IsClimb);
            SprintControl.Tick(IsMoving);

            if (IsClimb && ClimbInput != 0f)
                _stats.UseStamina(useStaminaInWall, false);

            if (_stats.Stamina <= 0f) Mover.CancelClimb();
            
            ClimbSpeed = ClimbInput > 0f ? climbUpSpeed : climbDownSpeed;
            jumpDir = _facingController.IsFacingLeft ? 1f : -1f;
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

            _damage = GetComponent<DamageModule>();
            SprintControl = new SprintController(_stats, useStaminaInRun);
            _moveStateMachine = PlayerMoveStateFactory.Create(this);

            var viewerObject = new GameObject("StateMachineViewer");
            viewerObject.transform.SetParent(transform, false);

            var viewer = viewerObject.AddComponent<StateMachineViewer>();
            viewer.Initialize(_moveStateMachine);

            _damage.OnDamaged += OnDead;
        }

        protected override void OnDispose() {
            base.OnDispose();
            _damage.OnDamaged -= OnDead;
            UnsubscribeInputEvents();
        }

        public void ChangeSpeed(float speed) {
            if (_moveStateMachine != null && _moveStateMachine.TryGetState<WalkState>(out var walkState)) {
                moveSpeed = speed;
            }
        }

        #region DeadHandler

        private bool _isDead;
        
        private void OnDead() {
            
            if (_isDead) return;

            _isDead = true;
            
            DeadUni().Forget();
        }

        private async UniTaskVoid DeadUni() {
            await Tween.Alpha(fade, 0, 1, 1, Ease.InExpo);
            Destroy(gameObject);
        }

        #endregion
        
        #region ModulesGet

        private void GetModules() {
            _inputReader = GetModule<IInputReader>();
            Mover = GetModule<IMover>();
            _interactor = GetModule<IInteractor>();
            _stats = GetModule<IStats>();
            _checkClimbWall = GetModule<ICheckClimbWall>();
            CrouchControl = GetModule<ICrouchController>();
            _facingController = GetModule<IFacingController>();
        }

        #endregion

        public bool TryWallJump() {
            if (!CanStartWallAction(useStaminaInWallJump)) return false;

            Mover.WallJump(_facingController.IsFacingLeft ? 1f : -1f);
            _stats.UseStamina(useStaminaInWallJump, true);
            return true;
        }

        public bool TryWallDash() {
            if (!CanStartWallAction(useStaminaInWallDash)) return false;

            Mover.WallDash();
            _stats.UseStamina(useStaminaInWallDash, true);
            return true;
        }

        private bool CanStartWallAction(float staminaCost) {
            return IsClimb && !IsWallJump
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
        

        public void EndWallDash() {
            Mover.EndWallDash();
        }

        public void Jump() {
            if (!CanSJ) return;
            Mover.Jump();
        }

        public void CancelClimb() {
            Mover.CancelClimb();
        }

        public void SetPushSpeed(float speed)
        {
            pushSpeed = speed;
        }

        #endregion
    }
}