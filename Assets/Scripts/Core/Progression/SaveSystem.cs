using System;
using System.IO;
using UnityEngine;
using Expo.Data;
using Expo.Core.Debug;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Expo.Core.Progression
{
    /// <summary>
    /// Handles saving and loading player progression data to/from persistent storage.
    /// Uses JSON serialization with Unity's JsonUtility for cross-platform compatibility.
    /// WebGL builds use PlayerPrefs for reliable browser storage persistence.
    /// </summary>
    public static class SaveSystem
    {
        private const string SAVE_FILE_NAME = "player_progress.json";
        private const string SAVE_KEY = "PlayerSaveData";
        private static string SavePath => Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void SyncFiles();
        
        private static bool IsWebGL => true;
#else
        private static bool IsWebGL => false;
#endif
        
        /// <summary>
        /// Save player data to persistent storage
        /// WebGL: Uses PlayerPrefs with localStorage (reliable across page refreshes)
        /// Other platforms: Uses file-based storage
        /// </summary>
        public static bool SavePlayerData(PlayerSaveData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, true);
                
#if UNITY_WEBGL && !UNITY_EDITOR
                // Use PlayerPrefs for WebGL - it's stored in browser localStorage
                // which persists reliably across page refreshes
                PlayerPrefs.SetString(SAVE_KEY, json);
                PlayerPrefs.Save(); // Force immediate save
                DebugLogger.Log(DebugLogger.Category.PROGRESSION, $"Player data saved to PlayerPrefs (WebGL)");
#else
                // Use file-based storage for other platforms
                File.WriteAllText(SavePath, json);
                DebugLogger.Log(DebugLogger.Category.PROGRESSION, $"Player data saved to: {SavePath}");
#endif
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
        /// WebGL: Uses PlayerPrefs with localStorage
        /// Other platforms: Uses file-based storage
        /// </summary>
        public static PlayerSaveData LoadPlayerData()
        {
            try
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                // Use PlayerPrefs for WebGL
                if (PlayerPrefs.HasKey(SAVE_KEY))
                {
                    string json = PlayerPrefs.GetString(SAVE_KEY);
                    PlayerSaveData data = JsonUtility.FromJson<PlayerSaveData>(json);
                    DebugLogger.Log(DebugLogger.Category.PROGRESSION, "Player data loaded from PlayerPrefs (WebGL)");
                    return data;
                }
                else
                {
                    DebugLogger.Log(DebugLogger.Category.PROGRESSION, "No save data found in PlayerPrefs, creating new player data");
                    return new PlayerSaveData();
                }
#else
                // Use file-based storage for other platforms
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
#endif
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
#if UNITY_WEBGL && !UNITY_EDITOR
            return PlayerPrefs.HasKey(SAVE_KEY);
#else
            return File.Exists(SavePath);
#endif
        }
        
        /// <summary>
        /// Delete the save file (for testing or new game functionality)
        /// </summary>
        public static bool DeleteSaveFile()
        {
            try
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                if (PlayerPrefs.HasKey(SAVE_KEY))
                {
                    PlayerPrefs.DeleteKey(SAVE_KEY);
                    PlayerPrefs.Save();
                    DebugLogger.Log(DebugLogger.Category.PROGRESSION, "Save data deleted from PlayerPrefs (WebGL)");
                    return true;
                }
                return false;
#else
                if (File.Exists(SavePath))
                {
                    File.Delete(SavePath);
                    DebugLogger.Log(DebugLogger.Category.PROGRESSION, "Save file deleted");
                    return true;
                }
                return false;
#endif
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