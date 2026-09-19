using _02._Script._01_Players;
using _02._Script._01_Players.Components.DamageCompo;

namespace _02._Script._03_TrapAndEnemy.Traps
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