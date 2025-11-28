using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Expo.Data;
using Expo.Core.Debug;
using Expo.Core.Progression;

namespace Expo.UI
{
    /// <summary>
    /// Simple pre-shift scene for dish selection.
    /// Allows players to choose which dishes will be available during their shift.
    /// </summary>
    public class PreShiftUI : MonoBehaviour
    {
        [Header("Dish Selection")]
        [SerializeField] private Transform dishListContainer;
        [SerializeField] private GameObject dishButtonPrefab;
        [SerializeField] private Button startShiftButton;
        [SerializeField] private TMP_FontAsset customFont; // Rainhearts Bitmap font
        
        [Header("Available Dishes")]
        [SerializeField] private bool useProgressionSystem = true;
        [SerializeField] private List<DishData> fallbackDishes = new List<DishData>();
        
        [Header("Settings")]
        [SerializeField] private string expoSceneName = "ExpoScene";
        [SerializeField] private int maxSelectedDishes = 3;
        
        [HideInInspector] private List<string> selectedDishIds = new List<string>();
        [HideInInspector] private List<Button> dishButtons = new List<Button>();
        [HideInInspector] private HashSet<string> unlockedDishIds = new HashSet<string>();
        
        // Static property to pass selected dishes to the expo scene
        public static List<string> SelectedDishesForShift { get; private set; } = new List<string>();
        
        private void Start()
        {
            SetupUI();
            PopulateDishList();
        }
        
        private void SetupUI()
        {
            if (startShiftButton != null)
                startShiftButton.onClick.AddListener(StartShift);
        }
        
        private void PopulateDishList()
        {
            // Clear existing items
            foreach (Transform child in dishListContainer)
            {
                Destroy(child.gameObject);
            }
            dishButtons.Clear();
            selectedDishIds.Clear();
            unlockedDishIds.Clear();
            
            // Get ALL dishes and unlocked dishes separately
            var allDishes = GetAllDishes();
            var unlockedDishes = GetUnlockedDishes();
            
            // Build a set of unlocked dish IDs for quick lookup
            foreach (var dish in unlockedDishes)
            {
                unlockedDishIds.Add(dish.dishName);
            }
            
            // Create button for EVERY dish (both locked and unlocked)
            foreach (var dish in allDishes)
            {
                bool isUnlocked = unlockedDishIds.Contains(dish.dishName);
                CreateDishButton(dish, isUnlocked);
            }
            
            // Don't auto-select any dishes - player must choose manually
            UpdateStartButton();
        }
        
