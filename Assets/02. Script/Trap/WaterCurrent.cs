using _02._Script.Players;
using UnityEngine;

namespace _02._Script.Trap
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