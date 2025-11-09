using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Expo.Editor
{
    /// <summary>
    /// Editor script that automatically loads the Title Scene when entering play mode,
    /// regardless of which scene is currently open. This ensures ProgressionManager
    /// and other persistent systems are properly initialized during development.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeSceneLoader
    {
        private const string TITLE_SCENE_PATH = "Assets/Scenes/TitleScene.unity";
        private const string ENABLE_AUTO_LOAD_KEY = "Expo.AutoLoadTitleScene";
        private const string ORIGINAL_SCENE_KEY = "Expo.OriginalScenePath";
        private const string SHOULD_RESTORE_KEY = "Expo.ShouldRestoreScene";
        
        static PlayModeSceneLoader()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
        
        [MenuItem("Expo/Toggle Auto-Load Title Scene")]
        public static void ToggleAutoLoadTitleScene()
        {
            bool isEnabled = EditorPrefs.GetBool(ENABLE_AUTO_LOAD_KEY, true);
            EditorPrefs.SetBool(ENABLE_AUTO_LOAD_KEY, !isEnabled);
            
            string status = !isEnabled ? "ENABLED" : "DISABLED";
            Debug.Log($"Auto-load Title Scene: {status}");
            
            // Update menu checkmark
            Menu.SetChecked("Expo/Toggle Auto-Load Title Scene", !isEnabled);
        }
        
        [MenuItem("Expo/Toggle Auto-Load Title Scene", true)]
        public static bool ValidateToggleAutoLoadTitleScene()
        {
            bool isEnabled = EditorPrefs.GetBool(ENABLE_AUTO_LOAD_KEY, true);
            Menu.SetChecked("Expo/Toggle Auto-Load Title Scene", isEnabled);
            return true;
        }
        
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Check if auto-load is enabled
            if (!EditorPrefs.GetBool(ENABLE_AUTO_LOAD_KEY, true))
                return;
                
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                    HandleExitingEditMode();
                    break;
                    
                case PlayModeStateChange.EnteredEditMode:
                    HandleEnteredEditMode();
                    break;
            }
        }
        
        private static void HandleExitingEditMode()
        {
            // Store the current scene so we can restore it later
            var currentScene = EditorSceneManager.GetActiveScene();
            string originalScenePath = currentScene.path;
            
            // Check if we're already in the title scene
            if (originalScenePath == TITLE_SCENE_PATH)
            {
                EditorPrefs.SetBool(SHOULD_RESTORE_KEY, false);
                return;
            }
            
            // Check if title scene exists
            if (!System.IO.File.Exists(TITLE_SCENE_PATH))
            {
                Debug.LogWarning($"Title scene not found at {TITLE_SCENE_PATH}. Skipping auto-load.");
                EditorPrefs.SetBool(SHOULD_RESTORE_KEY, false);
                return;
            }
            
            // Save current scene if it has unsaved changes
            if (currentScene.isDirty)
            {
                EditorSceneManager.SaveScene(currentScene);
            }
            
            // Store the original scene path in EditorPrefs so it persists
            EditorPrefs.SetString(ORIGINAL_SCENE_KEY, originalScenePath);
            EditorPrefs.SetBool(SHOULD_RESTORE_KEY, true);
            
            // Load title scene
            EditorSceneManager.OpenScene(TITLE_SCENE_PATH);
            
            Debug.Log($"Auto-loaded Title Scene for play mode. Will restore {System.IO.Path.GetFileName(originalScenePath)} when stopping.");
        }
        
        private static void HandleEnteredEditMode()
        {
            // Restore the original scene when exiting play mode
            bool shouldRestore = EditorPrefs.GetBool(SHOULD_RESTORE_KEY, false);
            string originalScenePath = EditorPrefs.GetString(ORIGINAL_SCENE_KEY, "");
            
            if (shouldRestore && !string.IsNullOrEmpty(originalScenePath))
            {
                if (System.IO.File.Exists(originalScenePath))
                {
                    EditorSceneManager.OpenScene(originalScenePath);
                    Debug.Log($"Restored original scene: {System.IO.Path.GetFileName(originalScenePath)}");
                }
                else
                {
                    Debug.LogWarning($"Could not restore original scene. File not found: {originalScenePath}");
                }
                
                // Clear the stored values
                EditorPrefs.DeleteKey(SHOULD_RESTORE_KEY);
                EditorPrefs.DeleteKey(ORIGINAL_SCENE_KEY);
            }
        }
        
        /// <summary>
        /// Manual method to set the title scene path if it's different
        /// </summary>
        [MenuItem("Expo/Set Title Scene Path")]
        public static void SetTitleScenePath()
        {
            string path = EditorUtility.OpenFilePanel("Select Title Scene", "Assets/Scenes", "unity");
            
            if (!string.IsNullOrEmpty(path))
            {
                // Convert absolute path to relative
                if (path.StartsWith(Application.dataPath))
                {
                    path = "Assets" + path.Substring(Application.dataPath.Length);
                }
                
                EditorPrefs.SetString("Expo.TitleScenePath", path);
                Debug.Log($"Title scene path set to: {path}");
            }
        }
        
        /// <summary>
        /// Get the configured title scene path
        /// </summary>
        private static string GetTitleScenePath()
        {
            return EditorPrefs.GetString("Expo.TitleScenePath", TITLE_SCENE_PATH);
        }
    }
}