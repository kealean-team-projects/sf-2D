using _02._Script.Players;

namespace _02._Script.FSM.MoveState
{
    [System.Serializable]
    public class ClimbState : PlayerMoveState
    {
        public float climbSpeed;
        public float climbUpSpeed = 10f;
        public float climbDownSpeed = 20f;
        public ClimbState(Player owner, MoveStateMachine stateMachine) : base(owner, stateMachine)
        {
        }

        public override void Enter()
        {
            
        }

        public override void Exit()
        {
        }

        public override void Tick()
        {
            if (TryTransition<WalkState>(!_player.IsClimb))
                return;

            climbSpeed = _player.ClimbInput > 0f
                ? climbUpSpeed
                : climbDownSpeed;

            _player.ApplyClimb(_player.ClimbInput * climbSpeed);
        }
        
        public override void HandleJumpInput()
        {
            if (!_player.IsClimb)
                return;

            if (_player.MoveInput != 0f)
            {
                TryTransition<WallJumpState>(_player.TryWallJump());
                return;
            }

            if (_player.ClimbInput > 0f)
            {
                TryTransition<WallDashState>(_player.TryWallDash());
                return;
            }

            if (_player.ClimbInput < 0f)
            {
                _player.CancelClimb();
                _stateMachine.ChangeState<WalkState>();
            }
        }
    }
}