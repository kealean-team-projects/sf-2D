using UnityEngine;

namespace _02._Script
{
    using UnityEngine;
    using UnityEngine.Rendering.Universal;

    [RequireComponent(typeof(ShadowCaster2D))]
    public class ShadowCasterController : MonoBehaviour
    {
        private ShadowCaster2D shadowCaster;

        private void Awake()
        {
            shadowCaster = GetComponent<ShadowCaster2D>();
            shadowCaster.enabled = false;
        }

        public void SetShadowActive(bool active)
        {
            shadowCaster.enabled = active;
        }
    }
}