using System.Collections.Generic;
using UnityEngine;

namespace csiimnida.CSILib.SoundManager.RunTime {
    [CreateAssetMenu(fileName = "SoundListSO", menuName = "SO/Sound/SoundListSO")]
    public class SoundListSo : ScriptableObject {
        [SerializeField] private List<SoundSo> Sounds = new();

        [System.NonSerialized] public Dictionary<string, SoundSo> SoundsDictionary;

        private void OnEnable() {
            SoundsDictionary = new Dictionary<string, SoundSo>();
            if (Sounds == null)
                return;
            foreach (var soundSo in Sounds) {
                if (soundSo == null || string.IsNullOrEmpty(soundSo.soundName)) continue;
                SoundsDictionary[soundSo.soundName] = soundSo;
            }
        }

        public void AddSound(SoundSo soundSo) {
            Sounds.Add(soundSo);
        }

        public List<SoundSo> GetSoundList() {
            return Sounds;
        }

        public void RemoveSound(SoundSo so) {
            if (so != null) Sounds.Remove(so);
        }
    }
}
