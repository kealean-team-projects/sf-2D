using System;
using _02._Script._01_Players.Interface;
using UnityEngine;

namespace _02._Script._01_Players.FSM.MoveState {
    [Serializable]
    public class WallDashState : PlayerMoveState {
        private float _elapsedTime;

        public WallDashState(IPlayerMoveContext context, MoveStateMachine stateMachine) : base(context, stateMachine) { }

        public override void Enter() {
            _elapsedTime = 0f;
            _context.Mover.WallDash();
            _context.Mover.ApplyWallDash(_context.Impulse);
        }

        public override void Tick() {
            _elapsedTime += Time.fixedDeltaTime;

            if (_elapsedTime < _context.JumpDuration)
                return;

            _context.EndWallDash();
            ReturnToMovement();
        }

        public override void Exit() {
            _context.EndWallDash();
        }
    }
}