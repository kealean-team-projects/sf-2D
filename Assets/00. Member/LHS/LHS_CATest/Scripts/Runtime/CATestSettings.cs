using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LHS_CATest {
    /// <summary>
    /// CATest 게임 설정 (타이틀 화면 설정 / 인게임 ESC 설정이 같은 값을 쓴다).
    ///
    /// ■ 저장: PlayerPrefs ("CATest.Settings.*") — 게임을 껐다 켜도 유지된다.
    /// ■ 적용
    ///   - 배경음/효과음 볼륨 : CATestAudio 가 재생할 때 곱함(효과음 볼륨은 환경음에도 적용)
    ///   - 전체 볼륨 : AudioListener.volume (모든 소리에 곱해지는 전역 볼륨. 원본 AudioMixer 설정과 겹쳐도 충돌 없음)
    ///   - 화면 밝기 : 우선순위 높은 전역 Volume 의 Lift Gamma Gain → Gain 의 밝기 오프셋(맵들의 색보정과 겹치지 않는 항목)
    ///   - 해상도/전체 화면 : Screen.SetResolution (에디터 Game 창에서는 해상도 변경이 반영되지 않음 — 빌드에서 동작)
    ///   - 머리 위 대사 표시 : CATestHUD.Say 가 이 값을 보고 대사를 띄울지 결정
    ///   - 화면 흔들림 : CATestCutscene.Shake 가 이 값을 봄
    /// ■ Changed 이벤트: 값이 바뀌면 UI(설정 패널)가 다시 그린다.
    /// </summary>
    public static class CATestSettings {
        private const string P = "CATest.Settings.";

        public static float MasterVolume { get; private set; } = 1f;
        public static float BgmVolume { get; private set; } = 0.8f;   // 배경음(CATestAudio 배경음 채널)
        public static float SfxVolume { get; private set; } = 1f;     // 효과음 + 환경음
        public static float Brightness { get; private set; }          // -1 ~ 1 (0 = 기본)
        public static bool ShowPlayerLines { get; private set; } = true;
        public static bool ScreenShake { get; private set; } = true;
        public static bool Fullscreen { get; private set; } = true;
        public static Vector2Int Resolution { get; private set; }

        public static event Action Changed;

        private static bool _loaded;
        private static Volume _brightnessVolume;
        private static LiftGammaGain _gain;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() {
            _loaded = false;
            _brightnessVolume = null;
            _gain = null;
            Changed = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ApplyOnStart() {
            EnsureLoaded();
            ApplyAll(false);
        }

        public static void EnsureLoaded() {
            if (_loaded) return;
            _loaded = true;
            MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(P + "Volume", 1f));
            Brightness = Mathf.Clamp(PlayerPrefs.GetFloat(P + "Brightness", 0f), -1f, 1f);
            BgmVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(P + "Bgm", 0.8f));
            SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(P + "Sfx", 1f));
            ShowPlayerLines = PlayerPrefs.GetInt(P + "PlayerLines", 1) == 1;
            ScreenShake = PlayerPrefs.GetInt(P + "Shake", 1) == 1;
            Fullscreen = PlayerPrefs.GetInt(P + "Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
            Resolution = new Vector2Int(PlayerPrefs.GetInt(P + "Width", Screen.currentResolution.width),
                PlayerPrefs.GetInt(P + "Height", Screen.currentResolution.height));
        }

        public static void Save() {
            PlayerPrefs.SetFloat(P + "Volume", MasterVolume);
            PlayerPrefs.SetFloat(P + "Brightness", Brightness);
            PlayerPrefs.SetFloat(P + "Bgm", BgmVolume);
            PlayerPrefs.SetFloat(P + "Sfx", SfxVolume);
            PlayerPrefs.SetInt(P + "PlayerLines", ShowPlayerLines ? 1 : 0);
            PlayerPrefs.SetInt(P + "Shake", ScreenShake ? 1 : 0);
            PlayerPrefs.SetInt(P + "Fullscreen", Fullscreen ? 1 : 0);
            PlayerPrefs.SetInt(P + "Width", Resolution.x);
            PlayerPrefs.SetInt(P + "Height", Resolution.y);
            PlayerPrefs.Save();
        }

        // ───────────── 값 바꾸기 (UI 에서 호출) ─────────────
        public static void SetVolume(float v) { EnsureLoaded(); MasterVolume = Mathf.Clamp01(v); ApplyAudio(); Changed?.Invoke(); }
        public static void SetBgmVolume(float v) { EnsureLoaded(); BgmVolume = Mathf.Clamp01(v); Changed?.Invoke(); }
        public static void SetSfxVolume(float v) { EnsureLoaded(); SfxVolume = Mathf.Clamp01(v); Changed?.Invoke(); }
        public static void SetBrightness(float v) { EnsureLoaded(); Brightness = Mathf.Clamp(v, -1f, 1f); ApplyBrightness(); Changed?.Invoke(); }

        public static void SetShowPlayerLines(bool on) {
            EnsureLoaded();
            ShowPlayerLines = on;
            if (!on) CATestHUD.HideSpeechNow();
            Changed?.Invoke();
        }

        public static void SetScreenShake(bool on) { EnsureLoaded(); ScreenShake = on; Changed?.Invoke(); }
        public static void SetFullscreen(bool on) { EnsureLoaded(); Fullscreen = on; ApplyDisplay(); Changed?.Invoke(); }
        public static void SetResolution(Vector2Int r) { EnsureLoaded(); Resolution = r; ApplyDisplay(); Changed?.Invoke(); }

        public static void ResetToDefault() {
            EnsureLoaded();
            MasterVolume = 1f;
            BgmVolume = 0.8f;
            SfxVolume = 1f;
            Brightness = 0f;
            ShowPlayerLines = true;
            ScreenShake = true;
            ApplyAll(false);
            Changed?.Invoke();
        }

        // ───────────── 적용 ─────────────
        public static void ApplyAll(bool display) {
            ApplyAudio();
            ApplyBrightness();
            if (display) ApplyDisplay();
        }

        private static void ApplyAudio() => AudioListener.volume = MasterVolume;

        private static void ApplyDisplay() {
#if !UNITY_EDITOR
            if (Resolution.x >= 640 && Resolution.y >= 360)
                Screen.SetResolution(Resolution.x, Resolution.y, Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
            else Screen.fullScreenMode = Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
#endif
        }

        /// <summary>밝기: 씬이 바뀌어도 남는 전역 Volume 하나를 만들어 Post Exposure 만 조절한다.</summary>
        private static void ApplyBrightness() {
            if (_brightnessVolume == null) {
                var go = new GameObject("CATest_BrightnessVolume");
                UnityEngine.Object.DontDestroyOnLoad(go);
                _brightnessVolume = go.AddComponent<Volume>();
                _brightnessVolume.isGlobal = true;
                _brightnessVolume.priority = 1000f; // 맵의 다른 볼륨보다 나중에 섞여 최종 밝기를 정함
                var profile = ScriptableObject.CreateInstance<VolumeProfile>();
                _gain = profile.Add<LiftGammaGain>(true);
                _brightnessVolume.sharedProfile = profile;
            }
            if (_gain == null) return;
            // 맵마다 Color Adjustments(Post Exposure)를 이미 쓰고 있어서, 그걸 덮어쓰면 맵 분위기가 바뀐다.
            // 그래서 맵들이 쓰지 않는 Lift Gamma Gain 의 Gain(w = 전체 밝기 오프셋)만 조절한다 → 맵 색보정 위에 "곱해지듯" 얹힘.
            _gain.gain.overrideState = true;
            _gain.gain.value = new Vector4(1f, 1f, 1f, Brightness * 0.45f);
        }
    }
}
