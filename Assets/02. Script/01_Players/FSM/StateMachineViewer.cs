using UnityEngine;

namespace _02._Script._01_Players.FSM {
    public class StateMachineViewer : MonoBehaviour {
        [SerializeField] private string currentState;

        private MoveStateMachine _machine;

        public void Initialize(MoveStateMachine machine) {
            _machine = machine;
        }

        private void LateUpdate() {
            currentState = _machine?.CurrentState?.GetType().Name;
        }
    }
}