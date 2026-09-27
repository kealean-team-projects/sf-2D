using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace _02._Script.UI {
    public class VolumeSlider : MonoBehaviour {
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private string param;

        public AudioMixer Mixer => mixer;

        public void SliderChange() {
            float value = volumeSlider.value;
            float decibels = value <= 0f
                ? -80f
                : Mathf.Log10(value) * 20f;

            mixer.SetFloat(param, decibels);
        }
    }
}
