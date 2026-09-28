using System.Collections.Generic;
using csiimnida.CSILib.SoundManager.RunTime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace _02._Script.UI {
    public static class UIAudio {
        private static readonly HashSet<Button> BoundButtons = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            BoundButtons.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize() {
            SceneManager.sceneLoaded += OnSceneLoaded;
            BindButtons();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => BindButtons();

        public static void BindButtons() {
            BoundButtons.RemoveWhere(button => button == null);
            foreach (var button in Object.FindObjectsByType<Button>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                if (!BoundButtons.Add(button)) continue;
                // onClick also covers keyboard/controller submit and rejects disabled buttons.
                button.onClick.AddListener(() => Play("Click"));
            }
        }

        public static void Play(string soundName) {
            var manager = Object.FindAnyObjectByType<SoundManager>();
            if (manager == null) return;
            var source = manager.PlayTrackedSound(soundName);
            if (source == null) return;
            source.ignoreListenerPause = true;
            source.loop = false;
            source.spatialBlend = 0f;
        }
    }
}
