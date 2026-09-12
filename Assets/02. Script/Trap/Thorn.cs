using System;
using _02._Script.Component;
using _02._Script.Interface;
using UnityEngine;
using _02._Script._00_Scripts._07_Managers;

namespace _02._Script.Trap
{
    public class Thorn : TrapBase, IDamagable
    {
        public DamageModule DamageCompo { get; }

        private void Awake()
        {
            DamageCompo.OnDamaged += GameManager.Instance.Restart;
        }

        public override void TriggerRule()
        {
            
        }
        
        protected override void Damage()
        {
            
        }
    }
}