using _02._Script.Component;
using _02._Script.Players;
using UnityEngine;

namespace _02._Script.Trap
{
    public class UrchinTrap : TrapBase
    {
        protected override void OnPlayerEnter(Player player)
        {
            if (player.TryGetComponent<DamageModule>(out var damage))
                damage.TakeDamage();
        }
    }
}