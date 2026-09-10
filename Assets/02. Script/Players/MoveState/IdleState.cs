using UnityEngine;

namespace _02._Script.Players.MoveState {
    public class IdleState : PlayerMoveState {
        public IdleState(IPlayerMoveContext context, MoveStateMachine stateMachine) : base(context, stateMachine) { }

        public override void Enter() {
            _context.ApplyMoveInput(0f);
            Debug.Log("IdleState 진입");
        }

        public override void Tick() {
            if (_context.MoveInput != 0f) {
                _stateMachine.ChangeState<WalkState>();
                return;
            }

            _context.ApplyMoveInput(0f);
        }

        public override void Exit() { }
    }
}