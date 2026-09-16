using System;
using _02._Script.Component;
using _02._Script.Players;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _02._Script.Trap
{
    public class Clamp : TrapBase
    {
        protected override void OnPlayerEnter(Player player)
        {
            player.ChangeSpeed(0f);
            player.CanSJ = false;

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