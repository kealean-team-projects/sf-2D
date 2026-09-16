using System;
using _02._Script.Component;
using _02._Script.Players;
using UnityEngine;

namespace _02._Script.Trap.Enemies
{
    public abstract class EnemyBase : TrapBase
    {
        private void FixedUpdate() => Move();

        protected override void OnPlayerEnter(Player player) { }
        
        protected virtual void Move() { }
        
        protected virtual bool CanAttack() => false;
        
        protected virtual void Attack() { }

        protected void DealDamage(Player player)
        {
            if (player == null) return;

            if (player.TryGetComponent(out DamageModule dmg))
            {
                dmg.TakeDamage();
            }
        }
        
        protected bool IsAttacking { get; private set; }

        protected void TryAttack()
        {
            if (IsAttacking || !CanAttack()) return;

            IsAttacking = true;
            Attack();
        }

        protected void EndAttack()
        {
            IsAttacking = false;
        }
    }
}