using _02._Script.Players;
using UnityEngine;

namespace _02._Script.FSM.MoveState
{
    [System.Serializable]
    public class WallJumpState : PlayerMoveState
    {
        public float jumpXSpeed = 8f;
        public float jumpYSpeed = 12f;
        public float jumpDuration = 0.2f;
        
        private float _elapsedTime;
        
        public WallJumpState(Player owner, MoveStateMachine stateMachine) : base(owner, stateMachine)
        {
        }

        public override void Enter()
        {
            _elapsedTime = 0f;
            _player.ApplyWallJump(jumpXSpeed, jumpYSpeed);
        }

        public override void Exit()
        {
        }

        public override void Tick()
        {
            _elapsedTime += Time.fixedDeltaTime;

            if (_elapsedTime < jumpDuration)
                return;

            ReturnToMovement();
        }
    }
}