using System;
using _02._Script.Players;
using _02._Script.Players.Interface;

namespace _02._Script.FSM.MoveState {
    [Serializable]
    public class ClimbState : PlayerMoveState {
        public ClimbState(IPlayerMoveContext _context, MoveStateMachine stateMachine) : base(_context, stateMachine) { }

        public override void Enter() { }

        public override void Exit() { }

        public override void Tick() {
            if (TryTransition<WalkState>(!_context.IsClimb))
                return;

            _context.Mover.ApplyClimb(_context.ClimbInput * _context.ClimbSpeed);
        }

        public override void HandleJumpInput() {
            if (!_context.IsClimb)
                return;

            if (_context.MoveInput != 0f) {
                TryTransition<WallJumpState>(_context.TryWallJump());
                return;
            }

            if (_context.ClimbInput > 0f) {
                TryTransition<WallDashState>(_context.TryWallDash());
                return;
            }

            if (_context.ClimbInput < 0f) {
                _context.CancelClimb();
                _stateMachine.ChangeState<WalkState>();
            }
        }
    }
}