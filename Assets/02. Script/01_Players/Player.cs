using _02._Script._01_Players.Components.ControllerCompo;
using _02._Script._01_Players.Components.DamageCompo;
using _02._Script._01_Players.Components.Stamina;
using _02._Script._01_Players.FSM;
using _02._Script._01_Players.FSM.MoveState;
using _02._Script._01_Players.Interface;
using _02._Script._05_Managers;
using _02._Script.UI;
using UnityEngine;
using UnityEngine.UI;

namespace _02._Script._01_Players {
    public class Player : Agent, IPlayerMoveContext {
        [SerializeField] private StaminaCosts staminaCosts = new();
        [SerializeField] private StaminaHUD staminaHUD;
        [SerializeField] private Image fade;

        [Header("Walk Settings")] [SerializeField]
        private float moveSpeed = 10f;

        [SerializeField] private float moveSpeedMultiplier;

        [Header("Climb Settings")] [SerializeField]
        private float climbUpSpeed = 10f;

        [SerializeField] private float climbDownSpeed = 20f;

        [Header("WallJump Settings")] [SerializeField]
        private float jumpXSpeed = 8f;

        [SerializeField] private float jumpYSpeed = 12f;
        [SerializeField] private float jumpDuration = 0.2f;
        [SerializeField] private float jumpDashImpulse = 3f;

        [Header("Crouch Settings")] [SerializeField]
        private float crouchSpeedMultiplier = 0.5f;

        [SerializeField] private PlayerProgress progress;

        private bool _canSJ = true;
        private float jumpDir;

        private void Update() {
            if (IsDead) return;
            if (!IsClimb) _facingController.UpdateFacing(MoveInput);

            Mover.CalculateAirTime(IsClimb);

            _stats.StaminaUpdate(IsGrounded, IsMoving, IsClimb);
            SprintControl.Tick(IsMoving);

            ClimbSpeed = ClimbInput > 0f ? climbUpSpeed : climbDownSpeed;
            jumpDir = _facingController.IsFacingLeft ? 1f : -1f;
            if (_isHeat)
                _stats.HeatStrokeUpdate(10, _damage);
            if (_isHighHeat)
                _stats.HeatStrokeUpdate(20, _damage);
            if (!_isHeat && !_isHighHeat)
                _stats.HeatStrokeUpdate(-20, _damage);
        }

        private void FixedUpdate() {
            if (IsDead) return;
            _moveStateMachine.Tick();
            if (CanMove && !IsClimb) Mover.ApplyManualMoveY(PushSpeed.y);
        }

        private void LateUpdate() {
            staminaHUD?.UpdateStamina(_stats.Stamina);
        }

        private void OnTriggerEnter2D(Collider2D other) {
            if (other.CompareTag("Heat")) _isHeat = true;

            if (other.CompareTag("HighHeat")) _isHighHeat = true;
        }

        private void OnTriggerExit2D(Collider2D other) {
            _isHeat = false;
            _isHighHeat = false;
        }

        public bool CanSJ {
            get => _canSJ;
            set {
                _canSJ = value;
                if (!_canSJ && SprintControl != null) SprintControl.StopSprint();
            }
        }

        protected override void AfterInitialize() {
            CanSJ = true;
            base.AfterInitialize();

            GetModules();
            CrouchControl.SetCrouchSpeedMultiplier(crouchSpeedMultiplier);
            SubscribeInputEvents();

            _damage = GetComponent<DamageModule>();
            SprintControl = new SprintController(_stats, staminaCosts.runPerSecond);
            _moveStateMachine = PlayerMoveStateFactory.Create(this, _stats);

            var viewerObject = new GameObject("StateMachineViewer");
            viewerObject.transform.SetParent(transform, false);

            var viewer = viewerObject.AddComponent<StateMachineViewer>();
            viewer.Initialize(_moveStateMachine);

            _damage.OnDamaged += OnDead;
            progress.Initialize(this);
        }

        protected override void OnDispose() {
            base.OnDispose();
            UnsubscribeInputEvents();

            _damage.OnDamaged -= OnDead;
            progress.Shutdown();
        }

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

