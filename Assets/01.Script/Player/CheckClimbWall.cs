using UnityEngine;

namespace _01.Script.Player {
    public class CheckClimbWall : Check {
        [field: SerializeField] public bool IsClimbed { get; private set; }

        private void FixedUpdate() {
            var col = CheckCol();
            IsClimbed = col != null;
        }
    }
}