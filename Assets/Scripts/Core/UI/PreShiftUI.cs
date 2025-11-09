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
        
        [Header("Available Dishes")]
        [SerializeField] private bool useProgressionSystem = true;
        [SerializeField] private List<DishData> fallbackDishes = new List<DishData>();
        
        [Header("Settings")]
        [SerializeField] private string expoSceneName = "ExpoScene";
        [SerializeField] private int maxSelectedDishes = 8;
        
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
            
            // Select first few UNLOCKED dishes by default
            SelectDefaultDishes(unlockedDishes);
        }
        
        private void CreateDishButton(DishData dish, bool isUnlocked)
        {
            if (dishButtonPrefab == null || dishListContainer == null) return;
            
            var buttonGO = Instantiate(dishButtonPrefab, dishListContainer);
            var button = buttonGO.GetComponent<Button>();
            var text = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
            
            if (button != null)
            {
                // Only allow clicking on unlocked dishes
                button.interactable = isUnlocked;
                
                if (isUnlocked)
                {
                    button.onClick.AddListener(() => OnDishButtonClicked(dish.dishName, button));
                }
                
                dishButtons.Add(button);
            }
            
            if (text != null)
            {
                // Show locked dishes with visual indicator
                text.text = isUnlocked ? dish.dishName : $"🔒 {dish.dishName}";
                
                // Dim locked dishes
                if (!isUnlocked)
                {
                    text.alpha = 0.5f;
                }
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
            for (int i = 0; i < count && i < dishButtons.Count; i++)
            {
                if (i < availableDishes.Count)
                {
                    OnDishButtonClicked(availableDishes[i].dishName, dishButtons[i]);
                }
            }
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