using csiimnida.CSILib.SoundManager.RunTime;
using UnityEngine;

namespace _02._Script._05_Managers {
    [DisallowMultipleComponent]
    public sealed class StageAudio : MonoBehaviour {
        [SerializeField] private string backgroundMusic = "BG_1";
        private SoundManager _manager;
        private bool _started;

        private void Start() {
            _started = true;
            PlayMusic();
        }

        private void OnEnable() {
            if (_started) PlayMusic();
        }

        private void PlayMusic() {
            if (_manager == null) _manager = FindAnyObjectByType<SoundManager>();
            if (_manager == null) {
                Debug.LogWarning("StageAudio requires the shared SoundManager in CoreScene.", this);
                return;
            }
            _manager.PlayBgm(backgroundMusic, this);
        }

        private void OnDisable() {
            if (_manager != null) _manager.StopBgm(this);
        }
    }
}
