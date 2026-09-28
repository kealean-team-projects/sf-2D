using System;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

namespace _00._Member.LHS.Script.DevLib.SoundSystem.Runtime {
    [RequireComponent(typeof(AudioSource))]
    public sealed class SoundPlayer : MonoBehaviour {
        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField] private AudioMixerGroup musicGroup;

        private AudioSource _audioSource;
        private SoundClipSO _currentClipData;
        private float _endTime;

        private bool _isPlaying;

        private float _startTime;

        private void Awake() {
            _audioSource = GetComponent<AudioSource>();

            _audioSource.playOnAwake = false;

            // 특정 구간 반복을 직접 처리하기 때문에
            // AudioSource 자체 Loop는 사용하지 않는다.
            _audioSource.loop = false;
        }

        private void Update() {
            if (!_isPlaying || _currentClipData == null)
                return;

            if (_currentClipData.isLoop) {
                HandleLoop();
                return;
            }

            HandleFinish();
        }

        public event Action<SoundPlayer> OnSoundFinished;

        public void PlaySound(SoundClipSO clipData) {
            if (clipData == null) {
                Debug.LogWarning("[SoundPlayer] SoundClipSO is null.", this);
                return;
            }

            if (clipData.clip == null) {
                Debug.LogWarning(
                    $"[SoundPlayer] AudioClip is null : {clipData.name}",
                    clipData);

                return;
            }

            StopCurrentSound();

            _currentClipData = clipData;

            ConfigureAudioSource(clipData);
            ConfigurePlayRange(clipData);

            SetPlaybackTime(_startTime);

            _isPlaying = true;
            _audioSource.Play();
        }

        private void ConfigureAudioSource(SoundClipSO clipData) {
            _audioSource.clip = clipData.clip;
            _audioSource.volume = clipData.volume;

            var pitch = clipData.pitch;

            if (clipData.randomizePitch)
                pitch += Random.Range(
                    -clipData.randomPitchModifier,
                    clipData.randomPitchModifier);

            _audioSource.pitch = Mathf.Clamp(pitch, 0.1f, 3f);

            _audioSource.outputAudioMixerGroup = clipData.audioType switch {
                AudioType.Sfx => sfxGroup,
                AudioType.Music => musicGroup,
                _ => null
            };
        }

        private void ConfigurePlayRange(SoundClipSO clipData) {
            var clipLength = clipData.clip.length;

            _startTime = Mathf.Clamp(
                clipData.startTime,
                0f,
                clipLength);

            _endTime = clipData.endTime;

            if (_endTime <= _startTime)
                _endTime = clipLength;

            _endTime = Mathf.Clamp(
                _endTime,
                _startTime,
                clipLength);
        }

        private void HandleLoop() {
            if (_audioSource.isPlaying &&
                _audioSource.time < _endTime)
                return;

            SetPlaybackTime(_startTime);
            _audioSource.Play();
        }

        private void HandleFinish() {
            var reachedEnd =
                _audioSource.isPlaying &&
                _audioSource.time >= _endTime;

            var naturallyStopped =
                !_audioSource.isPlaying;

            if (!reachedEnd && !naturallyStopped)
                return;

            CompleteSound();
        }

        private void CompleteSound() {
            _audioSource.Stop();

            _isPlaying = false;
            _currentClipData = null;

            OnSoundFinished?.Invoke(this);
        }

        private void StopCurrentSound() {
            if (_audioSource != null)
                _audioSource.Stop();

            _isPlaying = false;
            _currentClipData = null;
        }

        private void SetPlaybackTime(float time) {
            var clip = _audioSource.clip;

            if (clip == null || clip.samples <= 0)
                return;

            var sample =
                Mathf.RoundToInt(time * clip.frequency);

            sample = Mathf.Clamp(
                sample,
                0,
                clip.samples - 1);

            _audioSource.timeSamples = sample;
        }

        public void ForceStopSound() {
            StopCurrentSound();
        }
    }
}