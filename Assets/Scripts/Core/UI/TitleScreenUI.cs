using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Expo.Data;
using Expo.Core.Debug;
using Expo.Core.Progression;
using Expo.Controllers;
using DG.Tweening;

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
        [SerializeField] private Image title;
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
        
        [Header("Music Controller")]
        [Tooltip("Prefab for the persistent music controller. Will be instantiated if not already present.")]
        [SerializeField] private GameObject musicControllerPrefab;
        
        [Header("Title Animation Settings")]
        [Tooltip("Scale amount for the title breathing animation")]
        [SerializeField] [Range(0.95f, 1.1f)] private float titleScaleAmount = 1.03f;
        
        [Tooltip("Duration of one cycle of the title breathing animation")]
        [SerializeField] [Range(0.1f, 3f)] private float titleScaleDuration = 1.2f;
        
        [Header("Main Menu Panel Animation Settings")]
        [Tooltip("Scale amount for the main menu panel breathing animation")]
        [SerializeField] [Range(0.95f, 1.05f)] private float panelScaleAmount = 1.01f;
        
        [Tooltip("Duration of one cycle of the panel breathing animation")]
        [SerializeField] [Range(0.1f, 2f)] private float panelScaleDuration = 0.8f;
        
        private PlayerSaveData _currentSaveData;
        private Tween _titleScaleTween;
        private Tween _mainMenuScaleTween;
        
        private void Start()
        {
            InitializeMusicController();
            InitializeUI();
            LoadSaveData();
            UpdateSaveStateDisplay();
        }
        
        /// <summary>
        /// Ensures the MusicController exists. Creates it from prefab if needed.
        /// Only creates one instance even if returning to title screen multiple times.
        /// </summary>
        private void InitializeMusicController()
        {
            // Check if MusicController already exists
            if (MusicController.Instance != null)
            {
                DebugLogger.Log(DebugLogger.Category.UI, 
                    "MusicController already exists, skipping instantiation");
                return;
            }
            
            // Create MusicController from prefab if provided
            if (musicControllerPrefab != null)
            {
                GameObject musicControllerGO = Instantiate(musicControllerPrefab);
                musicControllerGO.name = "MusicController"; // Clean up the (Clone) suffix
                DebugLogger.Log(DebugLogger.Category.UI, 
                    "MusicController instantiated from prefab in title screen");
            }
            else
            {
                DebugLogger.LogWarning(DebugLogger.Category.UI, 
                    "No MusicController prefab assigned to TitleScreenUI");
            }
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
            
            // Start with main menu active
            ShowMainMenu();
            
            // Start the scale animations
            StartTitleAnimation();
            StartMainMenuPanelAnimation();
            
            DebugLogger.Log(DebugLogger.Category.UI, "Title Screen UI initialized");
        }
        
        /// <summary>
        /// Starts the subtle breathing scale animation on the title image.
        /// </summary>
        private void StartTitleAnimation()
        {
            if (title == null) return;
            
            // Kill any existing tween to avoid duplicates
            _titleScaleTween?.Kill();
            
            // Reset scale to 1 before starting
            title.transform.localScale = Vector3.one;
            
            // Create a ping-pong loop tween
            _titleScaleTween = title.transform
                .DOScale(titleScaleAmount, titleScaleDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
            
            DebugLogger.Log(DebugLogger.Category.UI, 
                $"Title scale animation started (scale: {titleScaleAmount}, duration: {titleScaleDuration}s)");
        }
        
        /// <summary>
        /// Starts the subtle breathing scale animation on the main menu panel.
        /// </summary>
        private void StartMainMenuPanelAnimation()
        {
            if (mainMenuPanel == null) return;
            
            // Kill any existing tween to avoid duplicates
            _mainMenuScaleTween?.Kill();
            
            // Reset scale to 1 before starting
            mainMenuPanel.transform.localScale = Vector3.one;
            
            // Create a ping-pong loop tween
            _mainMenuScaleTween = mainMenuPanel.transform
                .DOScale(panelScaleAmount, panelScaleDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
            
            DebugLogger.Log(DebugLogger.Category.UI, 
                $"Main menu panel scale animation started (scale: {panelScaleAmount}, duration: {panelScaleDuration}s)");
        }
        
        private void OnDestroy()
        {
            // Clean up tweens when this UI is destroyed
            _titleScaleTween?.Kill();
            _mainMenuScaleTween?.Kill();
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