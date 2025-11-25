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
        
        [Header("Grading Thresholds")]
        public GradingThresholds gradingThresholds = new GradingThresholds();
        
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
    
    /// <summary>
    /// Defines mistake count thresholds for letter grades.
    /// Grades are calculated as: A (0-maxMistakesForA), B, C, D, F (anything over maxMistakesForD)
    /// </summary>
    [Serializable]
    public class GradingThresholds
    {
        [Tooltip("Maximum mistakes allowed for an A grade")]
        public int maxMistakesForA = 0;
        
        [Tooltip("Maximum mistakes allowed for a B grade")]
        public int maxMistakesForB = 2;
        
        [Tooltip("Maximum mistakes allowed for a C grade")]
        public int maxMistakesForC = 5;
        
        [Tooltip("Maximum mistakes allowed for a D grade")]
        public int maxMistakesForD = 8;
        
        // Anything over maxMistakesForD is an F
        
        [Header("XP Rewards")]
        [Tooltip("XP awarded for an A grade")]
        public int xpForA = 200;
        
        [Tooltip("XP awarded for a B grade")]
        public int xpForB = 100;
        
        [Tooltip("XP awarded for a C grade")]
        public int xpForC = 50;
        
        [Tooltip("XP awarded for a D grade")]
        public int xpForD = 50;
        
        [Tooltip("XP awarded for an F grade")]
        public int xpForF = 25;
        
        /// <summary>
        /// Calculate letter grade based on mistake count
        /// </summary>
        public string CalculateGrade(int mistakeCount)
        {
            if (mistakeCount <= maxMistakesForA) return "A";
            if (mistakeCount <= maxMistakesForB) return "B";
            if (mistakeCount <= maxMistakesForC) return "C";
            if (mistakeCount <= maxMistakesForD) return "D";
            return "F";
        }
        
        /// <summary>
        /// Get XP reward for a grade
        /// </summary>
        public int GetXPForGrade(string grade)
        {
            switch (grade)
            {
                case "A": return xpForA;
                case "B": return xpForB;
                case "C": return xpForC;
                case "D": return xpForD;
                case "F": return xpForF;
                default: return 0;
            }
        }
        
        /// <summary>
        /// Get color for grade (for UI display)
        /// </summary>
        public Color GetGradeColor(string grade)
        {
            switch (grade)
            {
                case "A": return new Color(0.2f, 0.8f, 0.2f); // Green
                case "B": return new Color(0.4f, 0.7f, 0.3f); // Light green
                case "C": return new Color(0.9f, 0.9f, 0.2f); // Yellow
                case "D": return new Color(0.9f, 0.5f, 0.2f); // Orange
                case "F": return new Color(0.9f, 0.2f, 0.2f); // Red
                default: return Color.white;
            }
        }
    }
}