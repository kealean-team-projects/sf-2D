using System;
using _02._Script._01_Players.Interface;

namespace _02._Script._01_Players.FSM.MoveState {
    [Serializable]
    public class AirState : PlayerMoveState {
        public AirState(IPlayerMoveContext context, MoveStateMachine stateMachine) :
            base(context, stateMachine) { }

        public AirState(IPlayerMoveContext context, MoveStateMachine stateMachine, string animBoolName) :
            base(context, stateMachine, animBoolName) { }

        public override void Tick() {
            ReturnToMovement();
            if (_stateMachine.CurrentState == this)
                ApplyHorizontalMovement();
        }
    }
}