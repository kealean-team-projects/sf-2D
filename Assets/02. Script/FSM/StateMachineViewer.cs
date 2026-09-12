using System.Collections.Generic;
using _02._Script.FSM.MoveState;
using UnityEngine;

namespace _02._Script.FSM
{
    public class StateMachineViewer : MonoBehaviour
    {
        [SerializeField] private string currentState;
        [SerializeReference]
        private List<PlayerMoveState> states = new();

        private MoveStateMachine _machine;

        public void Initialize(MoveStateMachine machine)
        {
            _machine = machine;
            states.Clear();

            foreach (var state in machine.States)
                states.Add(state);
        }

        private void LateUpdate()
        {
            currentState = _machine?.CurrentState?.GetType().Name;
        }
    }
}