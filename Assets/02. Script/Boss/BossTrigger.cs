using System;
using _02._Script._01_Players;
using UnityEngine;

namespace _02._Script.Boss
{
    [RequireComponent(typeof(Collider2D))]
    public class BossTrigger : MonoBehaviour
    {
        public event Action OnEnter;
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
                OnEnter?.Invoke();
        }
    }
}