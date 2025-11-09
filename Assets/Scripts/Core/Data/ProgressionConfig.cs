using System;
using System.Collections.Generic;
using UnityEngine;

namespace Expo.Data
{
    /// <summary>
    /// Configuration data for progression system.
    /// Defines what gets unlocked at each level/milestone.
    /// </summary>
    [Serializable]
    public class ProgressionConfig
    {
        [Header("Progression Configuration")]
        public List<ProgressionLevel> levels = new List<ProgressionLevel>();
        
        /// <summary>
        /// Get all dishes that should be unlocked at or below the specified level
        /// </summary>
        public List<string> GetUnlockedDishesForLevel(int level)
        {
            var unlockedDishes = new List<string>();
            
            foreach (var progressionLevel in levels)
            {
                if (progressionLevel.level <= level)
                {
                    unlockedDishes.AddRange(progressionLevel.unlockedDishes);
                }
            }
            
            return unlockedDishes;
        }
        
        /// <summary>
        /// Get the level at which a specific dish is unlocked
        /// </summary>
        public int GetUnlockLevelForDish(string dishId)
        {
            foreach (var progressionLevel in levels)
            {
                if (progressionLevel.unlockedDishes.Contains(dishId))
                {
                    return progressionLevel.level;
                }
            }
            return -1; // Not found
        }
        
        /// <summary>
        /// Get all unlocks for a specific level
        /// </summary>
        public ProgressionLevel GetLevelData(int level)
        {
            return levels.Find(l => l.level == level);
        }
    }
    
    /// <summary>
    /// Data for a single progression level
    /// </summary>
    [Serializable]
    public class ProgressionLevel
    {
        [Header("Level Info")]
        public int level;
        public string levelName;
        public string description;
        
        [Header("XP Requirements")]
        public int xpRequired = 0; // Total XP needed to reach this level
        
        [Header("Unlocks")]
        public List<string> unlockedDishes = new List<string>();
        public List<string> unlockedFeatures = new List<string>();
        
        [Header("Rewards")]
        public string rewardTitle;
        public string rewardDescription;
    }
}