using System;
using _02._Script._01_Players;
using _02._Script._01_Players.Components.DamageCompo;
using BalioProductions.CameraFilterPack.URP;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _02._Script._03_TrapAndEnemy.Traps {
    public class Clamp : TrapBase
    {
        [SerializeField] private Animator animator;
        private DamageModule _damage;
        protected override void OnPlayerEnter(Player player) {
            // 변경
            player.LockMovement();

            if (player.TryGetComponent<DamageModule>(out var damage))
            {
                _damage = damage;
                animator.SetTrigger("catch");
            }
        }

        public void KillPlayer()
        {
            _damage.TakeDamage();
        }

        private async UniTaskVoid DelayDieTask(DamageModule damage) {
            await UniTask.Delay(TimeSpan.FromSeconds(3f));
            damage.TakeDamage();
        }
    }
}