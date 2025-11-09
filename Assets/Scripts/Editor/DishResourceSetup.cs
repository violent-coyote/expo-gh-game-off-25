using UnityEngine;
using UnityEditor;
using System.IO;
using Expo.Data;

namespace Expo.Editor
{
    /// <summary>
    /// Editor utility to set up dish assets in Resources folder for runtime loading.
    /// Required for WebGL and other platform builds to access dishes at runtime.
    /// </summary>
    public static class DishResourceSetup
    {
        [MenuItem("Expo/Setup Dish Resources")]
        public static void SetupDishResources()
        {
            string sourcePath = "Assets/Data/Dishes";
            string targetPath = "Assets/Resources/Data/Dishes";
            
            // Ensure target directory exists
            Directory.CreateDirectory(targetPath);
            
            // Find all DishData assets in source directory
            string[] guids = AssetDatabase.FindAssets("t:DishData", new[] { sourcePath });
            
            if (guids.Length == 0)
            {
                Debug.LogWarning($"No DishData assets found in {sourcePath}");
                return;
            }
            
            int copiedCount = 0;
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileName(assetPath);
                string targetFilePath = Path.Combine(targetPath, fileName);
                
                // Copy the asset
                if (AssetDatabase.CopyAsset(assetPath, targetFilePath))
                {
                    copiedCount++;
                    Debug.Log($"Copied {fileName} to Resources folder");
                }
                else
                {
                    Debug.LogError($"Failed to copy {fileName}");
                }
            }
            
            // Also copy the progression config
            string configSource = "Assets/Data/progression_config.json";
            string configTarget = "Assets/Resources/Data/progression_config.json";
            
            if (File.Exists(configSource))
            {
                Directory.CreateDirectory("Assets/Resources/Data");
                if (AssetDatabase.CopyAsset(configSource, configTarget))
                {
                    Debug.Log("Copied progression_config.json to Resources folder");
                }
                else
                {
                    Debug.LogError("Failed to copy progression_config.json");
                }
            }
            
            AssetDatabase.Refresh();
            Debug.Log($"Setup complete! Copied {copiedCount} dish assets and progression config to Resources folder.");
            Debug.Log("✅ WebGL builds will now load dishes and config correctly.");
        }
        
        [MenuItem("Expo/Update Progression Config")]
        public static void UpdateProgressionConfig()
        {
            // This would update the JSON file with any new dishes found
            // For now, just log what dishes are available
            
            string dishesPath = "Assets/Data/Dishes";
            string[] guids = AssetDatabase.FindAssets("t:DishData", new[] { dishesPath });
            
            Debug.Log($"Found {guids.Length} dishes in {dishesPath}:");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                DishData dish = AssetDatabase.LoadAssetAtPath<DishData>(path);
                if (dish != null)
                {
                    Debug.Log($"- {dish.dishName} ({dish.station}, {dish.pickupTime}s)");
                }
            }
            
            Debug.Log("Update the progression_config.json file manually to include these dishes in the appropriate levels.");
        }
    }
}