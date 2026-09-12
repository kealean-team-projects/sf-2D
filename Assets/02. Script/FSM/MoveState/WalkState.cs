using _02._Script.Players;

namespace _02._Script.FSM.MoveState {
    [System.Serializable]
    public class WalkState : GroundState
    {
        public float walkSpeed = 10f;
        public WalkState(Player owner, MoveStateMachine stateMachine) : base(owner, stateMachine) { }

        public override void Enter() {
        }

        public override void Tick()
        {
            if (CheckGround()) return;
            _player.ApplyManualMove(_player.MoveInput * walkSpeed);
        }

        public override void Exit() { }
    }
}