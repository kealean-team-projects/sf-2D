using _02._Script.FSM.MoveState;
using _02._Script.Players;
using _02._Script.Players.Interface;

namespace _02._Script.FSM {
    public static class PlayerMoveStateFactory {
        public static MoveStateMachine Create(IPlayerMoveContext owner) {
            var stateMachine = new MoveStateMachine();

            stateMachine.AddState(new WalkState(owner, stateMachine));
            stateMachine.AddState(new ClimbState(owner, stateMachine));
            stateMachine.AddState(new WallJumpState(owner, stateMachine));
            stateMachine.AddState(new WallDashState(owner, stateMachine));

            stateMachine.ChangeState<WalkState>();

            return stateMachine;
        }
    }
}