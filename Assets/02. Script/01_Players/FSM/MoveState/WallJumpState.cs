using System;
using _02._Script._01_Players.Interface;
using UnityEngine;

namespace _02._Script._01_Players.FSM.MoveState {
    [Serializable]
    public class WallJumpState : PlayerMoveState {
        private float _elapsedTime;

        public WallJumpState(IPlayerMoveContext context, MoveStateMachine stateMachine) :
            base(context, stateMachine) { }

        public WallJumpState(IPlayerMoveContext context, MoveStateMachine stateMachine, string animBoolName) :
            base(context, stateMachine, animBoolName) { }

        public override void Enter() {
            base.Enter();
            _elapsedTime = 0f;
            var jumpSpeed = _context.JumpSpeed;
            _context.Mover.ApplyWallJump(jumpSpeed);
            // 점프하는 쪽을 바라보게 해서, 벽 감지 박스(WallChecker)가 다음 벽을 향하게 한다.
            _context.FaceDirection(jumpSpeed.x);
        }

        // 벽점프 동안은 좌우 입력을 적용하지 않는다(ApplyHorizontalMovement 호출 안 함).
        // 상승이 끝나 낙하가 시작되면 일반 공중 상태로 돌아가 그때부터 입력이 먹는다.
        public override void Tick() {
            _elapsedTime += Time.fixedDeltaTime;

            if (_elapsedTime < _context.WallJumpMinTime)
                return;

            // 반대편 벽에 닿으면 바로 매달린다 (굴뚝 벽점프 연속 등반)
            if (TryTransition<ClimbState>(_context.IsClimb))
                return;

            var stillRising = MoveStateMachine.IsRising(_context.VerticalSpeed);
            if (stillRising && !_context.IsGrounded && _elapsedTime < _context.WallJumpMaxTime)
                return;

            ReturnToMovement();
        }
    }
}