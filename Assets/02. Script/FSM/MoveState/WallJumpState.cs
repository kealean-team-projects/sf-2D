using System;
using _02._Script.Players;
using _02._Script.Players.Interface;
using UnityEngine;

namespace _02._Script.FSM.MoveState {
    [Serializable]
    public class WallJumpState : PlayerMoveState {
        private float _elapsedTime;

        public WallJumpState(IPlayerMoveContext context, MoveStateMachine stateMachine) : base(context, stateMachine) { }

        public override void Enter() {
            _elapsedTime = 0f;
            _context.Mover.ApplyWallJump(_context.JumpSpeed);
        }

        public override void Exit() { }

        public override void Tick() {
            _elapsedTime += Time.fixedDeltaTime;

            if (_elapsedTime < _context.JumpDuration)
                return;

            ReturnToMovement();
        }
    }
}