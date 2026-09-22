using System;
using _02._Script._01_Players.Interface;
using UnityEngine;

namespace _02._Script._01_Players.FSM.MoveState {
    [Serializable]
    public class ClimbState : PlayerMoveState {
        private readonly IStats _stats;

        public ClimbState(IPlayerMoveContext _context, MoveStateMachine stateMachine, IStats stats) : base(_context,
            stateMachine) {
            _stats = stats;
        }

        public override void Enter() { }

        public override void Exit() { }

        public override void Tick() {
            if (TryTransition<WalkState>(!_context.IsClimb))
                return;

            if (_context.ClimbInput != 0f) {
                var amount = _context.ClimbStaminaCostPerSecond * Time.fixedDeltaTime;
                _stats.UseStamina(amount, true);

                if (_stats.Stamina <= 0f) {
                    _context.CancelClimb();
                    _stateMachine.ChangeState<WalkState>();
                    return;
                }
            }

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