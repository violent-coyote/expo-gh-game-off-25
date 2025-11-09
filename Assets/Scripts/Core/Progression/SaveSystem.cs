using System;
using System.IO;
using UnityEngine;
using Expo.Data;
using Expo.Core.Debug;

namespace Expo.Core.Progression
{
    /// <summary>
    /// Handles saving and loading player progression data to/from persistent storage.
    /// Uses JSON serialization with Unity's JsonUtility for cross-platform compatibility.
    /// </summary>
    public static class SaveSystem
    {
        private const string SAVE_FILE_NAME = "player_progress.json";
        private static string SavePath => Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
        
        /// <summary>
        /// Save player data to persistent storage
        /// </summary>
        public static bool SavePlayerData(PlayerSaveData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(SavePath, json);
                DebugLogger.Log(DebugLogger.Category.PROGRESSION, $"Player data saved to: {SavePath}");
                return true;
            }
            catch (Exception e)
            {
                DebugLogger.LogError(DebugLogger.Category.PROGRESSION, $"Failed to save player data: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Load player data from persistent storage
        /// </summary>
        public static PlayerSaveData LoadPlayerData()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    string json = File.ReadAllText(SavePath);
                    PlayerSaveData data = JsonUtility.FromJson<PlayerSaveData>(json);
                    DebugLogger.Log(DebugLogger.Category.PROGRESSION, $"Player data loaded from: {SavePath}");
                    return data;
                }
                else
                {
                    DebugLogger.Log(DebugLogger.Category.PROGRESSION, "No save file found, creating new player data");
                    return new PlayerSaveData();
                }
            }
            catch (Exception e)
            {
                DebugLogger.LogError(DebugLogger.Category.PROGRESSION, $"Failed to load player data: {e.Message}");
                return new PlayerSaveData();
            }
        }
        
        /// <summary>
        /// Check if a save file exists
        /// </summary>
        public static bool SaveFileExists()
        {
            return File.Exists(SavePath);
        }
        
        /// <summary>
        /// Delete the save file (for testing or new game functionality)
        /// </summary>
        public static bool DeleteSaveFile()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    File.Delete(SavePath);
                    DebugLogger.Log(DebugLogger.Category.PROGRESSION, "Save file deleted");
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                DebugLogger.LogError(DebugLogger.Category.PROGRESSION, $"Failed to delete save file: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Get the full path to the save file for debugging
        /// </summary>
        public static string GetSaveFilePath()
        {
            return SavePath;
        }
    }
}