using System;
using _02._Script._01_Players.Components.CheckComponent;
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
        [SerializeField] private Animator animator;

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

        [Tooltip("벽점프 직후 이 시간 동안은 도착한 벽에 다시 붙지 않는다(출발한 벽에 바로 재부착되는 것 방지).")]
        [SerializeField] private float wallJumpMinTime = 0.08f;

        [Tooltip("벽점프 입력 잠금 최대 시간. 보통은 상승이 끝나(낙하 시작) 입력이 먼저 풀린다.")]
        [SerializeField] private float wallJumpMaxTime = 0.8f;

        [Header("WallDash Settings")]
        [Tooltip("벽 등반 대쉬 속도(유닛/초). 대쉬 동안 중력 0, 이 속도로 위로 이동.")]
        [SerializeField] private float wallDashSpeed = 22f;

        [SerializeField] private float wallDashDuration = 0.22f;

        [Tooltip("대쉬가 끝날 때 남기는 최대 상승 속도. 벽 끝을 넘으면 이 속도로 살짝 튀어 올라 턱 위로 올라선다.")]
        [SerializeField] private float wallDashExitSpeed = 8f;

        [Header("Crouch Settings")] [SerializeField]
        private float crouchSpeedMultiplier = 0.5f;

        [SerializeField] private PlayerProgress progress;

        private bool _canSJ = true;
        
        public event Action<PlayerMoveState, PlayerMoveState> MoveStateChanged;

        public PlayerMoveState CurrentMoveState =>
            _moveStateMachine?.CurrentState;
        
        private void HandleMoveStateChanged(
            PlayerMoveState previousState,
            PlayerMoveState currentState)
        {
            MoveStateChanged?.Invoke(previousState, currentState);
        }

        public void SetAnimationBool(int parameterHash, bool value) {
            // 애니메이션 구성은 선택 사항이며 이동 상태 전환을 막지 않는다.
            if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null)
                return;

            foreach (var parameter in animator.parameters) {
                if (parameter.nameHash != parameterHash || parameter.type != AnimatorControllerParameterType.Bool)
                    continue;

                animator.SetBool(parameterHash, value);
                return;
            }
        }

        private void Update() {
            if (IsDead) return;
            // 벽점프 중에는 입력으로 방향이 바뀌지 않는다(점프 방향을 바라봐야 다음 벽을 감지할 수 있다).
            if (!IsClimb && !IsWallJump) _facingController.UpdateFacing(MoveInput);

            Mover.CalculateAirTime(IsClimb);

            _stats.StaminaUpdate(IsGrounded, IsMoving, IsClimb, IsSprinting);
            SprintControl.Tick(IsMoving);

            ClimbSpeed = ClimbInput > 0f ? climbUpSpeed : climbDownSpeed;
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
            _moveStateMachine.StateChanged += HandleMoveStateChanged;

            var viewerObject = new GameObject("StateMachineViewer");
            viewerObject.transform.SetParent(transform, false);

            var viewer = viewerObject.AddComponent<StateMachineViewer>();
            viewer.Initialize(_moveStateMachine);

            _damage.OnDamaged += OnDead;
            progress.Initialize(this);
            UIManager.Instance.RegisterPlayer(this);
        }

        protected override void OnDispose() {
            base.OnDispose();
            UnsubscribeInputEvents();

            _damage.OnDamaged -= OnDead;
            progress.Shutdown();
            
            if (_moveStateMachine != null)
            {
                _moveStateMachine.StateChanged -= HandleMoveStateChanged;
            }
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
        public IStats Stats => _stats;
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
        // 점프 입력 순간의 방향으로 계산한다(방향키와 점프를 같은 프레임에 눌러도 누른 방향이 반영되도록).
        public Vector2 JumpSpeed => new(jumpXSpeed * GetWallJumpDirection(), jumpYSpeed);
        public float JumpDuration => jumpDuration;
        public float Impulse => jumpDashImpulse;
        public float WallJumpMinTime => wallJumpMinTime;
        public float WallJumpMaxTime => wallJumpMaxTime;
        public float WallDashSpeed => wallDashSpeed;
        public float WallDashDuration => wallDashDuration;
        public float WallDashExitSpeed => wallDashExitSpeed;
        public Vector2 PushSpeed { get; private set; }


        public float SpeedMultiplier => SprintControl.IsSprinting ? moveSpeedMultiplier : 1f;
        public float CrouchSpeedMultiplier => CrouchControl.MoveSpeedMultiplier;

        #endregion

        #region State Settings

        public bool IsMoving => _inputReader.MoveInput != 0;
        public bool IsGrounded => Mover.IsGround;
        public float VerticalSpeed => Mover.VerticalSpeed;
        public bool IsSprinting => SprintControl.IsSprinting;
        public bool IsCrouching => CrouchControl.IsCrouching;

        public bool IsClimb => !IsGrounded
                               && _checkClimbWall.IsClimbed
                               && Mover.CanClimb
                               && _stats.Stamina > 0f;

        // 스태미나/대쉬 여부와 상관없이 "지금 등반 가능한 벽에 닿아 있는가"
        public bool IsTouchingClimbWall => _checkClimbWall != null && _checkClimbWall.IsClimbed;

        public bool IsWallJump => _moveStateMachine != null && _moveStateMachine.CurrentState is WallJumpState;
        public bool IsWallDash => _moveStateMachine != null && _moveStateMachine.CurrentState is WallDashState;

        public bool CanMove { get; private set; } = true;

        public bool IsDead { get; private set; }

        // Stamina
        public float CurrentStamina => _stats.Stamina;

        // spend amount
        public float ClimbStaminaCostPerSecond => staminaCosts.climbPerSecond;

        #endregion

        #region Simple Method

        public void ChangeSpeed(float speed) {
            if (_moveStateMachine != null)
                moveSpeed = speed;
        }

        public void LockMovement() {
            CanMove = false;
            CanSJ = false;
            SprintControl.StopSprint();
        }

        public void FaceDirection(float xDirection) {
            _facingController.UpdateFacing(xDirection);
        }

        public void SetPushSpeed(Vector2 speed) {
            PushSpeed = speed;
        }
        
        public void SetHUD(StaminaHUD hud) {
            staminaHUD = hud;
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

        public bool TryWallJump()
        {
            if (!CanStartWallAction(staminaCosts.wallJump))
                return false;

            _stats.UseStamina(staminaCosts.wallJump, true);

            return true;
        }

        public bool TryWallDash()
        {
            if (!CanStartWallAction(staminaCosts.wallDash))
                return false;

            _stats.UseStamina(staminaCosts.wallDash, true);

            return true;
        }

        // 기본: 벽 반대쪽으로 점프. 양면 덩굴(ClimbSurface.twoSided)에 매달려 있으면 입력한 방향으로 뛸 수 있다.
        private float GetWallJumpDirection() {
            var away = _facingController.IsFacingLeft ? 1f : -1f;
            if (!IsClimb || _inputReader.MoveInput == 0f) return away;
            var surface = _checkClimbWall.CurrentSurface;
            if (surface != null && surface.TryGetComponent(out ClimbSurface climbSurface) && climbSurface.TwoSided)
                return Mathf.Sign(_inputReader.MoveInput);
            return away;
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

        public void Jump()
        {
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
            Mover.RestorePosition(position);
            CrouchControl.Stand();
            _stats.RestoreStamina(stamina);
            staminaHUD?.UpdateStamina(_stats.Stamina);

            PushSpeed = Vector2.zero;
            CanMove = true;
            CanSJ = true;
            _moveStateMachine.ReturnToMovement(this);
        }

        public void FinishRespawn() {
            if (!IsDead) return;

            IsDead = false;
            SubscribeInputEvents();
        }

        #endregion
    }
}
