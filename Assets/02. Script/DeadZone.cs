using _02._Script._00_Scripts._07_Managers;
using UnityEngine;

namespace _02._Script {
    public class DeadZone : MonoBehaviour {
        private void OnTriggerEnter2D(Collider2D other) {
            if (other.CompareTag("Player")) {
                Debug.Log("플레이어가 사망하였습니다. 이전 위치로 복구합니다.");
                GameManager.Instance.Restart();
            }
        }
    }
}