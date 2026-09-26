using UnityEngine;

namespace _02._Script._01_Players.Components.Audio {
    public enum FootstepMaterial { Default, Stone }

    [DisallowMultipleComponent]
    public sealed class FootstepSurface : MonoBehaviour {
        [SerializeField] private FootstepMaterial material = FootstepMaterial.Stone;
        public FootstepMaterial Material => material;
    }
}