        private void CreateDishButton(DishData dish, bool isUnlocked)
        {
            if (dishButtonPrefab == null || dishListContainer == null) return;
            
            // Create a container for the button and text
            GameObject containerGO = new GameObject($"{dish.dishName}Container");
            containerGO.transform.SetParent(dishListContainer, false);
            
            // Add horizontal layout group to arrange button and text side by side
            HorizontalLayoutGroup layoutGroup = containerGO.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.childAlignment = TextAnchor.MiddleLeft;
            layoutGroup.spacing = 10;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            
            // Add RectTransform to container
            RectTransform containerRect = containerGO.GetComponent<RectTransform>();
            containerRect.sizeDelta = new Vector2(400, 150); // Width for button + text, height matches button
            
            var buttonGO = Instantiate(dishButtonPrefab, containerGO.transform);
            var button = buttonGO.GetComponent<Button>();
            
            // Set button size to 150x150
            RectTransform buttonRect = buttonGO.GetComponent<RectTransform>();
            if (buttonRect != null)
            {
                buttonRect.sizeDelta = new Vector2(150, 150);
            }
            
            if (button != null)
            {
                // Disable navigation to prevent button staying selected after click
                Navigation nav = button.navigation;
                nav.mode = Navigation.Mode.None;
                button.navigation = nav;
                
                // Only allow clicking on unlocked dishes
                button.interactable = isUnlocked;
                
                if (isUnlocked)
                {
                    button.onClick.AddListener(() => OnDishButtonClicked(dish.dishName, button));
                }
                
                dishButtons.Add(button);
            }
            
            // Create icon child
            if (dish.icon != null)
            {
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(buttonGO.transform, false);
                
                Image iconImage = iconObj.AddComponent<Image>();
                iconImage.sprite = dish.icon;
                iconImage.preserveAspect = true; // Maintain aspect ratio
                
                // Set RectTransform with padding to fit nicely within button
                RectTransform iconRect = iconImage.GetComponent<RectTransform>();
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                iconRect.sizeDelta = new Vector2(-10, -10); // 5px padding on all sides
                iconRect.anchoredPosition = Vector2.zero;
                
                // Dim icon for locked dishes
                if (!isUnlocked)
                {
                    iconImage.color = new Color(1f, 1f, 1f, 0.5f);
                }
            }
            
            // Create text info to the right of the button
            GameObject textObj = new GameObject("DishInfo");
            textObj.transform.SetParent(containerGO.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(200, 150);
            
            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = isUnlocked ? 
                $"{dish.dishName}\n{dish.pickupTime}s pickup" : 
                $"LOCKED {dish.dishName}\n{dish.pickupTime}s pickup";
            textComponent.fontSize = 48;
            textComponent.color = Color.black;
            textComponent.alignment = TextAlignmentOptions.Left;
            textComponent.verticalAlignment = VerticalAlignmentOptions.Middle;
            
            // Apply custom font if available
            if (customFont != null)
            {
                textComponent.font = customFont;
            }
            
            // Set visual state for locked dishes
            if (!isUnlocked)
            {
                UpdateButtonVisuals(button, false, true);
            }
        }
        
        /// <summary>
        /// Get ALL dishes (both locked and unlocked) for display
        /// </summary>
        private List<DishData> GetAllDishes()
        {
            if (useProgressionSystem && ProgressionManager.Instance != null)
            {
                return ProgressionManager.Instance.allAvailableDishes;
            }
            
            // Fallback to manual list if progression system not available
            DebugLogger.LogWarning(DebugLogger.Category.UI, "ProgressionManager not available, using fallback dishes");
            return fallbackDishes;
        }
        
        /// <summary>
        /// Get only unlocked dishes from progression system
        /// </summary>
        private List<DishData> GetUnlockedDishes()
        {
            if (useProgressionSystem && ProgressionManager.Instance != null)
            {
                var unlockedDishes = ProgressionManager.Instance.GetUnlockedDishes();
                if (unlockedDishes.Count > 0)
                {
                    return unlockedDishes;
                }
            }
            
            // Fallback to manual list if progression system not available
            return fallbackDishes;
        }
        
        private void SelectDefaultDishes(List<DishData> availableDishes)
        {
            // Select first 4 dishes by default
            int count = Mathf.Min(4, availableDishes.Count);
            for (int i = 0; i < count; i++)
            {
                string dishId = availableDishes[i].dishName;
                // Find the button that corresponds to this dish
                Button button = FindButtonForDish(dishId);
                if (button != null)
                {
                    // Directly add to selected and update visuals
                    selectedDishIds.Add(dishId);
                    UpdateButtonVisuals(button, true, false);
                }
            }
            UpdateStartButton();
        }
        
        private Button FindButtonForDish(string dishId)
        {
            // Find the button container that has the dishId in its name
            for (int i = 0; i < dishListContainer.childCount; i++)
            {
                Transform child = dishListContainer.GetChild(i);
                if (child.name == $"{dishId}Container")
                {
                    // Get the button from the container's first child
                    if (child.childCount > 0)
                    {
                        return child.GetChild(0).GetComponent<Button>();
                    }
                }
            }
            return null;
        }
        
        private void OnDishButtonClicked(string dishId, Button button)
        {
            // Only allow interaction with unlocked dishes
            if (!unlockedDishIds.Contains(dishId))
            {
                DebugLogger.LogWarning(DebugLogger.Category.UI, $"Attempted to select locked dish: {dishId}");
                return;
            }
            
            if (selectedDishIds.Contains(dishId))
            {
                // Deselect dish
                selectedDishIds.Remove(dishId);
                UpdateButtonVisuals(button, false, false);
            }
            else
            {
                if (selectedDishIds.Count < maxSelectedDishes)
                {
                    // Select dish
                    selectedDishIds.Add(dishId);
                    UpdateButtonVisuals(button, true, false);
                }
                // If max reached, do nothing (could add feedback here)
            }
            
            UpdateStartButton();
        }
        
        private void UpdateButtonVisuals(Button button, bool isSelected, bool isLocked)
        {
            if (button == null) return;
            
            var colors = button.colors;
            if (isLocked)
            {
                // Locked dishes: dark gray, non-interactable
                colors.normalColor = new Color(0.3f, 0.3f, 0.3f, 1f);
                colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
                colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            }
            else if (isSelected)
            {
                // Selected dishes: green
                colors.normalColor = Color.green;
                colors.highlightedColor = Color.green * 0.8f;
            }
            else
            {
                // Unselected but unlocked: white
                colors.normalColor = Color.white;
                colors.highlightedColor = Color.gray;
            }
            button.colors = colors;
        }
        
        private void UpdateStartButton()
        {
            // Enable start button only if at least one dish is selected
            if (startShiftButton != null)
            {
                startShiftButton.interactable = selectedDishIds.Count > 0;
            }
        }
        
        private void StartShift()
        {
            if (selectedDishIds.Count == 0)
            {
                DebugLogger.LogWarning(DebugLogger.Category.UI, "No dishes selected for shift!");
                return;
            }
            
            // Store selected dishes for the expo scene
            SelectedDishesForShift = new List<string>(selectedDishIds);
            
            DebugLogger.Log(DebugLogger.Category.UI, $"Starting shift with {selectedDishIds.Count} dishes: {string.Join(", ", selectedDishIds)}");
            
            // Load expo scene
            SceneManager.LoadScene(expoSceneName);
        }
        
        /// <summary>
        /// Get the dishes selected for the current shift (for use by other systems)
        /// </summary>
        public static List<string> GetSelectedDishIds()
        {
            return new List<string>(SelectedDishesForShift);
        }
        
        /// <summary>
        /// Clear the selected dishes (call when shift ends)
        /// </summary>
        public static void ClearSelectedDishes()
        {
            SelectedDishesForShift.Clear();
        }
    }
}