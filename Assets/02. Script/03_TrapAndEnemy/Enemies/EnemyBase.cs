using _02._Script._01_Players;
using _02._Script._01_Players.Components.DamageCompo;
using _02._Script._03_TrapAndEnemy.Traps;

namespace _02._Script._03_TrapAndEnemy.Enemies {
    public abstract class EnemyBase : TrapBase {
        protected bool IsAttacking { get; private set; }

        private void FixedUpdate() {
            Move();
        }

        protected override void OnPlayerEnter(Player player) { }

        protected virtual void Move() { }

        protected virtual bool CanAttack() {
            return false;
        }

        protected virtual void Attack() { }

        protected void DealDamage(Player player) {
            if (player == null) return;

            if (player.TryGetComponent(out DamageModule dmg)) dmg.TakeDamage();
        }

        protected void TryAttack() {
            if (IsAttacking || !CanAttack()) return;

            IsAttacking = true;
            Attack();
        }

        protected void EndAttack() {
            IsAttacking = false;
        }
    }
}