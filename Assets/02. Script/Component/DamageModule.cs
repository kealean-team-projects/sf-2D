using System;
using _02._Script.Players;
using UnityEngine;

namespace _02._Script.Component
{
    public sealed class DamageModule : MonoBehaviour
    {
        public event Action OnDamaged;

        private void OnTriggerEnter2D(Collider2D other)
        {
            OnDamaged?.Invoke();
        }
    }
}
