using System;
using System.Collections.Generic;
using UnityEngine;

namespace Expo.Core.Debug
{
    /// <summary>
    /// Centralized debug logging system with category-based filtering.
    /// Categories can be toggled on/off through the GameManager.
    /// </summary>
    public static class DebugLogger
    {
        /// <summary>
        /// Available debug categories throughout the game
        /// </summary>
        public enum Category
        {
            TABLE,
            TABLE_MANAGER,
            TABLE_DEBUG,
            TABLE_UI,
            EXPO,
            PASS,
            TICKET,
            TICKET_UI,
            TICKET_MANAGER,
            COURSE,
            STATION,
            SCORE,
            MISTAKE,
            UI,
            PROGRESSION,
            TIME,
            GENERAL
        }

        // Dictionary to store which categories are enabled
        private static Dictionary<Category, bool> _categoryStates = new Dictionary<Category, bool>();

        /// <summary>
        /// Initialize all categories as enabled by default
        /// </summary>
        static DebugLogger()
        {
            foreach (Category category in Enum.GetValues(typeof(Category)))
            {
                _categoryStates[category] = true;
            }
        }

        /// <summary>
        /// Set whether a specific category is enabled
        /// </summary>
        public static void SetCategoryEnabled(Category category, bool enabled)
        {
            _categoryStates[category] = enabled;
        }

        /// <summary>
        /// Get whether a specific category is enabled
        /// </summary>
        public static bool IsCategoryEnabled(Category category)
        {
            return _categoryStates.TryGetValue(category, out bool enabled) && enabled;
        }

        /// <summary>
        /// Enable all debug categories
        /// </summary>
        public static void EnableAll()
        {
            foreach (Category category in Enum.GetValues(typeof(Category)))
            {
                _categoryStates[category] = true;
            }
        }

        /// <summary>
        /// Disable all debug categories
        /// </summary>
        public static void DisableAll()
        {
            foreach (Category category in Enum.GetValues(typeof(Category)))
            {
                _categoryStates[category] = false;
            }
        }

        /// <summary>
        /// Log a message if the category is enabled
        /// </summary>
        public static void Log(Category category, string message)
        {
            if (IsCategoryEnabled(category))
            {
                UnityEngine.Debug.Log($"[{category}] {message}");
            }
        }

        /// <summary>
        /// Log a warning if the category is enabled
        /// </summary>
        public static void LogWarning(Category category, string message)
        {
            if (IsCategoryEnabled(category))
            {
                UnityEngine.Debug.LogWarning($"[{category}] {message}");
            }
        }

        /// <summary>
        /// Log an error if the category is enabled
        /// </summary>
        public static void LogError(Category category, string message)
        {
            if (IsCategoryEnabled(category))
            {
                UnityEngine.Debug.LogError($"[{category}] {message}");
            }
        }

        /// <summary>
        /// Get a formatted string of all category states (for debugging the debugger!)
        /// </summary>
        public static string GetCategoryStatesString()
        {
            var states = new List<string>();
            foreach (var kvp in _categoryStates)
            {
                states.Add($"{kvp.Key}: {(kvp.Value ? "✓" : "✗")}");
            }
            return string.Join(", ", states);
        }
    }
}
