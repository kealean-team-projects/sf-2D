using _02._Script._01_Players.Interface;
using _02._Script.Interface;
using UnityEngine;

namespace _02._Script._01_Players.FSM {
    //왜 필요한가: 상태는 이제 Player의 점프·스태미나·충돌체 같은 기능에 접근할 수 없고,
    //인터페이스가 허용한 이동 기능만 사용할 수 있다.
    public abstract class PlayerMoveState : IState {
        private readonly int? _animBoolHash;
        protected readonly IPlayerMoveContext _context;
        protected readonly MoveStateMachine _stateMachine;

        protected PlayerMoveState(IPlayerMoveContext context, MoveStateMachine stateMachine) {
            _context = context;
            _stateMachine = stateMachine;
        }

        protected PlayerMoveState(IPlayerMoveContext context, MoveStateMachine stateMachine, string animBoolName)
            : this(context, stateMachine) {
            _animBoolHash = Animator.StringToHash(animBoolName);
        }

        public virtual void Enter() {
            if (_animBoolHash.HasValue)
                _context.SetAnimationBool(_animBoolHash.Value, true);
        }

        public virtual void Exit() {
            if (_animBoolHash.HasValue)
                _context.SetAnimationBool(_animBoolHash.Value, false);
        }

        public void UpdateState() {
            Tick();
        }

        public abstract void Tick();
        public virtual void HandleJumpInput() { }

        protected bool TryTransition<T>(bool condition) where T : PlayerMoveState {
            if (!condition)
                return false;

            _stateMachine.ChangeState<T>();
            return true;
        }

        protected void ReturnToMovement() {
            _stateMachine.ReturnToMovement(_context);
        }

        protected void ApplyHorizontalMovement() {
            _context.Mover.ApplyManualMove(_context.MoveInput * _context.SpeedMultiplier
                                                              * _context.CrouchSpeedMultiplier + _context.PushSpeed.x);
        }
    }
}