namespace _02._Script.Player.MoveState
{
    public static class PlayerMoveStateFactory
    {
        public static MoveStateMachine Create(IPlayerMoveContext context)
        {
            var stateMachine = new MoveStateMachine();

            stateMachine.AddState(new IdleState(context, stateMachine));
            stateMachine.AddState(new WalkState(context, stateMachine));

            stateMachine.ChangeState<IdleState>();

            return stateMachine;
        }
    }
}