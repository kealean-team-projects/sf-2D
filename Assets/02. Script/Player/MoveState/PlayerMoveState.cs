namespace _02._Script.Player.MoveState
{
    //왜 필요한가: 상태는 이제 Player의 점프·스태미나·충돌체 같은 기능에 접근할 수 없고,
    //인터페이스가 허용한 이동 기능만 사용할 수 있다.
    public abstract class PlayerMoveState
    {
        protected readonly IPlayerMoveContext _context;
        protected readonly MoveStateMachine _stateMachine;
        
        protected PlayerMoveState(IPlayerMoveContext context, MoveStateMachine stateMachine)
        {
            _context = context;
            _stateMachine = stateMachine;
        }
        
        public abstract void Enter();
        public abstract void Tick();
        public abstract void Exit();
    }
}