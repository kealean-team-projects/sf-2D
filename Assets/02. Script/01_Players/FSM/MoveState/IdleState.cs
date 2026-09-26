using _02._Script._01_Players.Interface;
using UnityEngine;

namespace _02._Script._01_Players.FSM.MoveState
{
    public class IdleState : GroundState
    {
        public IdleState(IPlayerMoveContext context, MoveStateMachine stateMachine) : base(context, stateMachine)
        {
        }

        public IdleState(IPlayerMoveContext context, MoveStateMachine stateMachine, string animBoolName) : base(context, stateMachine, animBoolName)
        {
        }

    }
}
