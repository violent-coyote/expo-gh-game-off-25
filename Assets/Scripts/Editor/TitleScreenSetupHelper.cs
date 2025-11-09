using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Expo.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Expo.Editor
{
    #if UNITY_EDITOR
    /// <summary>
    /// Editor utility to quickly set up a basic title screen for testing.
    /// This creates the minimal UI structure needed for the TitleScreenUI to function.
    /// </summary>
    public static class TitleScreenSetupHelper
    {
        [MenuItem("Expo/Setup Title Screen UI")]
        public static void CreateTitleScreenSetup()
        {
            CreateMinimalTitleScreen();
            Debug.Log("Title Screen setup complete! Assign the created UI elements to the TitleScreenUI component.");
        }
        
        private static void CreateMinimalTitleScreen()
        {
            // Find or create Canvas
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("Canvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
                
                Debug.Log("Created Canvas");
            }
            
            // Create main title screen controller
            GameObject titleScreenGO = new GameObject("TitleScreenUI");
            titleScreenGO.transform.SetParent(canvas.transform, false);
            TitleScreenUI titleScreenUI = titleScreenGO.AddComponent<TitleScreenUI>();
            
            // Create main menu panel
            GameObject mainMenuPanel = CreatePanel("MainMenuPanel", canvas.transform);
            
            // Create buttons in main menu
            GameObject playButton = CreateButton("PlayButton", mainMenuPanel.transform, "PLAY");
            GameObject howToPlayButton = CreateButton("HowToPlayButton", mainMenuPanel.transform, "HOW TO PLAY");
            GameObject quitButton = CreateButton("QuitButton", mainMenuPanel.transform, "QUIT");
            
            // Create save state panel
            GameObject saveStatePanel = CreatePanel("SaveStatePanel", mainMenuPanel.transform);
            GameObject saveStateText = CreateText("SaveStateText", saveStatePanel.transform, "Save State: Loading...");
            GameObject unlockedDishesText = CreateText("UnlockedDishesText", saveStatePanel.transform, "Unlocked Dishes: Loading...");
            GameObject bestScoreText = CreateText("BestScoreText", saveStatePanel.transform, "Best Score: Loading...");
            GameObject sessionsPlayedText = CreateText("SessionsPlayedText", saveStatePanel.transform, "Sessions: Loading...");
            GameObject deleteSaveButton = CreateButton("DeleteSaveButton", saveStatePanel.transform, "DELETE SAVE");
            
            // Create how to play panel
            GameObject howToPlayPanel = CreatePanel("HowToPlayPanel", canvas.transform);
            howToPlayPanel.SetActive(false); // Start hidden
            GameObject howToPlayText = CreateText("HowToPlayText", howToPlayPanel.transform, "Instructions will appear here...");
            GameObject backToMenuButton = CreateButton("BackToMenuButton", howToPlayPanel.transform, "BACK");
            
            // Apply basic layout
            ApplyBasicLayout(mainMenuPanel, saveStatePanel, howToPlayPanel);
            
            Debug.Log("Created minimal title screen UI structure. Remember to assign references in the TitleScreenUI inspector!");
        }
        
        private static GameObject CreatePanel(string name, Transform parent)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            
            Image image = panel.AddComponent<Image>();
            image.color = new Color(0, 0, 0, 0.8f); // Semi-transparent black
            
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            return panel;
        }
        
        private static GameObject CreateButton(string name, Transform parent, string text)
        {
            GameObject buttonGO = new GameObject(name);
            buttonGO.transform.SetParent(parent, false);
            
            Image image = buttonGO.AddComponent<Image>();
            image.color = Color.white;
            
            Button button = buttonGO.AddComponent<Button>();
            
            // Create text child
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            
            TextMeshProUGUI textComponent = textGO.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.color = Color.black;
            textComponent.alignment = TextAlignmentOptions.Center;
            
            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            return buttonGO;
        }
        
        private static GameObject CreateText(string name, Transform parent, string text)
        {
            GameObject textGO = new GameObject(name);
            textGO.transform.SetParent(parent, false);
            
            TextMeshProUGUI textComponent = textGO.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.color = Color.white;
            textComponent.fontSize = 14;
            
            return textGO;
        }
        
        private static void ApplyBasicLayout(GameObject mainMenu, GameObject saveState, GameObject howToPlay)
        {
            // Add vertical layout groups for basic organization
            VerticalLayoutGroup mainLayout = mainMenu.AddComponent<VerticalLayoutGroup>();
            mainLayout.spacing = 10;
            mainLayout.padding = new RectOffset(20, 20, 20, 20);
            
            VerticalLayoutGroup saveLayout = saveState.AddComponent<VerticalLayoutGroup>();
            saveLayout.spacing = 5;
            saveLayout.padding = new RectOffset(10, 10, 10, 10);
            
            VerticalLayoutGroup howToLayout = howToPlay.AddComponent<VerticalLayoutGroup>();
            howToLayout.spacing = 10;
            howToLayout.padding = new RectOffset(20, 20, 20, 20);
            
            Debug.Log("Applied basic layout groups");
        }
    }
    #endif
}