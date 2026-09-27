using System;
using _02._Script._01_Players;
using _02._Script.UI;
using UnityEngine;

namespace _02._Script._05_Managers
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }
        
        
        [SerializeField] private StaminaHUD staminaHUD;

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
        }
    }
}