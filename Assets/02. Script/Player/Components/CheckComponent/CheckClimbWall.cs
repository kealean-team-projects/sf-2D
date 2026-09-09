using System;
using _02._Script.Player.Interface;
using UnityEngine;

namespace _02._Script.Player.Components.CheckComponent {
    public class CheckClimbWall : Check, ICheckClimbWall, IAgentModule {
        private void FixedUpdate() {
            var col = CheckCol();
            IsClimbed = col != null;
        }

        public void Initialize(Agent owner) { }

        public Type Type => typeof(ICheckClimbWall);
        [field: SerializeField] public bool IsClimbed { get; private set; }
    }
}