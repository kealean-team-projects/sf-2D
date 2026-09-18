using System;
using _02._Script.Players;
using _02._Script.Players.Interface;

namespace _02._Script.FSM.MoveState {
    [Serializable]
    public class WalkState : GroundState {
        public WalkState(IPlayerMoveContext context, MoveStateMachine stateMachine) : base(context, stateMachine) { }

        public override void Enter() { }

        public override void Tick() {
            if (CheckGround()) return;
            _context.Mover.ApplyManualMove(_context.MoveInput * _context.SpeedMultiplier
                                        * _context.CrouchSpeedMultiplier + _context.PushSpeed);
        }

        public override void Exit() { }
    }
}