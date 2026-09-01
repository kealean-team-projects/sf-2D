using System;
using _01.Script.Player.Interface;
using UnityEngine;

namespace _01.Script.Player.Components {
    public class CheckClimbWall : Check, ICheckClimbWall, IAgentModule {
        [field: SerializeField] public bool IsClimbed { get; private set; }

        private void FixedUpdate() {
            var col = CheckCol();
            IsClimbed = col != null;
        }

        public void Initialize(Agent owner) { }
        
        public Type Type => typeof(ICheckClimbWall);
    }
}