using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _02._Script.UI.Forest {
    [Serializable]
    public struct ForestSettingsValues {
        public float volume;
        public float bgmVolume;
        public float sfxVolume;
        public float brightness;
        public int width;
        public int height;
        public bool fullscreen;

        public static float VolumeToDecibels(float value) {
            return value <= 0f ? -80f : Mathf.Max(-80f, 20f * Mathf.Log10(Mathf.Clamp01(value)));
        }

        public static Color BrightnessTint(float value) {
            value = Mathf.Clamp01(value);
            return value < .5f ? new Color(0, 0, 0, (.5f - value) * .9f) : new Color(1, 1, 1, (value - .5f) * .35f);
        }
    }

    // Shared settings modal for the main menu and gameplay ESC.
    public sealed class ForestSettings : MonoBehaviour {
        public GameObject modal;
        public Slider volume;
        public Slider bgmVolume;
        public Slider sfxVolume;
        public TMP_Text bgmValue;
        public TMP_Text sfxValue;
        public Button[] tabs;
        public GameObject[] tabPages;
        public Image[] tabIndicators;
        public Slider brightness;
        public TMP_Text volumeValue;
        public TMP_Text brightnessValue;
        public TMP_Text fullscreenValue;
        public TMP_Dropdown resolution;
        public Toggle fullscreen;
        public Button closeButton;
        public Button backButton;
        public Button applyButton;
        public Image brightnessOverlay;
        public CanvasGroup panelFade;

        private readonly List<Vector2Int> modes = new();
        private ForestSettingsValues applied;
        private CanvasGroup backgroundMenu;
        private ForestSettingsValues draft;
        private bool initialized;
        private AudioMixer mixer;
        private float openedAt;
        private bool ownsPause;
        private bool previewMode;
        private CursorLockMode previousCursorLock;
        private bool previousCursorVisible;
        private float previousMenuAlpha;
        private bool previousMenuInteractable;
        private bool previousMenuRaycasts;
        private GameObject previousSelection;
        private float previousTimeScale;
        public bool IsOpen => modal != null && modal.activeSelf;

        private void Update() {
            if (IsOpen && panelFade != null && !previewMode)
                panelFade.alpha = Mathf.Clamp01((Time.unscaledTime - openedAt) / .16f);
        }

        private void OnDisable() {
            if (initialized && IsOpen) Cancel();
            RestoreTime();
        }

        public event Action Closed;

        public void Initialize(AudioMixer audioMixer, bool preview = false) {
            if (initialized) return;
            initialized = true;
            previewMode = preview;
            mixer = audioMixer;
            applied = preview
                ? new ForestSettingsValues {
                    volume = .75f, bgmVolume = 1f, sfxVolume = 1f, brightness = .5f, width = 1920, height = 1080,
                    fullscreen = true
                }
                : ForestSettingsStore.Load();
            BuildModes();
            volume.onValueChanged.AddListener(ChangeVolume);
            if (bgmVolume != null) bgmVolume.onValueChanged.AddListener(ChangeBgm);
            if (sfxVolume != null) sfxVolume.onValueChanged.AddListener(ChangeSfx);
            if (tabs != null)
                for (var i = 0; i < tabs.Length; i++) {
                    var index = i;
                    tabs[i].onClick.AddListener(() => ShowTab(index));
                }

            brightness.onValueChanged.AddListener(ChangeBrightness);
            resolution.onValueChanged.AddListener(ChangeResolution);
            fullscreen.onValueChanged.AddListener(ChangeFullscreen);
            closeButton.onClick.AddListener(Cancel);
            backButton.onClick.AddListener(Cancel);
            applyButton.onClick.AddListener(Apply);
            Preview(applied);
            // SetResolution is deferred until Start/explicit initialization, never called from Awake.
            if (!preview && ForestSettingsStore.HasSavedDisplay) ApplyDisplay(applied);
            modal.SetActive(false);
        }

        private void BuildModes() {
            modes.Clear();
            foreach (var mode in Screen.resolutions) AddMode(mode.width, mode.height);
            AddMode(Screen.width, Screen.height);
            if (previewMode) {
                AddMode(1280, 720);
                AddMode(1600, 900);
                AddMode(1920, 1080);
                AddMode(1920, 1200);
            }

            if (modes.Count == 0) AddMode(1280, 720);
            // Discard a stored monitor mode that is no longer supported after changing displays.
            if (!modes.Contains(new Vector2Int(applied.width, applied.height))) {
                applied.width = Screen.width;
                applied.height = Screen.height;
                if (applied.width < 640 || applied.height < 480) {
                    applied.width = 1280;
                    applied.height = 720;
                    AddMode(1280, 720);
                }
            }

            modes.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            resolution.ClearOptions();
            var labels = new List<string>();
            foreach (var mode in modes) labels.Add($"{mode.x} × {mode.y}");
            resolution.AddOptions(labels);
        }

        private void AddMode(int width, int height) {
            var size = new Vector2Int(width, height);
            if (width >= 640 && height >= 480 && !modes.Contains(size)) modes.Add(size);
        }

        public void Open(CanvasGroup menu = null) {
            if (!initialized || IsOpen) return;
            backgroundMenu = menu;
            if (backgroundMenu != null) {
                previousMenuInteractable = backgroundMenu.interactable;
                previousMenuRaycasts = backgroundMenu.blocksRaycasts;
                previousMenuAlpha = backgroundMenu.alpha;
                backgroundMenu.interactable = false;
                backgroundMenu.blocksRaycasts = false;
                backgroundMenu.alpha = 0f;
            }

            // Respect Master changes made elsewhere before opening the shared modal.
            if (!previewMode && mixer != null && mixer.GetFloat("Master", out var db))
                applied.volume = db <= -80f ? 0f : Mathf.Clamp01(Mathf.Pow(10f, db / 20f));
            draft = applied;
            volume.SetValueWithoutNotify(draft.volume);
            if (bgmVolume != null) bgmVolume.SetValueWithoutNotify(draft.bgmVolume);
            if (sfxVolume != null) sfxVolume.SetValueWithoutNotify(draft.sfxVolume);
            brightness.SetValueWithoutNotify(draft.brightness);
            fullscreen.SetIsOnWithoutNotify(draft.fullscreen);
            resolution.SetValueWithoutNotify(Mathf.Max(0, modes.IndexOf(new Vector2Int(draft.width, draft.height))));
            resolution.RefreshShownValue();
            RefreshLabels();
            if (EventSystem.current != null) previousSelection = EventSystem.current.currentSelectedGameObject;
            if (!previewMode) {
                previousCursorVisible = Cursor.visible;
                previousCursorLock = Cursor.lockState;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            modal.SetActive(true);
            if (!previewMode) {
                previousTimeScale = Time.timeScale;
                ownsPause = true;
                Time.timeScale = 0f;
            }

            ShowTab(0);
            openedAt = Time.unscaledTime;
            if (panelFade != null) panelFade.alpha = previewMode ? 1f : 0f;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
        }

        public void Escape() {
            if (resolution.IsExpanded) {
                resolution.Hide();
                return;
            }

            Cancel();
        }

        public void ShowTab(int index) {
            if (tabPages == null || index < 0 || index >= tabPages.Length) return;
            resolution.Hide();
            for (var i = 0; i < tabPages.Length; i++) {
                tabPages[i].SetActive(i == index);
                if (tabIndicators != null && i < tabIndicators.Length)
                    tabIndicators[i].enabled = i == index;
            }
        }

        private void ChangeVolume(float v) {
            draft.volume = v;
            Preview(draft);
            RefreshLabels();
        }

        private void ChangeBgm(float v) {
            draft.bgmVolume = v;
            Preview(draft);
            RefreshLabels();
        }

        private void ChangeSfx(float v) {
            draft.sfxVolume = v;
            Preview(draft);
            RefreshLabels();
        }

        private void ChangeBrightness(float v) {
            draft.brightness = v;
            Preview(draft);
            RefreshLabels();
        }

        private void ChangeFullscreen(bool v) {
            draft.fullscreen = v;
            RefreshLabels();
        }

        private void ChangeResolution(int index) {
            if (index < 0 || index >= modes.Count) return;
            draft.width = modes[index].x;
            draft.height = modes[index].y;
        }

        private void RefreshLabels() {
            volumeValue.text = $"{Mathf.RoundToInt(draft.volume * 100)}%";
            if (bgmValue != null) bgmValue.text = $"{Mathf.RoundToInt(draft.bgmVolume * 100)}%";
            if (sfxValue != null) sfxValue.text = $"{Mathf.RoundToInt(draft.sfxVolume * 100)}%";
            brightnessValue.text = $"{Mathf.RoundToInt(draft.brightness * 100)}%";
            fullscreenValue.text = draft.fullscreen ? "Enabled" : "Disabled";
        }

        private void Preview(ForestSettingsValues values) {
            if (!previewMode && mixer != null &&
                !mixer.SetFloat("Master", ForestSettingsValues.VolumeToDecibels(values.volume)))
                Debug.LogWarning("AudioMixer에 노출된 Master 파라미터가 없습니다.", this);
            if (!previewMode && mixer != null) {
                mixer.SetFloat("BGM", ForestSettingsValues.VolumeToDecibels(values.bgmVolume));
                mixer.SetFloat("SFX", ForestSettingsValues.VolumeToDecibels(values.sfxVolume));
            }

            if (brightnessOverlay != null)
                brightnessOverlay.color = ForestSettingsValues.BrightnessTint(values.brightness);
        }

        public void Apply() {
            if (!IsOpen) return;
            applied = draft;
            Preview(applied);
            if (!previewMode) {
                ApplyDisplay(applied);
                ForestSettingsStore.Save(applied);
            }

            Dismiss();
        }

        private static void ApplyDisplay(ForestSettingsValues values) {
            Screen.SetResolution(values.width, values.height,
                values.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        }

        public void Cancel() {
            if (!IsOpen) return;
            draft = applied;
            Preview(applied);
            Dismiss();
        }

        private void Dismiss() {
            RestoreTime();
            resolution.Hide();
            modal.SetActive(false);
            if (backgroundMenu != null) {
                backgroundMenu.interactable = previousMenuInteractable;
                backgroundMenu.blocksRaycasts = previousMenuRaycasts;
                backgroundMenu.alpha = previousMenuAlpha;
                backgroundMenu = null;
            }

            if (!previewMode) {
                Cursor.visible = previousCursorVisible;
                Cursor.lockState = previousCursorLock;
            }

            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(
                    previousSelection != null && previousSelection.activeInHierarchy ? previousSelection : null);
            Closed?.Invoke();
        }

        private void RestoreTime() {
            if (!ownsPause) return;
            ownsPause = false;
            Time.timeScale = previousTimeScale;
        }
    }
}