using System;
using _02._Script._01_Players.Interface;

namespace _02._Script._01_Players.FSM.MoveState {
    [Serializable]
    public class JumpState : PlayerMoveState {
        private bool _skipLandingOnce;

        public JumpState(IPlayerMoveContext context, MoveStateMachine stateMachine) :
            base(context, stateMachine) { }

        public JumpState(IPlayerMoveContext context, MoveStateMachine stateMachine, string animBoolName) :
            base(context, stateMachine, animBoolName) { }

        public override void Enter() {
            base.Enter();
            // 점프 충격량과 접지 판정이 다음 물리 스텝에 반영될 시간을 준다.
            _skipLandingOnce = true;
        }

        public override void Tick() {
            if (TryTransition<ClimbState>(_context.IsClimb)) return;

            if (!_skipLandingOnce && !MoveStateMachine.IsRising(_context.VerticalSpeed)) {
                ReturnToMovement();
                return;
            }

            _skipLandingOnce = false;
            ApplyHorizontalMovement();
        }
    }
}
