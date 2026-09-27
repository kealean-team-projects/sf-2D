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
        public float brightness;
        public int width;
        public int height;
        public bool fullscreen;

        public static float VolumeToDecibels(float value) => value <= 0f ? -80f : Mathf.Max(-80f, 20f * Mathf.Log10(Mathf.Clamp01(value)));
        public static Color BrightnessTint(float value) {
            value=Mathf.Clamp01(value);
            return value<.5f ? new Color(0,0,0,(.5f-value)*.9f) : new Color(1,1,1,(value-.5f)*.35f);
        }
    }

    // Main-menu modal. Gameplay ESC UI remains owned by UIManager's existing settingsPanel.
    public sealed class ForestSettings : MonoBehaviour {
        public GameObject modal;
        public Slider volume;
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

        private readonly List<Vector2Int> modes=new List<Vector2Int>();
        private AudioMixer mixer;
        private ForestSettingsValues applied;
        private ForestSettingsValues draft;
        private bool initialized;
        private bool previewMode;
        private GameObject previousSelection;
        private bool previousCursorVisible;
        private CursorLockMode previousCursorLock;
        private float openedAt;
        private CanvasGroup backgroundMenu;
        private bool previousMenuInteractable;
        private bool previousMenuRaycasts;
        private float previousMenuAlpha;
        public bool IsOpen => modal!=null && modal.activeSelf;
        public event Action Closed;

        public void Initialize(AudioMixer audioMixer, bool preview=false) {
            if(initialized) return;
            initialized=true;
            previewMode=preview;
            mixer=audioMixer;
            applied=preview
                ? new ForestSettingsValues { volume=.75f, brightness=.5f, width=1920, height=1080, fullscreen=true }
                : ForestSettingsStore.Load();
            BuildModes();
            volume.onValueChanged.AddListener(ChangeVolume);
            brightness.onValueChanged.AddListener(ChangeBrightness);
            resolution.onValueChanged.AddListener(ChangeResolution);
            fullscreen.onValueChanged.AddListener(ChangeFullscreen);
            closeButton.onClick.AddListener(Cancel);
            backButton.onClick.AddListener(Cancel);
            applyButton.onClick.AddListener(Apply);
            Preview(applied);
            // SetResolution is deferred until Start/explicit initialization, never called from Awake.
            if(!preview && ForestSettingsStore.HasSavedDisplay) ApplyDisplay(applied);
            modal.SetActive(false);
        }

        private void BuildModes() {
            modes.Clear();
            foreach(var mode in Screen.resolutions) AddMode(mode.width,mode.height);
            AddMode(Screen.width,Screen.height);
            if(previewMode) { AddMode(1280,720); AddMode(1600,900); AddMode(1920,1080); AddMode(1920,1200); }
            if(modes.Count==0) AddMode(1280,720);
            // Discard a stored monitor mode that is no longer supported after changing displays.
            if(!modes.Contains(new Vector2Int(applied.width,applied.height))) {
                applied.width=Screen.width; applied.height=Screen.height;
                if(applied.width<640||applied.height<480) { applied.width=1280; applied.height=720; AddMode(1280,720); }
            }
            modes.Sort((a,b)=>a.x!=b.x?a.x.CompareTo(b.x):a.y.CompareTo(b.y));
            resolution.ClearOptions();
            var labels=new List<string>();
            foreach(var mode in modes) labels.Add($"{mode.x} × {mode.y}");
            resolution.AddOptions(labels);
        }

        private void AddMode(int width,int height) {
            var size=new Vector2Int(width,height);
            if(width>=640 && height>=480 && !modes.Contains(size)) modes.Add(size);
        }

        public void Open(CanvasGroup menu=null) {
            if(!initialized || IsOpen) return;
            backgroundMenu=menu;
            if(backgroundMenu!=null) {
                previousMenuInteractable=backgroundMenu.interactable;
                previousMenuRaycasts=backgroundMenu.blocksRaycasts;
                previousMenuAlpha=backgroundMenu.alpha;
                backgroundMenu.interactable=false;
                backgroundMenu.blocksRaycasts=false;
                backgroundMenu.alpha=0f;
            }
            // The separate gameplay ESC panel may have changed Master since this panel last opened.
            if(!previewMode && mixer!=null && mixer.GetFloat("Master",out float db))
                applied.volume=db<=-80f?0f:Mathf.Clamp01(Mathf.Pow(10f,db/20f));
            draft=applied;
            volume.SetValueWithoutNotify(draft.volume);
            brightness.SetValueWithoutNotify(draft.brightness);
            fullscreen.SetIsOnWithoutNotify(draft.fullscreen);
            resolution.SetValueWithoutNotify(Mathf.Max(0,modes.IndexOf(new Vector2Int(draft.width,draft.height))));
            resolution.RefreshShownValue();
            RefreshLabels();
            if(EventSystem.current!=null) previousSelection=EventSystem.current.currentSelectedGameObject;
            if(!previewMode) {
                previousCursorVisible=Cursor.visible; previousCursorLock=Cursor.lockState;
                Cursor.lockState=CursorLockMode.None; Cursor.visible=true;
            }
            modal.SetActive(true);
            openedAt=Time.unscaledTime;
            if(panelFade!=null) panelFade.alpha=previewMode?1f:0f;
            if(EventSystem.current!=null) EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
        }

        private void Update() {
            if(IsOpen && panelFade!=null && !previewMode)
                panelFade.alpha=Mathf.Clamp01((Time.unscaledTime-openedAt)/.16f);
        }

        public void Escape() {
            if(resolution.IsExpanded) { resolution.Hide(); return; }
            Cancel();
        }

        private void ChangeVolume(float v) { draft.volume=v; Preview(draft); RefreshLabels(); }
        private void ChangeBrightness(float v) { draft.brightness=v; Preview(draft); RefreshLabels(); }
        private void ChangeFullscreen(bool v) { draft.fullscreen=v; RefreshLabels(); }
        private void ChangeResolution(int index) {
            if(index<0||index>=modes.Count) return;
            draft.width=modes[index].x; draft.height=modes[index].y;
        }
        private void RefreshLabels() {
            volumeValue.text=$"{Mathf.RoundToInt(draft.volume*100)}%";
            brightnessValue.text=$"{Mathf.RoundToInt(draft.brightness*100)}%";
            fullscreenValue.text=draft.fullscreen?"Enabled":"Disabled";
        }

        private void Preview(ForestSettingsValues values) {
            if(!previewMode && mixer!=null && !mixer.SetFloat("Master",ForestSettingsValues.VolumeToDecibels(values.volume)))
                Debug.LogWarning("AudioMixer에 노출된 Master 파라미터가 없습니다.",this);
            if(brightnessOverlay!=null) brightnessOverlay.color=ForestSettingsValues.BrightnessTint(values.brightness);
        }

        public void Apply() {
            if(!IsOpen) return;
            applied=draft;
            Preview(applied);
            if(!previewMode) {
                ApplyDisplay(applied);
                ForestSettingsStore.Save(applied);
            }
            Dismiss();
        }

        private static void ApplyDisplay(ForestSettingsValues values) {
            Screen.SetResolution(values.width,values.height,values.fullscreen?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed);
        }

        public void Cancel() {
            if(!IsOpen) return;
            draft=applied;
            Preview(applied);
            Dismiss();
        }

        private void Dismiss() {
            resolution.Hide();
            modal.SetActive(false);
            if(backgroundMenu!=null) {
                backgroundMenu.interactable=previousMenuInteractable;
                backgroundMenu.blocksRaycasts=previousMenuRaycasts;
                backgroundMenu.alpha=previousMenuAlpha;
                backgroundMenu=null;
            }
            if(!previewMode) { Cursor.visible=previousCursorVisible; Cursor.lockState=previousCursorLock; }
            if(EventSystem.current!=null)
                EventSystem.current.SetSelectedGameObject(previousSelection!=null && previousSelection.activeInHierarchy?previousSelection:null);
            Closed?.Invoke();
        }

        private void OnDisable() {
            if(initialized && IsOpen) Cancel();
        }
    }
}
