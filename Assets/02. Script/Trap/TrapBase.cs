using System;
using UnityEngine;

namespace _02._Script.Trap
{
    public enum TrapType
    {
        Damage,
        Effect
    }
    [RequireComponent(typeof(Collider2D))]
    public abstract class TrapBase : MonoBehaviour
    {
        [SerializeField] private TrapType trapType;

        public abstract void TriggerRule(); // 발동 조건 그러나 일단 보류

        private void Update()
        {
            TriggerRule();
        }

        protected virtual void Damage()
        {
            
        }

        protected virtual void Effect(Collider2D other)
        {
            
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                switch (trapType)
                {
                    case TrapType.Damage:
                        Damage();
                        break;
                    case TrapType.Effect:
                        Effect(other);
                        break;
                }
            }
        }
    }
}