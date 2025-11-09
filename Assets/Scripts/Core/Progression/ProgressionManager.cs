using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Expo.Core;
using Expo.Core.Debug;
using Expo.Data;
using Expo.Core.Progression;

namespace Expo.Core.Progression
{
    /// <summary>
    /// Simplified progression manager for dish selection and basic persistence.
    /// Handles dish unlocks and current shift selection.
    /// </summary>
    public class ProgressionManager : CoreManager
    {
        [Header("Progression Configuration")]
        [SerializeField] private bool forceUnlockAllDishes = false;
        [Tooltip("For debugging - forces all dishes to be available regardless of progression")]
        
        // Runtime data
        [HideInInspector] public List<DishData> allAvailableDishes = new List<DishData>();
        public PlayerSaveData CurrentSave { get; private set; }
        public ProgressionConfig ProgressionConfig { get; private set; }
        
        // Singleton pattern for global access
        public static ProgressionManager Instance { get; private set; }
        
        protected override void OnInitialize()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                DebugLogger.LogError(DebugLogger.Category.PROGRESSION, "Multiple ProgressionManager instances detected!");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            // Make this object persistent across scene loads
            DontDestroyOnLoad(gameObject);
            
            // Load progression configuration and all dishes
            LoadProgressionData();
            
            // Load save data
            LoadPlayerData();
            
            // Update unlocked dishes based on progression
            UpdateUnlockedDishes();
            
            DebugLogger.Log(DebugLogger.Category.PROGRESSION, $"ProgressionManager initialized. Player level: {GetCurrentPlayerLevel()}, {CurrentSave.unlockedDishIds.Count} dishes unlocked");
        }
        
        protected override void OnShutdown()
        {
            // Auto-save on shutdown
            SavePlayerData();
            
            if (Instance == this)
                Instance = null;
        }
        
        #region Save/Load Operations
        
        /// <summary>
        /// Load progression configuration and dish assets
        /// </summary>
        private void LoadProgressionData()
        {
            ProgressionConfig = ProgressionConfigLoader.LoadProgressionConfig();
            allAvailableDishes = ProgressionConfigLoader.LoadAllDishes();
            
            DebugLogger.Log(DebugLogger.Category.PROGRESSION, $"Loaded {ProgressionConfig.levels.Count} progression levels and {allAvailableDishes.Count} dish assets");
        }
        
        /// <summary>
        /// Load player data from persistent storage
        /// </summary>
        public void LoadPlayerData()
        {
            CurrentSave = SaveSystem.LoadPlayerData();
            DebugLogger.Log(DebugLogger.Category.PROGRESSION, $"Player data loaded: XP={CurrentSave.currentXP}, Level={CurrentSave.currentLevel}, {CurrentSave.unlockedDishIds.Count} dishes unlocked: [{string.Join(", ", CurrentSave.unlockedDishIds)}]");
        }
        
        /// <summary>
        /// Save current player data to persistent storage
        /// </summary>
        public void SavePlayerData()
        {
            if (CurrentSave != null)
            {
                bool success = SaveSystem.SavePlayerData(CurrentSave);
                if (success)
                {
                    DebugLogger.Log(DebugLogger.Category.PROGRESSION, "Player data saved successfully");
                }
            }
        }
        
        /// <summary>
        /// Reset player data (for testing or new game)
        /// </summary>
        public void ResetPlayerData()
        {
            SaveSystem.DeleteSaveFile();
            CurrentSave = new PlayerSaveData();
            UnlockAllDishes();
            DebugLogger.Log(DebugLogger.Category.PROGRESSION, "Player data reset");
        }
        
        #endregion
        
        #region Dish Management
        
