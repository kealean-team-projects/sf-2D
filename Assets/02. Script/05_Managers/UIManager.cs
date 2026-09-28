using System;
using _02._Script._01_Players;
using _02._Script._01_Players.Interface;
using _02._Script.UI;
using _02._Script.UI.Forest;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _02._Script._05_Managers
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }
        
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private StaminaUI staminaUI;
        [SerializeField] private ForestSettings forestSettings;
        [SerializeField] private AudioHighPassFilter settingsFilter;
        
        private IStats subscribedStats;

        private void HandleStaminaChanged(float ratio)
        {
            staminaUI.SetStamina(ratio);
        }

        private Player currentPlayer;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }

            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            if (Instance != this) return;
            ClearSettingsFilter();
            InitializeSettings();
            UIAudio.BindButtons();
        }

        private void InitializeSettings() {
            var legacyVolume = settingsPanel != null ? settingsPanel.GetComponentInChildren<VolumeSlider>(true) : null;
            var mixer = legacyVolume != null ? legacyVolume.Mixer : null;
            if (settingsPanel != null) settingsPanel.SetActive(false);
            forestSettings.Initialize(mixer);
            forestSettings.Closed += ClearSettingsFilter;
        }

        public void RegisterPlayer(Player player)
        {
            if (player == null) return;

            if (subscribedStats != null)
                subscribedStats.OnStaminaChanged -= HandleStaminaChanged;

            currentPlayer = player;
            subscribedStats = player.Stats;
            subscribedStats.OnStaminaChanged += HandleStaminaChanged;

            staminaUI.gameObject.SetActive(true);
            staminaUI.SetTarget(player.transform);
            HandleStaminaChanged(subscribedStats.StaminaRatio);
        }
        
        public void OpenSettings()
        {
            forestSettings.Open();
            if (settingsFilter != null) settingsFilter.enabled = forestSettings.IsOpen;
        }

        public void CloseSettings()
        {
            forestSettings.Cancel();
            ClearSettingsFilter();
        }

        public void OpenMainMenuSettings(CanvasGroup menu = null) {
            forestSettings.Open(menu);
            if (settingsFilter != null) settingsFilter.enabled = forestSettings.IsOpen;
        }

        private void ClearSettingsFilter() {
            if (settingsFilter != null) settingsFilter.enabled = false;
        }

        public void CloseMainMenuSettings() {
            if (forestSettings != null) forestSettings.Cancel();
        }

        private void OnDestroy() {
            if (Instance != this) return;
            if (subscribedStats != null)
                subscribedStats.OnStaminaChanged -= HandleStaminaChanged;
            
            CloseMainMenuSettings();
            if (forestSettings != null) forestSettings.Closed -= ClearSettingsFilter;
            Instance = null;
        }
        
        private void Update() {
            if (Instance == this && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                UIAudio.Play("OptionSound");
                if (forestSettings != null && forestSettings.IsOpen)
                    forestSettings.Escape();
                else if (UnityEngine.SceneManagement.SceneManager.GetSceneByName("MainMenu").isLoaded)
                    return;
                else
                    OpenSettings();
            }
        }
    }
}
