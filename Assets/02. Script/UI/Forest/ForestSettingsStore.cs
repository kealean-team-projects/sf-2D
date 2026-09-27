using UnityEngine;

namespace _02._Script.UI.Forest {
    // Persistence is separate from the view's draft/apply/cancel behavior.
    public static class ForestSettingsStore {
        private const string Prefix = "SF.ForestSettings.";
        public static bool HasSavedDisplay => PlayerPrefs.HasKey(Prefix + "Width");

        public static ForestSettingsValues Load() {
            return new ForestSettingsValues {
                volume = Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "Volume", 1f)),
                brightness = Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "Brightness", .5f)),
                width = Mathf.Max(640, PlayerPrefs.GetInt(Prefix + "Width", Screen.width)),
                height = Mathf.Max(480, PlayerPrefs.GetInt(Prefix + "Height", Screen.height)),
                fullscreen = PlayerPrefs.GetInt(Prefix + "Fullscreen", Screen.fullScreen ? 1 : 0) == 1
            };
        }

        public static void Save(ForestSettingsValues values) {
            PlayerPrefs.SetFloat(Prefix + "Volume", values.volume);
            PlayerPrefs.SetFloat(Prefix + "Brightness", values.brightness);
            PlayerPrefs.SetInt(Prefix + "Width", values.width);
            PlayerPrefs.SetInt(Prefix + "Height", values.height);
            PlayerPrefs.SetInt(Prefix + "Fullscreen", values.fullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
