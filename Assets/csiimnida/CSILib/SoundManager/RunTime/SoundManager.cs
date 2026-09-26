using System.Collections;
using System.Collections.Generic;
using CSILib.SoundManager.RunTime;
using UnityEngine;
using UnityEngine.Audio;

namespace csiimnida.CSILib.SoundManager.RunTime {
    public class SoundManager : MonoSingleton<SoundManager> {
        [SerializeField] private SoundListSo _soundListSo;
        [SerializeField] private AudioMixer _mixer;

        private readonly HashSet<AudioSource> _sources = new();
        private AudioSource _bgmSource;
        private Object _bgmOwner;
        private string _bgmName;

        public void PlaySound(string soundName) {
            PlayTrackedSound(soundName);
        }

        public AudioSource PlayTrackedSound(string soundName) {
            if (!TryGetSound(soundName, out var sound)) return null;
            return CreateSource(soundName, sound, sound.loop);
        }

        public void StopSound(AudioSource source) {
            if (source == null || !_sources.Remove(source)) return;
            if (source == _bgmSource) {
                _bgmSource = null;
                _bgmOwner = null;
                _bgmName = null;
            }

            source.Stop();
            Destroy(source.gameObject);
        }

        public bool PlayBgm(string soundName, Object owner) {
            if (owner == null || !TryGetSound(soundName, out var sound)) return false;
            if (_bgmSource != null && _bgmOwner == owner && _bgmName == soundName) return true;

            StopSound(_bgmSource);
            _bgmSource = CreateSource(soundName, sound, true);
            _bgmOwner = owner;
            _bgmName = soundName;
            return true;
        }

        public void StopBgm(Object owner) {
            // Reference identity also works while a Unity owner is being destroyed.
            if (ReferenceEquals(_bgmOwner, owner)) StopSound(_bgmSource);
        }

        private bool TryGetSound(string soundName, out SoundSo sound) {
            sound = null;
            if (!isActiveAndEnabled || string.IsNullOrEmpty(soundName)) return false;
            if (_soundListSo == null || _soundListSo.SoundsDictionary == null ||
                !_soundListSo.SoundsDictionary.TryGetValue(soundName, out sound) ||
                sound == null || sound.clip == null) {
                Debug.LogWarning($"Sound '{soundName}' has no valid SoundListSo entry or clip.", this);
                return false;
            }

            return true;
        }

        private AudioSource CreateSource(string soundName, SoundSo sound, bool loop) {
            var obj = new GameObject(soundName + " Sound");
            obj.transform.SetParent(transform, false);
            var source = obj.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.clip = sound.clip;
            source.loop = loop;
            source.priority = sound.Priority;
            source.volume = sound.volume;
            source.pitch = sound.RandomPitch ? Random.Range(sound.MinPitch, sound.MaxPitch) : sound.pitch;
            source.panStereo = sound.stereoPan;
            source.spatialBlend = sound.SpatialBlend;

            if (_mixer != null) {
                var groupName = sound.soundType == SoundType.BGM ? "BGM" : "SFX";
                var groups = _mixer.FindMatchingGroups(groupName);
                if (groups.Length == 0) groups = _mixer.FindMatchingGroups("Master");
                if (groups.Length > 0) source.outputAudioMixerGroup = groups[0];
            }

            if (source.pitch < 0f) source.timeSamples = Mathf.Max(0, source.clip.samples - 1);
            _sources.Add(source);
            source.Play();
            if (!loop) StartCoroutine(CleanupWhenFinished(source));
            return source;
        }

        private IEnumerator CleanupWhenFinished(AudioSource source) {
            // Poll actual playback so pitch changes and Time.timeScale do not cut clips short.
            yield return null;
            while (source != null && _sources.Contains(source)) {
                if ((!AudioListener.pause || source.ignoreListenerPause) &&
                    source.pitch != 0f && !source.isPlaying) {
                    StopSound(source);
                    yield break;
                }

                yield return null;
            }
        }

        private void OnDisable() {
            StopAllCoroutines();
            foreach (var source in _sources) {
                if (source == null) continue;
                source.Stop();
                Destroy(source.gameObject);
            }

            _sources.Clear();
            _bgmSource = null;
            _bgmOwner = null;
            _bgmName = null;
        }
    }

    public enum SoundType {
        BGM,
        SFX
    }
}
