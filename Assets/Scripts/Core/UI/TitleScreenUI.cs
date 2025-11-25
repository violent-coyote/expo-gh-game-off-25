using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Expo.Data;
using Expo.Core.Debug;
using Expo.Core.Progression;

namespace Expo.UI
{
    /// <summary>
    /// Main title screen UI controller that manages navigation to the pre-shift scene,
    /// displays how-to-play information, and shows current save state.
    /// This serves as the entry point for the game experience.
    /// </summary>
    public class TitleScreenUI : MonoBehaviour
    {
        [Header("Main Menu UI")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private Button playButton;
        [SerializeField] private Button howToPlayButton;
        [SerializeField] private Button quitButton;
        
        [Header("How To Play UI")]
        [SerializeField] private GameObject howToPlayPanel;
        [SerializeField] private Button backToMenuButton;
        [SerializeField] private TextMeshProUGUI howToPlayText;
        
        [Header("Save State Display")]
        [SerializeField] private GameObject saveStatePanel;
        [SerializeField] private TextMeshProUGUI saveStateText;
        [SerializeField] private Button deleteSaveButton;
        
        [Header("Scene Settings")]
        [SerializeField] private string preShiftSceneName = "PreShiftScene";
        
        private PlayerSaveData _currentSaveData;

        // How to play content
        private const string HOW_TO_PLAY_CONTENT =
            "<color=black>" +
            "<color=white><size=24><b>How to Play - EXPO</b></size>\n\n" +
            "You are EXPO who runs this shit.\n\n" +
            "Don't serve cold food. Send food out on time. Don't fuck it up.\n\n" +
            "</color>\n"+
            "<color=red><b>FIRE DISHES</b></color>\n" +
            "• Tickets appear showing what each table ordered\n" +
            "• Click buttons to send dishes to cook\n" +
            "• Each dish has a pickup time (how long it takes to cook)\n\n\n" +
            "<color=red><b>RECEIVE DISHES</b></color>\n" +
            "• Cooked dishes appear on the pass\n" +
            "• Click the dish when a dish is ready to serve\n" +
            "• Be quick - dishes die if left too long on the pass!\n\n\n" +
            "<color=red><b>SEND DISHES</b></color>\n" +
            "• Click any available table to send selected dishes to it\n" +
            "• Complete all courses for each table to finish tickets\n\n\n" +
            "<b>Speed Controls:</b>\n" +
            "• Press '1' , '2', '3' for faster speeds\n\n" +
            "<color=red><b>Tips:</b></color>\n" +
            "Dishes should be sent together, per course\n"+
            "• Watch the timers - dishes die if left too long\n" +
            "• Recall pickup times - fire dishes strategically for each course\n" +
            "• Keep an eye on multiple tables and their timing\n"+
            "• Keep your cool. The worst thing that can happen is you ruin someone's life.\n" +
            "</color>";
        
        private void Start()
        {
            InitializeUI();
            LoadSaveData();
            UpdateSaveStateDisplay();
        }
        
        private void InitializeUI()
        {
            // Setup button listeners
            if (playButton != null)
                playButton.onClick.AddListener(OnPlayClicked);
                
            if (howToPlayButton != null)
                howToPlayButton.onClick.AddListener(OnHowToPlayClicked);
                
            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);
                
            if (backToMenuButton != null)
                backToMenuButton.onClick.AddListener(OnBackToMenuClicked);
                
            if (deleteSaveButton != null)
                deleteSaveButton.onClick.AddListener(OnDeleteSaveClicked);
            
            // Setup how to play text
            if (howToPlayText != null)
                howToPlayText.text = HOW_TO_PLAY_CONTENT;
            
            // Start with main menu active
            ShowMainMenu();
            
            DebugLogger.Log(DebugLogger.Category.UI, "Title Screen UI initialized");
        }
        
        private void LoadSaveData()
        {
            _currentSaveData = SaveSystem.LoadPlayerData();
            DebugLogger.Log(DebugLogger.Category.UI, $"Loaded save data: {_currentSaveData.unlockedDishIds.Count} dishes unlocked, {_currentSaveData.totalSessionsPlayed} sessions played");
        }
        
        private void UpdateSaveStateDisplay()
        {
            if (_currentSaveData == null) return;
            
            bool hasSaveFile = SaveSystem.SaveFileExists();
            
            // Main save state summary - just show level if save exists
            if (saveStateText != null)
            {
                if (hasSaveFile)
                {
                    saveStateText.text = $"Found save: Level {_currentSaveData.currentLevel}";
                }
                else
                {
                    saveStateText.text = "No save found";
                }
            }
            
            // Enable/disable delete save button based on whether save exists
            if (deleteSaveButton != null)
            {
                deleteSaveButton.gameObject.SetActive(hasSaveFile);
            }
        }
        
        #region Button Event Handlers
        
