using System;
using _02._Script.Players;

namespace _02._Script.FSM.MoveState {
    [Serializable]
    public class WalkState : GroundState {
        public WalkState(Player owner, MoveStateMachine stateMachine) : base(owner, stateMachine) { }

        public override void Enter() { }

        public override void Tick() {
            if (CheckGround()) return;
            _player.Mover.ApplyManualMove(_player.MoveInput * _player.SpeedMultiplier
                                        * _player.CrouchSpeedMultiplier + _player.PushSpeed);
        }

        public override void Exit() { }
    }
}