        /// <summary>
        /// Update unlocked dishes based on current player progression
        /// </summary>
        private void UpdateUnlockedDishes()
        {
            if (forceUnlockAllDishes)
            {
                // Debug mode: unlock everything
                UnlockAllDishes();
                return;
            }
            
            DebugLogger.Log(DebugLogger.Category.PROGRESSION, $"UpdateUnlockedDishes: Starting with {CurrentSave.unlockedDishIds.Count} unlocked dishes: [{string.Join(", ", CurrentSave.unlockedDishIds)}]");
            
            // Get dishes that should be available at current progression level
            var availableDishes = ProgressionConfigLoader.GetAvailableDishesForPlayer(CurrentSave);
            
            DebugLogger.Log(DebugLogger.Category.PROGRESSION, $"UpdateUnlockedDishes: ProgressionConfigLoader returned {availableDishes.Count} dish objects");
            foreach (var d in availableDishes)
            {
                DebugLogger.Log(DebugLogger.Category.PROGRESSION, $"  - Dish to unlock: {d.dishName}");
            }
            
            // Unlock dishes that should be available
            foreach (var dish in availableDishes)
            {
                DebugLogger.Log(DebugLogger.Category.PROGRESSION, $"  Attempting to unlock: {dish.dishName}");
                bool wasNew = CurrentSave.UnlockDish(dish.dishName);
                DebugLogger.Log(DebugLogger.Category.PROGRESSION, $"    Result: wasNew={wasNew}, current count={CurrentSave.unlockedDishIds.Count}");
            }
            
            DebugLogger.Log(DebugLogger.Category.PROGRESSION, $"Updated unlocks: {CurrentSave.unlockedDishIds.Count} dishes available at level {GetCurrentPlayerLevel()}: [{string.Join(", ", CurrentSave.unlockedDishIds)}]");
        }
        
        /// <summary>
        /// Unlock all dishes for the player (debug/fallback approach)
        /// </summary>
        private void UnlockAllDishes()
        {
            foreach (var dish in allAvailableDishes)
            {
                CurrentSave.UnlockDish(dish.dishName);
            }
            DebugLogger.Log(DebugLogger.Category.PROGRESSION, $"All {allAvailableDishes.Count} dishes unlocked (debug mode)");
        }
        
        /// <summary>
        /// Unlock a specific dish for the player
        /// </summary>
        public bool UnlockDish(string dishId)
        {
            bool wasUnlocked = CurrentSave.UnlockDish(dishId);
            if (wasUnlocked)
            {
                DebugLogger.Log(DebugLogger.Category.PROGRESSION, $"New dish unlocked: {dishId}");
            }
            return wasUnlocked;
        }
        
        /// <summary>
        /// Get the player's current progression level
        /// </summary>
        public int GetCurrentPlayerLevel()
        {
            return ProgressionConfigLoader.CalculatePlayerLevel(CurrentSave, ProgressionConfig);
        }
        
        /// <summary>
        /// Get unlocked dishes as DishData objects
        /// </summary>
        public List<DishData> GetUnlockedDishes()
        {
            if (forceUnlockAllDishes)
            {
                return new List<DishData>(allAvailableDishes);
            }
            
            return allAvailableDishes.Where(dish => CurrentSave.unlockedDishIds.Contains(dish.dishName)).ToList();
        }
        
        /// <summary>
        /// Get the dishes selected for the current shift
        /// </summary>
        public List<DishData> GetSelectedDishes()
        {
            var selectedIds = CurrentSave.GetSelectedDishes();
            return allAvailableDishes.Where(dish => selectedIds.Contains(dish.dishName)).ToList();
        }
        
        /// <summary>
        /// Set the dishes to use for the current shift
        /// </summary>
        public void SetSelectedDishes(List<string> dishIds)
        {
            CurrentSave.SetSelectedDishes(dishIds);
            DebugLogger.Log(DebugLogger.Category.PROGRESSION, $"Selected {dishIds.Count} dishes for shift");
            SavePlayerData(); // Auto-save when selection changes
        }
        
        #endregion
        
        #region Session Management
        
        /// <summary>
        /// Start a new session (increment session counter)
        /// </summary>
        public void StartNewSession()
        {
            CurrentSave.totalSessionsPlayed++;
            DebugLogger.Log(DebugLogger.Category.PROGRESSION, $"Started session #{CurrentSave.totalSessionsPlayed}");
        }
        
