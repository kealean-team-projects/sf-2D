using UnityEngine;

namespace _02._Script.Players {
    public class AgentInitializer : MonoBehaviour {
        [SerializeField] private Agent agent;

        private void Awake() {
            agent.Initialize();
        }

        private void Reset() {
            agent = transform.root.GetComponent<Agent>();
        }

        private void OnDestroy() {
            agent.Dispose();
        }
    }
}