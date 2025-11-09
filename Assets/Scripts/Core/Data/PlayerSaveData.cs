using System;
using System.Collections.Generic;
using UnityEngine;

namespace Expo.Data
{
    /// <summary>
    /// Simplified player save data for basic progression.
    /// Contains unlocked dishes and current dish selection for the shift.
    /// </summary>
    [Serializable]
    public class PlayerSaveData
    {
        [Header("Dish Management")]
        public List<string> unlockedDishIds = new List<string>();
        public List<string> selectedDishIds = new List<string>(); // Dishes chosen for current shift
        
        [Header("Progression")]
        public int currentXP = 0;
        public int currentLevel = 1;
        
        [Header("Statistics")]
        public int totalSessionsPlayed = 0;
        public float bestScore = 0f;
        public int totalXPEarned = 0;
        
        public PlayerSaveData()
        {
            // Initialize progression
            currentXP = 0;
            currentLevel = 1;
            totalXPEarned = 0;
            
            UnityEngine.Debug.Log($"[PROGRESSION] NEW PlayerSaveData created with {unlockedDishIds.Count} unlocked dishes");
        }
        
        /// <summary>
        /// Add XP and return true if leveled up
        /// </summary>
        public bool AddXP(int xpAmount)
        {
            currentXP += xpAmount;
            totalXPEarned += xpAmount;
            return false; // Level up is handled by ProgressionManager
        }
        
        /// <summary>
        /// Unlock a new dish by ID
        /// </summary>
        public bool UnlockDish(string dishId)
        {
            if (!unlockedDishIds.Contains(dishId))
            {
                unlockedDishIds.Add(dishId);
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Set the dishes selected for the current shift
        /// </summary>
        public void SetSelectedDishes(List<string> dishIds)
        {
            selectedDishIds.Clear();
            foreach (var dishId in dishIds)
            {
                if (unlockedDishIds.Contains(dishId))
                {
                    selectedDishIds.Add(dishId);
                }
            }
        }
        
        /// <summary>
        /// Get the dishes selected for the current shift
        /// </summary>
        public List<string> GetSelectedDishes()
        {
            return new List<string>(selectedDishIds);
        }
    }
}