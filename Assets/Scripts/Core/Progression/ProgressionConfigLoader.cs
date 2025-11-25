using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Expo.Data;
using Expo.Core.Debug;

namespace Expo.Core.Progression
{
    /// <summary>
    /// Handles loading dish assets and progression configuration.
    /// Automatically discovers dishes from Assets/Data/Dishes and manages unlock progression.
    /// </summary>
    public static class ProgressionConfigLoader
    {
        private static ProgressionConfig _cachedConfig;
        private static List<DishData> _cachedAllDishes;
        
        /// <summary>
        /// Load progression configuration from JSON file
        /// </summary>
        public static ProgressionConfig LoadProgressionConfig()
        {
            if (_cachedConfig != null)
                return _cachedConfig;
            
            try
            {
                // Load from Resources folder for both editor and builds
                TextAsset configAsset = Resources.Load<TextAsset>("Data/progression_config");
                
                if (configAsset != null)
                {
                    string jsonContent = configAsset.text;
                    _cachedConfig = JsonUtility.FromJson<ProgressionConfig>(jsonContent);
                    DebugLogger.Log(DebugLogger.Category.PROGRESSION, $"Loaded progression config from Resources with {_cachedConfig.levels.Count} levels");
                }
                else
                {
                    DebugLogger.LogWarning(DebugLogger.Category.PROGRESSION, "Progression config not found in Resources/Data/progression_config.json, creating default");
                    _cachedConfig = CreateDefaultProgressionConfig();
                }
            }
            catch (System.Exception e)
            {
                DebugLogger.LogError(DebugLogger.Category.PROGRESSION, $"Failed to load progression config: {e.Message}");
                _cachedConfig = CreateDefaultProgressionConfig();
            }
            
            return _cachedConfig;
        }
        
        /// <summary>
        /// Load all dish assets from Resources/Data/Dishes folder
        /// </summary>
        public static List<DishData> LoadAllDishes()
        {
            if (_cachedAllDishes != null)
                return _cachedAllDishes;
                
            _cachedAllDishes = new List<DishData>();
            
            // Load from Resources folder for both editor and builds
            DishData[] dishAssets = Resources.LoadAll<DishData>("Data/Dishes");
            _cachedAllDishes.AddRange(dishAssets);
            
            // If Resources folder doesn't have dishes, log a warning
            if (dishAssets.Length == 0)
            {
                DebugLogger.LogError(DebugLogger.Category.PROGRESSION, 
                    "No dishes found in Resources/Data/Dishes! " +
                    "Create DishData assets in Assets/Resources/Data/Dishes/ to add dishes to the game.");
            }
            
            DebugLogger.Log(DebugLogger.Category.PROGRESSION, $"Loaded {_cachedAllDishes.Count} dishes from disk");
            foreach (var dish in _cachedAllDishes)
            {
                DebugLogger.Log(DebugLogger.Category.PROGRESSION, $"Found dish: {dish.dishName}");
            }
            
            return _cachedAllDishes;
        }
        
        /// <summary>
        /// Get dishes that should be available based on player progression
        /// </summary>
        public static List<DishData> GetAvailableDishesForPlayer(PlayerSaveData saveData)
        {
            var progressionConfig = LoadProgressionConfig();
            var allDishes = LoadAllDishes();
            var availableDishes = new List<DishData>();
            
            // Determine player's current level
            int playerLevel = CalculatePlayerLevel(saveData, progressionConfig);
            
            DebugLogger.Log(DebugLogger.Category.PROGRESSION, $"GetAvailableDishesForPlayer: Player XP={saveData.currentXP}, Level={playerLevel}");
            
            // Get dishes unlocked at this level
            var unlockedDishIds = progressionConfig.GetUnlockedDishesForLevel(playerLevel);
            
            DebugLogger.Log(DebugLogger.Category.PROGRESSION, $"GetUnlockedDishesForLevel returned {unlockedDishIds.Count} dish IDs: {string.Join(", ", unlockedDishIds)}");
            
            // Find matching dish assets
            foreach (var dishId in unlockedDishIds)
            {
                var dish = allDishes.Find(d => d.dishName == dishId);
                if (dish != null)
                {
                    availableDishes.Add(dish);
                }
                else
                {
                    DebugLogger.LogWarning(DebugLogger.Category.PROGRESSION, $"Dish '{dishId}' is in progression config but no asset found");
                }
            }
            
            DebugLogger.Log(DebugLogger.Category.PROGRESSION, $"Player level {playerLevel}: {availableDishes.Count} dishes available");
            
            return availableDishes;
        }
        
