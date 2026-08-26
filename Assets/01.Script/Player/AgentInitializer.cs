using UnityEngine;

namespace _01.Script.Player {
    public class AgentInitializer : MonoBehaviour {
        [SerializeField] private Agent agent;

        private void Reset() {
            agent = transform.root.GetComponent<Agent>();
        }

        private void Awake() {
            agent.Initialize();
        }

        private void OnDestroy() {
            agent.Dispose();
        }
    }
}