        // Components Settings

        #region Components

        private IInputReader _inputReader;
        private MoveStateMachine _moveStateMachine;

        private IInteractor _interactor;
        private IFacingController _facingController;

        private IStats _stats;
        private ICheckClimbWall _checkClimbWall;
        private DamageModule _damage;


        public IMover Mover { get; private set; }
        public ICrouchController CrouchControl { get; private set; }
        public SprintController SprintControl { get; private set; }

        #endregion

        #region Property FSM Settings

        public float MoveInput => CanMove ? _inputReader.MoveInput * moveSpeed : 0f;
        public float ClimbInput => _inputReader.ClimbInput;


        public float ClimbSpeed { get; private set; }
        public Vector2 JumpSpeed => new(jumpXSpeed * jumpDir, jumpYSpeed);
        public float JumpDuration => jumpDuration;
        public float Impulse => jumpDashImpulse;
        public Vector2 PushSpeed { get; private set; }


        public float SpeedMultiplier => SprintControl.IsSprinting ? moveSpeedMultiplier : 1f;
        public float CrouchSpeedMultiplier => CrouchControl.MoveSpeedMultiplier;

        #endregion

        #region State Settings

        public bool IsMoving => _inputReader.MoveInput != 0;
        public bool IsGrounded => Mover.IsGround;

        public bool IsClimb => !IsGrounded
                               && _checkClimbWall.IsClimbed
                               && Mover.CanClimb
                               && _stats.Stamina > 0f;

        public bool IsWallJump => _moveStateMachine.CurrentState is WallJumpState;
        public bool IsWallDash => _moveStateMachine.CurrentState is WallDashState;

        public bool CanMove { get; private set; } = true;

        public bool IsDead { get; private set; }

        // Stamina
        public float CurrentStamina => _stats.Stamina;

        // spend amount
        public float ClimbStaminaCostPerSecond => staminaCosts.climbPerSecond;

        #endregion

        #region Simple Method

        public void ChangeSpeed(float speed) {
            if (_moveStateMachine != null && _moveStateMachine.TryGetState<WalkState>(out var walkState))
                moveSpeed = speed;
        }

        public void LockMovement() {
            CanMove = false;
            CanSJ = false;
            SprintControl.StopSprint();
        }

        public void SetPushSpeed(Vector2 speed) {
            PushSpeed = speed;
        }

        #endregion

        #region DeadHandler

        private bool _isHeat;
        private bool _isHighHeat;

        private void OnDead() {
            if (IsDead) return;

            if (GameManager.Instance == null) {
                Debug.LogError("사망 복귀에 필요한 GameManager가 없습니다.");
                return;
            }

            if (GameManager.Instance.IsRestarting) return;

            IsDead = true;
            SprintControl.StopSprint();
            UnsubscribeInputEvents();

            GameManager.Instance.Restart(this);
        }

        #endregion

        #region CheckMethod

        public bool TryWallJump() {
            if (!CanStartWallAction(staminaCosts.wallJump)) return false;

            _stats.UseStamina(staminaCosts.wallJump, true);
            return true;
        }

        public bool TryWallDash() {
            if (!CanStartWallAction(staminaCosts.wallDash)) return false;

            _stats.UseStamina(staminaCosts.wallDash, true);
            return true;
        }

        private bool CanStartWallAction(float staminaCost) {
            return IsClimb && !IsWallJump
                           && !IsWallDash
                           && _stats.Stamina >= staminaCost;
        }

        #endregion

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

        #endregion

        #region RestoreProgress

        public void RestoreAfterDeath() {
            progress.RestoreAfterDeath();
        }

        public void RestoreState(Vector2 position, float stamina) {
            SprintControl.StopSprint();
            _moveStateMachine.ChangeState<WalkState>();

            Mover.RestorePosition(position);
            CrouchControl.Stand();
            _stats.RestoreStamina(stamina);

            PushSpeed = Vector2.zero;
            CanMove = true;
            CanSJ = true;
        }

        public void FinishRespawn() {
            if (!IsDead) return;

            IsDead = false;
            SubscribeInputEvents();
        }

        #endregion
    }
}