        /// <summary>
        /// Calculate player's current progression level based on their XP
        /// </summary>
        public static int CalculatePlayerLevel(PlayerSaveData saveData, ProgressionConfig config)
        {
            int highestLevelMet = 1; // Start at level 1
            
            DebugLogger.Log(DebugLogger.Category.PROGRESSION, $"CalculatePlayerLevel: Checking {config.levels.Count} levels with player XP={saveData.currentXP}");
            
            foreach (var level in config.levels)
            {
                // Check if player has enough XP for this level
                if (saveData.currentXP >= level.xpRequired)
                {
                    DebugLogger.Log(DebugLogger.Category.PROGRESSION, $"  Level {level.level} ({level.levelName}): {saveData.currentXP} >= {level.xpRequired} ✅ UNLOCKED");
                    highestLevelMet = Mathf.Max(highestLevelMet, level.level);
                }
                else
                {
                    DebugLogger.Log(DebugLogger.Category.PROGRESSION, $"  Level {level.level} ({level.levelName}): {saveData.currentXP} < {level.xpRequired} ❌ LOCKED");
                }
            }
            
            DebugLogger.Log(DebugLogger.Category.PROGRESSION, $"CalculatePlayerLevel: Final level = {highestLevelMet}");
            
            return highestLevelMet;
        }
        
        /// <summary>
        /// Get XP required for next level
        /// </summary>
        public static int GetXPRequiredForNextLevel(PlayerSaveData saveData, ProgressionConfig config)
        {
            int currentLevel = CalculatePlayerLevel(saveData, config);
            var nextLevel = config.levels.Find(l => l.level == currentLevel + 1);
            return nextLevel?.xpRequired ?? -1; // -1 if max level
        }
        
        /// <summary>
        /// Get XP required for current level (for progress bar calculation)
        /// </summary>
        public static int GetXPRequiredForCurrentLevel(PlayerSaveData saveData, ProgressionConfig config)
        {
            int currentLevel = CalculatePlayerLevel(saveData, config);
            var level = config.levels.Find(l => l.level == currentLevel);
            return level?.xpRequired ?? 0;
        }
        
        /// <summary>
        /// Calculate progress to next level as a percentage (0-1)
        /// </summary>
        public static float GetLevelProgressPercentage(PlayerSaveData saveData, ProgressionConfig config)
        {
            int currentLevel = CalculatePlayerLevel(saveData, config);
            int currentLevelXP = GetXPRequiredForCurrentLevel(saveData, config);
            int nextLevelXP = GetXPRequiredForNextLevel(saveData, config);
            
            if (nextLevelXP == -1) return 1f; // Max level
            
            int xpIntoCurrentLevel = saveData.currentXP - currentLevelXP;
            int xpNeededForNextLevel = nextLevelXP - currentLevelXP;
            
            return Mathf.Clamp01((float)xpIntoCurrentLevel / xpNeededForNextLevel);
        }
        
        /// <summary>
        /// Check if player has just leveled up (for showing rewards)
        /// </summary>
        public static bool CheckForLevelUp(PlayerSaveData previousSave, PlayerSaveData currentSave, ProgressionConfig config)
        {
            int previousLevel = CalculatePlayerLevel(previousSave, config);
            int currentLevel = CalculatePlayerLevel(currentSave, config);
            
            return currentLevel > previousLevel;
        }
        
        /// <summary>
        /// Get the next level's requirements for UI display
        /// </summary>
        public static ProgressionLevel GetNextLevel(PlayerSaveData saveData, ProgressionConfig config)
        {
            int currentLevel = CalculatePlayerLevel(saveData, config);
            return config.levels.Find(l => l.level == currentLevel + 1);
        }
        
        /// <summary>
        /// Create a default progression config if none exists
        /// </summary>
        private static ProgressionConfig CreateDefaultProgressionConfig()
        {
            var config = new ProgressionConfig();
            
            // Add a basic level 1 that unlocks all dishes for fallback
            var level1 = new ProgressionLevel
            {
                level = 1,
                levelName = "Basic Level",
                description = "Default progression level",
                xpRequired = 0,
                unlockedDishes = new List<string>(),
                rewardTitle = "Getting Started",
                rewardDescription = "Welcome to the kitchen!"
            };
            
            // Auto-populate with all found dishes
            var allDishes = LoadAllDishes();
            foreach (var dish in allDishes)
            {
                level1.unlockedDishes.Add(dish.dishName);
            }
            
            config.levels.Add(level1);
            
            DebugLogger.Log(DebugLogger.Category.PROGRESSION, "Created default progression config");
            return config;
        }
        
        /// <summary>
        /// Clear cached data (useful for testing or hot-reloading)
        /// </summary>
        public static void ClearCache()
        {
            _cachedConfig = null;
            _cachedAllDishes = null;
            DebugLogger.Log(DebugLogger.Category.PROGRESSION, "Progression cache cleared");
        }
    }
}