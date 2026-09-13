using System;
using _02._Script.Players;

namespace _02._Script.FSM.MoveState {
    [Serializable]
    public abstract class GroundState : PlayerMoveState {
        protected GroundState(Player owner, MoveStateMachine stateMachine) : base(owner, stateMachine) { }

        protected bool CheckGround() {
            return TryTransition<ClimbState>(_player.IsClimb);
        }

        public override void HandleJumpInput() {
            if (!_player.CanSJ || !_player.IsGrounded) return;
            _player.Jump();
        }
    }
}