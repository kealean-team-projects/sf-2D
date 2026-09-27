using System;
using _02._Script._01_Players;
using _02._Script.UI;
using _02._Script.UI.Forest;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _02._Script._05_Managers
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }
        
        [SerializeField] private StaminaHUD staminaHUD;
        [SerializeField] private GameObject settingsPanel;

        private Player currentPlayer;
        [SerializeField] private ForestSettings forestSettings;
        
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
        
        private void Start() {
            if (Instance != this) return;
            InitializeSettings();
            if (currentPlayer == null && staminaHUD != null) staminaHUD.gameObject.SetActive(false);
        }

        private void InitializeSettings() {
            var legacyVolume = settingsPanel != null ? settingsPanel.GetComponentInChildren<VolumeSlider>(true) : null;
            var mixer = legacyVolume != null ? legacyVolume.Mixer : null;
            if (settingsPanel != null) settingsPanel.SetActive(false);
            forestSettings.Initialize(mixer);
        }

        public void RegisterPlayer(Player player) 
        {
            if (player == null) return;

            currentPlayer = player;
            currentPlayer.SetHUD(staminaHUD);
            staminaHUD.gameObject.SetActive(true);
        }
        
        public void OpenSettings() {
            settingsPanel.SetActive(true);
        }

        public void CloseSettings() {
            settingsPanel.SetActive(false);
        }

        public void OpenMainMenuSettings(CanvasGroup menu = null) {
            forestSettings.Open(menu);
        }

        public void CloseMainMenuSettings() {
            if (forestSettings != null) forestSettings.Cancel();
        }

        private void OnDestroy() {
            if (Instance != this) return;
            CloseMainMenuSettings();
            Instance = null;
        }
        
        private void Update() {
            if (Instance == this && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (forestSettings != null && forestSettings.IsOpen)
                    forestSettings.Escape();
                else if (UnityEngine.SceneManagement.SceneManager.GetSceneByName("MainMenu").isLoaded)
                    return;
                else if (settingsPanel.activeSelf)
                    CloseSettings();
                else
                    OpenSettings();
            }
        }
    }
}