        /// <summary>
        /// Update best score if current score is higher
        /// </summary>
        public bool UpdateBestScore(float score)
        {
            if (score > CurrentSave.bestScore)
            {
                CurrentSave.bestScore = score;
                DebugLogger.Log(DebugLogger.Category.PROGRESSION, $"New best score: {score}");
                SavePlayerData(); // Auto-save new best score
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Award XP to the player and check for level up
        /// </summary>
        public bool AwardXP(int xpAmount)
        {
            if (xpAmount <= 0) return false;
            
            int previousLevel = GetCurrentPlayerLevel();
            CurrentSave.AddXP(xpAmount);
            
            // Recalculate level after XP gain
            int newLevel = ProgressionConfigLoader.CalculatePlayerLevel(CurrentSave, ProgressionConfig);
            
            DebugLogger.Log(DebugLogger.Category.PROGRESSION, 
                $"Awarded {xpAmount} XP. Total: {CurrentSave.currentXP} XP, Level: {newLevel}");
            
            // Check if leveled up
            if (newLevel > previousLevel)
            {
                CurrentSave.currentLevel = newLevel;
                OnLevelUp(previousLevel, newLevel);
                return true;
            }
            
            SavePlayerData();
            return false;
        }
        
        /// <summary>
        /// Handle level up event
        /// </summary>
        private void OnLevelUp(int oldLevel, int newLevel)
        {
            DebugLogger.Log(DebugLogger.Category.PROGRESSION, $"🎉 LEVEL UP! {oldLevel} → {newLevel}");
            
            // Update unlocked dishes
            UpdateUnlockedDishes();
            
            // Save progress
            SavePlayerData();
            
            // Get level info for rewards
            var levelInfo = GetCurrentLevelInfo();
            if (levelInfo != null)
            {
                DebugLogger.Log(DebugLogger.Category.PROGRESSION, 
                    $"Unlocked: {levelInfo.rewardTitle} - {levelInfo.rewardDescription}");
                
                if (levelInfo.unlockedDishes.Count > 0)
                {
                    DebugLogger.Log(DebugLogger.Category.PROGRESSION, 
                        $"New dishes available: {string.Join(", ", levelInfo.unlockedDishes)}");
                }
            }
        }
        
        /// <summary>
        /// Get progress to next level as a percentage (0-1)
        /// </summary>
        public float GetLevelProgress()
        {
            return ProgressionConfigLoader.GetLevelProgressPercentage(CurrentSave, ProgressionConfig);
        }
        
        /// <summary>
        /// Get XP needed for next level
        /// </summary>
        public int GetXPForNextLevel()
        {
            return ProgressionConfigLoader.GetXPRequiredForNextLevel(CurrentSave, ProgressionConfig);
        }
        
        /// <summary>
        /// Get XP needed for current level
        /// </summary>
        public int GetXPForCurrentLevel()
        {
            return ProgressionConfigLoader.GetXPRequiredForCurrentLevel(CurrentSave, ProgressionConfig);
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Get player's current statistics for UI display
        /// </summary>
        public PlayerSaveData GetPlayerStats()
        {
            return CurrentSave;
        }
        
        /// <summary>
        /// Get progression level information for UI display
        /// </summary>
        public ProgressionLevel GetCurrentLevelInfo()
        {
            int currentLevel = GetCurrentPlayerLevel();
            return ProgressionConfig.GetLevelData(currentLevel);
        }
        
        /// <summary>
        /// Get next level information for progression display
        /// </summary>
        public ProgressionLevel GetNextLevelInfo()
        {
            return ProgressionConfigLoader.GetNextLevel(CurrentSave, ProgressionConfig);
        }
        
        /// <summary>
        /// Check if player can level up and handle level up rewards
        /// </summary>
        public bool CheckAndHandleLevelUp(PlayerSaveData previousSave)
        {
            bool leveledUp = ProgressionConfigLoader.CheckForLevelUp(previousSave, CurrentSave, ProgressionConfig);
            
            if (leveledUp)
            {
                UpdateUnlockedDishes(); // Refresh unlocks after level up
                DebugLogger.Log(DebugLogger.Category.PROGRESSION, $"Player leveled up to level {GetCurrentPlayerLevel()}!");
            }
            
            return leveledUp;
        }
        
        /// <summary>
        /// Check if a dish is unlocked
        /// </summary>
        public bool IsDishUnlocked(string dishId)
        {
            return CurrentSave.unlockedDishIds.Contains(dishId);
        }
        
        /// <summary>
        /// Get all available dishes (regardless of unlock status)
        /// </summary>
        public List<DishData> GetAllDishes()
        {
            return new List<DishData>(allAvailableDishes);
        }
        
        /// <summary>
        /// Force refresh progression data (useful for testing)
        /// </summary>
        [ContextMenu("Refresh Progression Data")]
        public void RefreshProgressionData()
        {
            ProgressionConfigLoader.ClearCache();
            LoadProgressionData();
            UpdateUnlockedDishes();
            
            DebugLogger.Log(DebugLogger.Category.PROGRESSION, 
                $"Progression data refreshed. Level {GetCurrentPlayerLevel()}, {CurrentSave.unlockedDishIds.Count} dishes unlocked");
        }
        
        /// <summary>
        /// Debug method to simulate level progression
        /// </summary>
        [ContextMenu("Simulate Level Up")]
        public void SimulateLevelUp()
        {
            int xpToNextLevel = GetXPForNextLevel();
            if (xpToNextLevel == -1)
            {
                DebugLogger.LogWarning(DebugLogger.Category.PROGRESSION, "Already at max level!");
                return;
            }
            
            int currentLevelXP = GetXPForCurrentLevel();
            int xpNeeded = xpToNextLevel - CurrentSave.currentXP;
            
            DebugLogger.Log(DebugLogger.Category.PROGRESSION, $"Awarding {xpNeeded} XP to reach next level...");
            AwardXP(xpNeeded);
        }
        
        /// <summary>
        /// Debug method to add a specific amount of XP
        /// </summary>
        [ContextMenu("Add 50 XP")]
        public void Add50XP()
        {
            AwardXP(50);
        }
        
        /// <summary>
        /// Debug method to add a specific amount of XP
        /// </summary>
        [ContextMenu("Add 100 XP")]
        public void Add100XP()
        {
            AwardXP(100);
        }
        
        /// <summary>
        /// Verify that ProgressionManager is properly set up and accessible
        /// </summary>
        [ContextMenu("Verify Setup")]
        public void VerifySetup()
        {
            DebugLogger.Log(DebugLogger.Category.PROGRESSION, "=== ProgressionManager Setup Verification ===");
            DebugLogger.Log(DebugLogger.Category.PROGRESSION, $"✅ Instance exists: {Instance != null}");
            DebugLogger.Log(DebugLogger.Category.PROGRESSION, $"✅ GameObject persistent: {gameObject != null}");
            DebugLogger.Log(DebugLogger.Category.PROGRESSION, $"✅ Dishes loaded: {allAvailableDishes.Count} dishes found");
            DebugLogger.Log(DebugLogger.Category.PROGRESSION, $"✅ Config loaded: {ProgressionConfig?.levels?.Count ?? 0} progression levels");
            
            int currentLevel = GetCurrentPlayerLevel();
            int currentXP = CurrentSave?.currentXP ?? 0;
            int xpForNext = GetXPForNextLevel();
            float progress = GetLevelProgress();
            
            DebugLogger.Log(DebugLogger.Category.PROGRESSION, $"✅ Player Level: {currentLevel}");
            DebugLogger.Log(DebugLogger.Category.PROGRESSION, $"✅ Current XP: {currentXP}");
            DebugLogger.Log(DebugLogger.Category.PROGRESSION, $"✅ XP for Next Level: {(xpForNext == -1 ? "MAX LEVEL" : xpForNext.ToString())}");
            DebugLogger.Log(DebugLogger.Category.PROGRESSION, $"✅ Level Progress: {progress:P1}");
            DebugLogger.Log(DebugLogger.Category.PROGRESSION, $"✅ Dishes Unlocked: {CurrentSave?.unlockedDishIds?.Count ?? 0}");
            
            if (allAvailableDishes.Count == 0)
            {
                DebugLogger.LogWarning(DebugLogger.Category.PROGRESSION, "⚠️ No dishes loaded! Run 'Expo → Setup Dish Resources' and check Assets/Data/Dishes/");
            }
            
            if (ProgressionConfig?.levels?.Count == 0)
            {
                DebugLogger.LogWarning(DebugLogger.Category.PROGRESSION, "⚠️ No progression levels loaded! Check Assets/Data/progression_config.json");
            }
            
            DebugLogger.Log(DebugLogger.Category.PROGRESSION, "=== End Verification ===");
        }
        
        #endregion
    }
}