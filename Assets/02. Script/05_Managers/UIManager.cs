using System;
using _02._Script._01_Players;
using _02._Script.UI;
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
        
        private void Update() {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (settingsPanel.activeSelf)
                    CloseSettings();
                else
                    OpenSettings();
            }
        }
    }
}