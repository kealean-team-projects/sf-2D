using csiimnida.CSILib.SoundManager.RunTime;
using UnityEngine;

namespace _02._Script._05_Managers {
    [DisallowMultipleComponent]
    public sealed class StageAudio : MonoBehaviour {
        [SerializeField] private string backgroundMusic = "BG_1";
        private SoundManager _manager;
        private bool _started;
        private Object _overrideOwner;
        private string _overrideMusic;
        private Object _pauseOwner;

        private void Start() {
            _started = true;
            PlayMusic();
        }

        private void OnEnable() {
            if (_started) PlayMusic();
        }

        private void PlayMusic() {
            if (!FindManager()) return;
            _manager.PlayBgm(_overrideOwner != null ? _overrideMusic : backgroundMusic, this);
            if (_pauseOwner != null) _manager.PauseBgm(this);
        }

        public void PauseMusic(Object owner) {
            if (!isActiveAndEnabled || owner == null || !FindManager()) return;
            _pauseOwner = owner;
            _manager.PauseBgm(this);
        }

        public void ResumeMusic(Object owner) {
            if (!ReferenceEquals(_pauseOwner, owner)) return;
            _pauseOwner = null;
            if (isActiveAndEnabled && _manager != null && _manager.IsBgmOwnedBy(this))
                PlayMusic();
        }

        public bool OverrideMusic(string soundName, Object owner) {
            if (!isActiveAndEnabled || owner == null || string.IsNullOrWhiteSpace(soundName) || !FindManager())
                return false;
            if (!_manager.PlayBgm(soundName, this)) return false;
            _overrideOwner = owner;
            _overrideMusic = soundName;
            return true;
        }

        public void RestoreMusic(Object owner) {
            if (!ReferenceEquals(_overrideOwner, owner)) return;
            _overrideOwner = null;
            _overrideMusic = null;
            // An old room must never take music back from a newly loaded stage.
            if (isActiveAndEnabled && _manager != null && _manager.IsBgmOwnedBy(this))
                _manager.PlayBgm(backgroundMusic, this);
        }

        private bool FindManager() {
            if (_manager == null) _manager = FindAnyObjectByType<SoundManager>();
            if (_manager == null) {
                Debug.LogWarning("StageAudio requires the shared SoundManager in CoreScene.", this);
                return false;
            }
            return true;
        }

        private void OnDisable() {
            if (_manager != null) _manager.StopBgm(this);
            _overrideOwner = null;
            _overrideMusic = null;
            _pauseOwner = null;
        }
    }
}
