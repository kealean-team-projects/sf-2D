using UnityEngine;

namespace _02._Script.Players.MoveState {
    public class WalkState : PlayerMoveState {
        public WalkState(IPlayerMoveContext context, MoveStateMachine stateMachine) : base(context, stateMachine) { }

        public override void Enter() {
            _context.ApplyMoveInput(_context.MoveInput);
            Debug.Log("WalkState 진입");
        }

        public override void Tick() {
            if (_context.MoveInput == 0f) {
                _stateMachine.ChangeState<IdleState>();
                return;
            }

            _context.ApplyMoveInput(_context.MoveInput);
        }

        public override void Exit() { }
    }
}