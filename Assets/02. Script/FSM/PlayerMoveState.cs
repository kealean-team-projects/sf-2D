using System;
using _02._Script.FSM.MoveState;
using _02._Script.Interface;
using _02._Script.Players;
using UnityEngine;

namespace _02._Script.FSM {
    //왜 필요한가: 상태는 이제 Player의 점프·스태미나·충돌체 같은 기능에 접근할 수 없고,
    //인터페이스가 허용한 이동 기능만 사용할 수 있다.
    [Serializable]
    public abstract class PlayerMoveState : IState {
        [SerializeField] private string name;

        protected readonly Player _player;
        protected readonly MoveStateMachine _stateMachine;

        protected PlayerMoveState(Player owner, MoveStateMachine stateMachine) {
            name = GetType().Name;
            _player = owner;
            _stateMachine = stateMachine;
        }

        public abstract void Enter();
        public abstract void Exit();

        public void UpdateState() {
            Tick();
        }

        public abstract void Tick();
        public virtual void HandleJumpInput() { }

        protected bool TryTransition<T>(bool condition) where T : PlayerMoveState {
            if (!condition)
                return false;

            _stateMachine.ChangeState<T>();
            return true;
        }

        protected void ReturnToMovement() {
            if (_player.IsClimb)
                _stateMachine.ChangeState<ClimbState>();
            else
                _stateMachine.ChangeState<WalkState>();
        }
    }
}