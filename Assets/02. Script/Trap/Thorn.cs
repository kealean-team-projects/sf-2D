using _02._Script._00_Scripts._07_Managers;
using _02._Script.Component;
using _02._Script.Interface;

namespace _02._Script.Trap {
    public class Thorn : TrapBase, IDamagable {
        private void Awake() {
            DamageCompo.OnDamaged += GameManager.Instance.Restart;
        }

        public DamageModule DamageCompo { get; }

        public override void TriggerRule() { }

        protected override void Damage() { }
    }
}