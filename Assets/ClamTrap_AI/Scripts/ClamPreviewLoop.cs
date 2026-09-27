using UnityEngine;

namespace ClamTrapArt
{
    public sealed class ClamPreviewLoop : MonoBehaviour
    {
        public ClamTrapPlayer trap;
        [Min(0)] public float openHoldSeconds = 0.55f;
        float readyTime;
        void Update()
        {
            if (!trap) return;
            if (trap.IsReady) { readyTime += Time.deltaTime; if (readyTime >= openHoldSeconds) { trap.Close(); readyTime=0; } }
            else readyTime=0;
        }
    }
}
