using _02._Script._05_Managers;
using UnityEngine;

namespace _02._Script {
    [RequireComponent(typeof(BoxCollider2D))]
    public class CheckPoint : MonoBehaviour {
        [SerializeField] [Min(0)] private int index;

        private void OnTriggerEnter2D(Collider2D other) {
            if (!other.CompareTag("Player")) return;

            CheckPointManager.Instance.SaveCheckpoint(index);
        }
    }
}