        /// <summary>
        /// Called when Play button is clicked. Loads the pre-shift scene.
        /// </summary>
        public void OnPlayClicked()
        {
            DebugLogger.Log(DebugLogger.Category.UI, $"Loading pre-shift scene: {preShiftSceneName}");
            
            // Clear any previous pre-shift selections to ensure fresh selection
            PreShiftUI.ClearSelectedDishes();
            
            SceneManager.LoadScene(preShiftSceneName);
        }
        
        /// <summary>
        /// Called when How To Play button is clicked. Shows the how-to-play panel.
        /// </summary>
        public void OnHowToPlayClicked()
        {
            ShowHowToPlayPanel();
        }
        
        /// <summary>
        /// Called when Quit button is clicked. Quits the application.
        /// </summary>
        public void OnQuitClicked()
        {
            DebugLogger.Log(DebugLogger.Category.UI, "Quit button clicked");
            
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
        
        /// <summary>
        /// Called when Back to Menu button is clicked. Returns to main menu from how-to-play.
        /// </summary>
        public void OnBackToMenuClicked()
        {
            ShowMainMenu();
        }
        
        /// <summary>
        /// Called when Delete Save button is clicked. Deletes the save file and refreshes display.
        /// </summary>
        public void OnDeleteSaveClicked()
        {
            if (SaveSystem.DeleteSaveFile())
            {
                DebugLogger.Log(DebugLogger.Category.UI, "Save file deleted successfully");
                
                // Reload save data (will create new default data)
                LoadSaveData();
                UpdateSaveStateDisplay();
                
                // If ProgressionManager exists, tell it to reset and reload
                if (ProgressionManager.Instance != null)
                {
                    ProgressionManager.Instance.ResetPlayerData();
                    DebugLogger.Log(DebugLogger.Category.UI, "Told ProgressionManager to reset after delete");
                }
            }
            else
            {
                DebugLogger.LogWarning(DebugLogger.Category.UI, "Failed to delete save file");
            }
        }
        
        #endregion
        
        #region Panel Management
        
        /// <summary>
        /// Shows the main menu panel and hides others.
        /// </summary>
        public void ShowMainMenu()
        {
            SetPanelActive(mainMenuPanel, true);
            SetPanelActive(howToPlayPanel, false);
            
            DebugLogger.Log(DebugLogger.Category.UI, "Showing main menu");
        }
        
        /// <summary>
        /// Shows the how-to-play panel and hides the main menu.
        /// </summary>
        public void ShowHowToPlayPanel()
        {
            SetPanelActive(mainMenuPanel, false);
            SetPanelActive(howToPlayPanel, true);
            
            DebugLogger.Log(DebugLogger.Category.UI, "Showing how to play panel");
        }
        
        /// <summary>
        /// Helper method to safely set panel active state.
        /// </summary>
        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
            {
                panel.SetActive(active);
            }
        }
        
        #endregion
        
        #region Public API for UI Buttons
        
        /// <summary>
        /// Public method that can be called directly from UI button events.
        /// Alternative to OnPlayClicked for direct Unity Event assignment.
        /// </summary>
        public void Play()
        {
            OnPlayClicked();
        }
        
        /// <summary>
        /// Public method that can be called directly from UI button events.
        /// Alternative to OnHowToPlayClicked for direct Unity Event assignment.
        /// </summary>
        public void HowToPlay()
        {
            OnHowToPlayClicked();
        }
        
        /// <summary>
        /// Public method that can be called directly from UI button events.
        /// Alternative to OnBackToMenuClicked for direct Unity Event assignment.
        /// </summary>
        public void BackToMenu()
        {
            OnBackToMenuClicked();
        }
        
        /// <summary>
        /// Public method that can be called directly from UI button events.
        /// Alternative to OnQuitClicked for direct Unity Event assignment.
        /// </summary>
        public void Quit()
        {
            OnQuitClicked();
        }
        
        /// <summary>
        /// Public method that can be called directly from UI button events.
        /// Alternative to OnDeleteSaveClicked for direct Unity Event assignment.
        /// </summary>
        public void DeleteSave()
        {
            OnDeleteSaveClicked();
        }
        
        #endregion
        
        #region Debug and Utility
        
        /// <summary>
        /// Force refresh the save state display (useful for testing).
        /// </summary>
        [ContextMenu("Refresh Save State Display")]
        public void RefreshSaveStateDisplay()
        {
            LoadSaveData();
            UpdateSaveStateDisplay();
        }
        
        /// <summary>
        /// Get current save data for debugging or external access.
        /// </summary>
        public PlayerSaveData GetCurrentSaveData()
        {
            return _currentSaveData;
        }
        
        /// <summary>
        /// Check if a save file exists (useful for conditional UI display).
        /// </summary>
        public bool HasSaveFile()
        {
            return SaveSystem.SaveFileExists();
        }
        
        #endregion
    }
}