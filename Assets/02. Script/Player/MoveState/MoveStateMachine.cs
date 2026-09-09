using System;
using System.Collections.Generic;

namespace _02._Script.Player.MoveState
{
    public class MoveStateMachine
    {
        private readonly Dictionary<Type, PlayerMoveState> _states = new();
        public PlayerMoveState CurrentState { get; private set; }

        public void Tick()
        {
            CurrentState?.Tick();
        }
        

        public void AddState(PlayerMoveState state)
        {
            if (state == null) return;
            _states.Add(state.GetType(), state);
        }

        public void ChangeState<T>() where T : PlayerMoveState
        {
            if (_states.TryGetValue(typeof(T), out var nextState))
            {
                ChangeState(nextState);
            }
        }
        
        public void ChangeState(PlayerMoveState nextState)
        {
            if (nextState == null || CurrentState == nextState) return;

            CurrentState?.Exit();
            CurrentState = nextState;
            CurrentState.Enter();
        }
    }
}