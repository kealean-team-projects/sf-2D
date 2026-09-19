using _02._Script._01_Players;
using UnityEngine;

namespace _02._Script._03_TrapAndEnemy.Traps
{
    public class WaterCurrent : TrapBase
    {
        [SerializeField] private float pushSpeed = 5f;
        
        protected override void OnPlayerEnter(Player player)
        {
            player.SetPushSpeed(pushSpeed);
        }

        protected override void OnPlayerExit(Player player)
        {
            player.SetPushSpeed(0f);
        }
    }
}