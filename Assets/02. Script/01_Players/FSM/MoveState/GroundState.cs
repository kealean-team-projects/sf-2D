using System;
using _02._Script._01_Players.Interface;

namespace _02._Script._01_Players.FSM.MoveState {
    [Serializable]
    public abstract class GroundState : PlayerMoveState {
        protected GroundState(IPlayerMoveContext context, MoveStateMachine stateMachine) :
            base(context, stateMachine) { }

        protected GroundState(IPlayerMoveContext context, MoveStateMachine stateMachine, string animBoolName) :
            base(context, stateMachine, animBoolName) { }

        public override void Tick() {
            ReturnToMovement();
            if (_stateMachine.CurrentState is GroundState)
                ApplyHorizontalMovement();
        }

        public override void HandleJumpInput() {
            if (!_context.CanSJ || !_context.IsGrounded) return;
            _context.Jump();
            _stateMachine.ChangeState<JumpState>();
        }
    }
}