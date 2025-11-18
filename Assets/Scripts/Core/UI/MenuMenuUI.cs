using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Expo.Core.Debug;
using Expo.Data;
using Expo.Runtime;
using Expo.Core.Events;
using Expo.Core.Managers;
using Expo.Core;

namespace Expo.UI
{
    /// <summary>
    /// Manages the restaurant menu UI that allows firing any available dish regardless of tickets.
    /// Similar to TableSelectionUI but shows DishData instead of tables and fires dishes instead of serving.
    /// </summary>
    public class MenuMenuUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private Transform buttonContainer; // Parent with Grid Layout Group

        [Header("Manager References")]
        [SerializeField] private TicketManager ticketManager;

        private readonly List<GameObject> _spawnedButtons = new();
        private readonly List<DishState> _standaloneDishes = new(); // Track standalone dishes for cleanup
        private int _standaloneDishCounter = 0;

        private void Start()
        {
            // Menu is always visible - ensure panel is active
            if (menuPanel != null)
            {
                menuPanel.SetActive(true);
            }

            // Delay population to ensure TicketManager has finished initialization
            StartCoroutine(PopulateMenuDelayed());
        }

        /// <summary>
        /// Delays menu population to ensure TicketManager has loaded and filtered dishes.
        /// </summary>
        private IEnumerator PopulateMenuDelayed()
        {
            if (ticketManager == null)
            {
                DebugLogger.LogError(DebugLogger.Category.UI, "TicketManager reference is missing!");
                yield break;
            }

            // Wait one frame to ensure all managers have initialized
            yield return null;

            // Try to populate, retry if no dishes available yet
            int retryCount = 0;
            const int maxRetries = 10;
            
            while (retryCount < maxRetries)
            {
                var availableDishes = ticketManager.GetAvailableDishes();
                
                if (availableDishes != null && availableDishes.Count > 0)
                {
                    PopulateMenu();
                    yield break;
                }
                
                // Wait a bit and retry
                retryCount++;
                DebugLogger.Log(DebugLogger.Category.UI, $"MenuMenu: Waiting for dishes to load (attempt {retryCount}/{maxRetries})...");
                yield return new WaitForSeconds(0.1f);
            }
            
            DebugLogger.LogWarning(DebugLogger.Category.UI, "MenuMenu: Failed to load dishes after multiple attempts!");
        }

        /// <summary>
        /// Populates the menu with available dishes from TicketManager.
        /// </summary>
        private void PopulateMenu()
        {
            if (ticketManager == null)
            {
                DebugLogger.LogError(DebugLogger.Category.UI, "TicketManager reference is missing!");
                return;
            }

            // Clear existing buttons
            ClearButtons();
            
            // Get available dishes from TicketManager
            var availableDishes = ticketManager.GetAvailableDishes();
            
            if (availableDishes == null || availableDishes.Count == 0)
            {
                DebugLogger.LogWarning(DebugLogger.Category.UI, "No available dishes to display!");
                return;
            }

            // Create button for each available dish
            foreach (var dishData in availableDishes)
            {
                CreateDishButton(dishData);
            }
            
            DebugLogger.Log(DebugLogger.Category.UI, $"MenuMenu populated with {availableDishes.Count} available dishes");
        }

        /// <summary>
        /// Creates a button for a specific dish.
        /// </summary>
        private void CreateDishButton(DishData dishData)
        {
            if (buttonContainer == null)
            {
                DebugLogger.LogError(DebugLogger.Category.UI, "Button container is missing!");
                return;
            }

            // Create button GameObject
            GameObject buttonObj = new GameObject($"{dishData.dishName}Button");
            buttonObj.transform.SetParent(buttonContainer, false);
            _spawnedButtons.Add(buttonObj);

            // Add Button component
            Button button = buttonObj.AddComponent<Button>();
            
            // Add Image component for button background
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = Color.white;

            // Create text child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            
            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = $"{dishData.dishName}\n{dishData.station}\n{dishData.pickupTime}s";
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.fontSize = 12;
            textComponent.color = Color.black;
            
            // Set RectTransform to fill parent
            RectTransform textRect = textComponent.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            // Add button click listener
            button.onClick.AddListener(() => OnDishButtonClicked(dishData));
        }

        /// <summary>
        /// Called when a dish button is clicked. Fires the dish immediately.
        /// </summary>
        private void OnDishButtonClicked(DishData dishData)
        {
            DebugLogger.Log(DebugLogger.Category.UI, $"MenuMenu: Firing {dishData.dishName}");
            
            // Generate unique instance ID for standalone dish
            int dishInstanceId = GenerateStandaloneDishId();
            
            // Create DishState
            var dishState = new DishState(dishData, dishInstanceId);
            
            // Track this standalone dish for cleanup
            _standaloneDishes.Add(dishState);
            
            // Publish DishFiredEvent - StationManager will handle the cooking
            EventBus.Publish(new DishFiredEvent
            {
                DishData = dishData,
                DishState = dishState,
                Station = dishData.station,
                DishInstanceId = dishInstanceId,
                Timestamp = GameTime.Time,
                ExpectedReadyTime = GameTime.Time + dishData.pickupTime
            });
            
            DebugLogger.Log(DebugLogger.Category.UI, $"MenuMenu: Published DishFiredEvent for {dishData.dishName} (ID: {dishInstanceId})");
        }

        /// <summary>
        /// Generates a unique dish instance ID for standalone dishes.
        /// Uses high base number (999000+) to avoid conflicts with ticket-based IDs.
        /// </summary>
        private int GenerateStandaloneDishId()
        {
            // Ticket-based IDs use: ticketId * 1000 + index (e.g., 1000, 2000, 3000)
            // Use 999000 as base to avoid conflicts
            return 999000 + (_standaloneDishCounter++);
        }

        /// <summary>
        /// Clears all spawned dish buttons.
        /// </summary>
        private void ClearButtons()
        {
            foreach (var button in _spawnedButtons)
            {
                if (button != null)
                {
                    DestroyImmediate(button);
                }
            }
            _spawnedButtons.Clear();
        }

        /// <summary>
        /// Cleanup standalone dishes that are no longer active (served or dead).
        /// Called periodically to prevent memory leaks.
        /// </summary>
        private void Update()
        {
            // Clean up standalone dishes that are no longer active
            for (int i = _standaloneDishes.Count - 1; i >= 0; i--)
            {
                var dish = _standaloneDishes[i];
                if (dish.Status == DishStatus.Served || dish.Status == DishStatus.Dead)
                {
                    _standaloneDishes.RemoveAt(i);
                }
            }
        }

        private void OnDestroy()
        {
            ClearButtons();
            _standaloneDishes.Clear();
        }
    }
}