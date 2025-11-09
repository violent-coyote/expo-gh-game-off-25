using UnityEngine;
using Expo.Core.Debug;
using Expo.Core.Progression;

namespace Expo.Core
{
    /// <summary>
    /// Bootstrap component that ensures ProgressionManager exists in any scene.
    /// Add this to any scene where you want to guarantee ProgressionManager is available,
    /// even when not starting from the title screen.
    /// </summary>
    public class ProgressionBootstrap : MonoBehaviour
    {
        [Header("Bootstrap Settings")]
        [SerializeField] private bool createProgressionManagerIfMissing = true;
        [SerializeField] private bool debugBootstrapProcess = true;
        
        private void Awake()
        {
            if (debugBootstrapProcess)
            {
                DebugLogger.Log(DebugLogger.Category.PROGRESSION, "ProgressionBootstrap checking for ProgressionManager...");
            }
            
            // Check if ProgressionManager already exists
            if (ProgressionManager.Instance != null)
            {
                if (debugBootstrapProcess)
                {
                    DebugLogger.Log(DebugLogger.Category.PROGRESSION, "✅ ProgressionManager already exists, bootstrap not needed");
                }
                
                // We can destroy this bootstrap since ProgressionManager exists
                Destroy(gameObject);
                return;
            }
            
            // ProgressionManager doesn't exist, create one if enabled
            if (createProgressionManagerIfMissing)
            {
                CreateProgressionManager();
            }
            else
            {
                DebugLogger.LogWarning(DebugLogger.Category.PROGRESSION, 
                    "ProgressionManager not found and creation is disabled. " +
                    "Enable 'createProgressionManagerIfMissing' or start from Title Scene.");
            }
            
            // Clean up bootstrap
            Destroy(gameObject);
        }
        
        private void CreateProgressionManager()
        {
            DebugLogger.Log(DebugLogger.Category.PROGRESSION, "🔧 Creating ProgressionManager via Bootstrap...");
            
            // Create GameObject for ProgressionManager
            GameObject progressionManagerGO = new GameObject("ProgressionManager (Bootstrap)");
            
            // Add ProgressionManager component
            ProgressionManager progressionManager = progressionManagerGO.AddComponent<ProgressionManager>();
            
            if (debugBootstrapProcess)
            {
                DebugLogger.Log(DebugLogger.Category.PROGRESSION, 
                    "✅ ProgressionManager created successfully via Bootstrap. " +
                    "Note: For production, start from Title Scene for proper initialization order.");
            }
        }
        
        /// <summary>
        /// Quick setup method - adds ProgressionBootstrap to current scene
        /// </summary>
        [ContextMenu("Add To Current Scene")]
        public void AddToCurrentScene()
        {
            #if UNITY_EDITOR
            GameObject bootstrapGO = new GameObject("ProgressionBootstrap");
            bootstrapGO.AddComponent<ProgressionBootstrap>();
            UnityEditor.Selection.activeGameObject = bootstrapGO;
            DebugLogger.Log(DebugLogger.Category.PROGRESSION, "ProgressionBootstrap added to current scene");
            #endif
        }
    }
}