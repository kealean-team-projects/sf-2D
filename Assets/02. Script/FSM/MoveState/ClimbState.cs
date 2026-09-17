using System;
using _02._Script.Players;

namespace _02._Script.FSM.MoveState {
    [Serializable]
    public class ClimbState : PlayerMoveState {
        public ClimbState(Player owner, MoveStateMachine stateMachine) : base(owner, stateMachine) { }

        public override void Enter() { }

        public override void Exit() { }

        public override void Tick() {
            if (TryTransition<WalkState>(!_player.IsClimb))
                return;

            _player.Mover.ApplyClimb(_player.ClimbInput * _player.ClimbSpeed);
        }

        public override void HandleJumpInput() {
            if (!_player.IsClimb)
                return;

            if (_player.MoveInput != 0f) {
                TryTransition<WallJumpState>(_player.TryWallJump());
                return;
            }

            if (_player.ClimbInput > 0f) {
                TryTransition<WallDashState>(_player.TryWallDash());
                return;
            }

            if (_player.ClimbInput < 0f) {
                _player.CancelClimb();
                _stateMachine.ChangeState<WalkState>();
            }
        }
    }
}