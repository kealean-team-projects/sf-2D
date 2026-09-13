using System;
using _02._Script.Players;
using UnityEngine;

namespace _02._Script.FSM.MoveState {
    [Serializable]
    public class WallDashState : PlayerMoveState {
        public float dashImpulse = 15f;
        public float dashDuration = 0.2f;

        private float _elapsedTime;

        public WallDashState(Player owner, MoveStateMachine stateMachine) : base(owner, stateMachine) { }

        public override void Enter() {
            _elapsedTime = 0f;
            _player.ApplyWallDash(dashImpulse);
        }

        public override void Tick() {
            _elapsedTime += Time.fixedDeltaTime;

            if (_elapsedTime < dashDuration)
                return;

            _player.EndWallDash();
            ReturnToMovement();
        }

        public override void Exit() { }
    }
}