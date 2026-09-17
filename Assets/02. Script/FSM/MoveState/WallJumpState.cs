using System;
using _02._Script.Players;
using UnityEngine;

namespace _02._Script.FSM.MoveState {
    [Serializable]
    public class WallJumpState : PlayerMoveState {
        private float _elapsedTime;

        public WallJumpState(Player owner, MoveStateMachine stateMachine) : base(owner, stateMachine) { }

        public override void Enter() {
            _elapsedTime = 0f;
            _player.Mover.ApplyWallJump(_player.JumpSpeed);
        }

        public override void Exit() { }

        public override void Tick() {
            _elapsedTime += Time.fixedDeltaTime;

            if (_elapsedTime < _player.JumpDuration)
                return;

            ReturnToMovement();
        }
    }
}