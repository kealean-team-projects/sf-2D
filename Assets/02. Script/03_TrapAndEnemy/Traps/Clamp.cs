using System;
using _02._Script._01_Players;
using _02._Script._01_Players.Components.DamageCompo;
using Cysharp.Threading.Tasks;

namespace _02._Script._03_TrapAndEnemy.Traps
{
    public class Clamp : TrapBase
    {
        protected override void OnPlayerEnter(Player player)
        {
            // 변경
            player.LockMovement();

            if (player.TryGetComponent<DamageModule>(out var damage))
                DelayDieTask(damage).Forget();
        }
        
        private async UniTaskVoid DelayDieTask(DamageModule damage)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(3f));
            damage.TakeDamage();
        }
    }
}