using _02._Script._04_Interaction;
using UnityEngine;

namespace _02._Script.Boss
{
    public class BossRoom : MonoBehaviour
    {
        [SerializeField] private Transform light;
        [SerializeField] private Boss boss;
        
        private InteractLight[] lights;

        public bool IsStarted { get; private set; }

        private void Awake()
        {
            lights = light.GetComponentsInChildren<InteractLight>();
            
            foreach (InteractLight roomLight in lights)
                roomLight.TurnOff();
        }

        public void Begin()
        {
            if (IsStarted) return;
            IsStarted = true;

            foreach (InteractLight roomLight in lights)
                roomLight.TurnOn();

            Debug.Log("보스방 시작", this);
            
            boss.Begin();
        }
        
        public void SetLightBrightness(float ratio)
        {
            foreach (InteractLight roomLight in lights)
                roomLight.SetBrightness(ratio);
        }
    }
}