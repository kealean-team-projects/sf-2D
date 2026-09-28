using UnityEngine;

namespace _00._Member.LHS.Script.DevLib.SoundSystem.Runtime {
    public enum AudioType {
        Sfx,
        Music
    }

    [CreateAssetMenu(
        fileName = "SoundClip",
        menuName = "Lib/Sound/Clip Data",
        order = 0)]
    public sealed class SoundClipSO : ScriptableObject {
        public AudioType audioType;
        public AudioClip clip;

        public bool isLoop;
        public bool randomizePitch;

        [Range(0f, 1f)] public float randomPitchModifier = 0.1f;

        [Range(0f, 1f)] public float volume = 1f;

        [Range(0.1f, 3f)] public float pitch = 1f;

        [Min(0f)] public float startTime;

        [Min(0f)] public float endTime;

#if UNITY_EDITOR
        private void OnValidate() {
            if (clip == null) {
                startTime = 0f;
                endTime = 0f;
                return;
            }

            startTime = Mathf.Clamp(startTime, 0f, clip.length);

            if (endTime <= 0f)
                endTime = clip.length;

            endTime = Mathf.Clamp(endTime, startTime, clip.length);
        }
#endif
    }
}