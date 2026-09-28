using System;
using _02._Script._01_Players.Interface;

namespace _02._Script._01_Players.FSM.MoveState {
    [Serializable]
    public class WalkState : GroundState {
        public WalkState(IPlayerMoveContext context, MoveStateMachine stateMachine) : base(context, stateMachine) { }

        public WalkState(IPlayerMoveContext context, MoveStateMachine stateMachine, string animBoolName) :
            base(context, stateMachine, animBoolName) { }
    }
}