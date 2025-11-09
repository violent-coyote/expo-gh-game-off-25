using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Expo.Editor
{
    /// <summary>
    /// Automatically sets up Resources folder before builds to ensure WebGL and other platforms
    /// have access to dishes and progression config at runtime.
    /// </summary>
    public class PreBuildSetup : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            Debug.Log($"[Pre-Build] Setting up Resources for {report.summary.platform} build...");
            
            // Automatically run the dish resources setup
            DishResourceSetup.SetupDishResources();
            
            Debug.Log("[Pre-Build] Resources setup complete!");
        }
    }
}
