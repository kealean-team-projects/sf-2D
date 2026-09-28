using System;
using _02._Script._01_Players.Interface;
using UnityEngine;

namespace _02._Script._01_Players.FSM.MoveState {
    [Serializable]
    public class WallDashState : PlayerMoveState {
        private float _elapsedTime;

        public WallDashState(IPlayerMoveContext context, MoveStateMachine stateMachine) :
            base(context, stateMachine) { }

        public WallDashState(IPlayerMoveContext context, MoveStateMachine stateMachine, string animBoolName) :
            base(context, stateMachine, animBoolName) { }

        public override void Enter() {
            base.Enter();
            _elapsedTime = 0f;
            _context.Mover.WallDash();
            _context.Mover.ApplyWallDashVelocity(_context.WallDashSpeed);
        }

        public override void Tick() {
            _elapsedTime += Time.fixedDeltaTime;

            // 대쉬 중에는 매 물리 스텝 속도를 유지한다(벽 마찰/충돌로 감속되지 않게).
            var wallEnded = !_context.IsTouchingClimbWall;
            if (_elapsedTime < _context.WallDashDuration && !wallEnded) {
                _context.Mover.ApplyWallDashVelocity(_context.WallDashSpeed);
                return;
            }

            // 끝: 벽 끝을 넘었으면 남은 속도로 턱 위로 튀어 오르고, 아니면 다시 벽에 매달린다.
            _context.Mover.ClampRiseSpeed(_context.WallDashExitSpeed);
            _context.EndWallDash();
            ReturnToMovement();
        }

        public override void Exit() {
            base.Exit();
            _context.EndWallDash();
        }
    }
}