using _02._Script._01_Players;
using UnityEngine;

namespace _02._Script.Boss
{
    [RequireComponent(typeof(Collider2D))]
    public class BossTrigger : MonoBehaviour
    {
        [SerializeField] private BossRoom bossRoom;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<Player>() == null)
                return;

            if (bossRoom == null)
            {
                Debug.LogError("BossRoom이 연결되지 않았습니다.", this);
                return;
            }

            bossRoom.Begin();
        }
    }
}