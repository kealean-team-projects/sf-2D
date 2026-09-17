using System;
using _02._Script.Players;
using UnityEngine;

namespace _02._Script.FSM.MoveState {
    [Serializable]
    public class WallDashState : PlayerMoveState {
        private float _elapsedTime;

        public WallDashState(Player owner, MoveStateMachine stateMachine) : base(owner, stateMachine) { }

        public override void Enter() {
            _elapsedTime = 0f;
            _player.Mover.ApplyWallDash(_player.Impulse);
        }

        public override void Tick() {
            _elapsedTime += Time.fixedDeltaTime;

            if (_elapsedTime < _player.JumpDuration)
                return;

            _player.EndWallDash();
            ReturnToMovement();
        }

        public override void Exit() { }
    }
}