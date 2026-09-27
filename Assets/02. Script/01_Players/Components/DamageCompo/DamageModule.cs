using System;
using UnityEngine;

namespace _02._Script._01_Players.Components.DamageCompo {
    public sealed class DamageModule : MonoBehaviour {
        public event Action OnDamaged;

        public void TakeDamage() {
            Debug.Log("사망 발동");
            OnDamaged?.Invoke();
        }
    }
}