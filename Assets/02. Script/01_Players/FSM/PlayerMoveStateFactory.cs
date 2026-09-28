using _02._Script._01_Players.FSM.MoveState;
using _02._Script._01_Players.Interface;

namespace _02._Script._01_Players.FSM {
    public static class PlayerMoveStateFactory {
        public static MoveStateMachine Create(IPlayerMoveContext owner, IStats stats) {
            var stateMachine = new MoveStateMachine();

            stateMachine.AddState(new IdleState(owner, stateMachine));
            stateMachine.AddState(new WalkState(owner, stateMachine));
            stateMachine.AddState(new RunState(owner, stateMachine));
            stateMachine.AddState(new JumpState(owner, stateMachine));
            stateMachine.AddState(new AirState(owner, stateMachine));
            stateMachine.AddState(new ClimbState(owner, stateMachine, stats));
            stateMachine.AddState(new WallJumpState(owner, stateMachine));
            stateMachine.AddState(new WallDashState(owner, stateMachine));
            stateMachine.AddState(new CrouchState(owner, stateMachine));

            stateMachine.ReturnToMovement(owner);

            return stateMachine;
        }
    }
}