using _02._Script._01_Players.FSM.MoveState;
using _02._Script._01_Players.Interface;

namespace _02._Script._01_Players.FSM {
    public static class PlayerMoveStateFactory {
        public static MoveStateMachine Create(IPlayerMoveContext owner, IStats stats) {
            var stateMachine = new MoveStateMachine();

            stateMachine.AddState(new WalkState(owner, stateMachine));
            stateMachine.AddState(new ClimbState(owner, stateMachine, stats));
            stateMachine.AddState(new WallJumpState(owner, stateMachine));
            stateMachine.AddState(new WallDashState(owner, stateMachine));

            stateMachine.ChangeState<WalkState>();

            return stateMachine;
        }
    }
}