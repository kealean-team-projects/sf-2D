using UnityEngine;

namespace _02._Script.Trap {
    public enum TrapType {
        Damage,
        Effect
    }

    [RequireComponent(typeof(Collider2D))]
    public abstract class TrapBase : MonoBehaviour {
        [SerializeField] private TrapType trapType;

        private void Update() {
            TriggerRule();
        }

        private void OnTriggerEnter2D(Collider2D other) {
            if (other.CompareTag("Player"))
                switch (trapType) {
                    case TrapType.Damage:
                        Damage();
                        break;
                    case TrapType.Effect:
                        Effect(other);
                        break;
                }
        }

        public abstract void TriggerRule(); // 발동 조건 그러나 일단 보류

        protected virtual void Damage() { }

        protected virtual void Effect(Collider2D other) { }
    }
}