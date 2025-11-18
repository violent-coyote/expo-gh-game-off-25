using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Expo.Core;
using Expo.Core.Debug;
using Expo.Core.Events;
using Expo.Data;
using Expo.Managers;

namespace Expo.UI
{
    /// <summary>
    /// Manages the table selection menu that is always visible.
    /// Displays all tables and updates their interactability based on table state.
    /// Players can click available tables to send walking dishes.
    /// </summary>
    public class TableSelectionUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private Transform buttonContainer; // Parent with Grid Layout Group

        [Header("Manager References")]
        [SerializeField] private TableManager tableManager;

        private Action<int> _onTableSelected;
        private readonly Dictionary<int, ButtonInfo> _tableButtons = new(); // tableNumber -> button components

        private class ButtonInfo
        {
            public GameObject ButtonObject;
            public Button ButtonComponent;
            public TextMeshProUGUI TextComponent;
        }

        private void Awake()
        {
            // Menu is always visible now
            if (menuPanel != null)
            {
                menuPanel.SetActive(true);
            }
        }

        private void Start()
        {
            // Subscribe to events to update button states
            EventBus.Subscribe<DishesServedEvent>(OnDishesServed);
            EventBus.Subscribe<CourseCompletedEvent>(OnCourseCompleted);
            EventBus.Subscribe<TicketCreatedEvent>(OnTicketCreated);
            EventBus.Subscribe<DishAssignedToTableEvent>(OnDishAssignedToTable);
            
            // Delay initialization to ensure TableManager has created tables
            StartCoroutine(InitializeAfterManagersReady());
        }
        
        private void OnDishAssignedToTable(DishAssignedToTableEvent e)
        {
            // Update button states when a dish is assigned to a table
            UpdateAllButtonStates();
        }

        private System.Collections.IEnumerator InitializeAfterManagersReady()
        {
            // Wait one frame to ensure all managers have initialized
            yield return null;
            
            // Create all table buttons at runtime (once)
            InitializeAllTableButtons();
        }
        
        private float _nextUpdateTime = 0f;
        private const float UPDATE_INTERVAL = 0.5f; // Update every half second
        
        private void Update()
        {
            // Periodically update button states to ensure consistency
            if (Time.time >= _nextUpdateTime)
            {
                UpdateAllButtonStates();
                _nextUpdateTime = Time.time + UPDATE_INTERVAL;
            }
        }

        private void OnTicketCreated(TicketCreatedEvent e)
        {
            // Update button states when a new ticket is created (table seated)
            UpdateAllButtonStates();
        }

        /// <summary>
        /// Creates buttons for all tables at startup.
        /// </summary>
        private void InitializeAllTableButtons()
        {
            if (tableManager == null)
            {
                DebugLogger.LogError(DebugLogger.Category.TABLE_UI, "TableManager reference is missing!");
                return;
            }

            var allTables = tableManager.GetAllTables();
            
            if (allTables == null || allTables.Count == 0)
            {
                DebugLogger.LogWarning(DebugLogger.Category.TABLE_UI, "No tables found in TableManager! Tables may not be initialized yet.");
                return;
            }
            
            DebugLogger.Log(DebugLogger.Category.TABLE_UI, $"Creating buttons for {allTables.Count} tables...");
            
            foreach (var table in allTables)
            {
                CreateTableButton(table);
            }
            
            DebugLogger.Log(DebugLogger.Category.TABLE_UI, $"Initialized {allTables.Count} table buttons");
            
            // Initial update of button states
            UpdateAllButtonStates();
        }

        private void OnDishesServed(DishesServedEvent e)
        {
            // Update button states when dishes are served
            UpdateAllButtonStates();
        }

        private void OnCourseCompleted(CourseCompletedEvent e)
        {
            // Update button states when a course is completed
            UpdateAllButtonStates();
        }

        /// <summary>
        /// Updates all button states based on current table states.
        /// Buttons are enabled only if the table is occupied and not currently eating.
        /// </summary>
        private void UpdateAllButtonStates()
        {
            if (tableManager == null) return;

            var allTables = tableManager.GetAllTables();
            
            foreach (var table in allTables)
            {
                if (_tableButtons.TryGetValue(table.TableNumber, out var buttonInfo))
                {
                    // Enable button only if table is occupied and NOT eating
                    bool isInteractable = table.IsOccupied && !table.IsEating;
                    buttonInfo.ButtonComponent.interactable = isInteractable;
                    
                    DebugLogger.Log(DebugLogger.Category.TABLE_UI,
                        $"Table {table.TableNumber}: IsOccupied={table.IsOccupied}, IsEating={table.IsEating}, Interactable={isInteractable}");
                    
                    // Update visual appearance based on state
                    UpdateButtonAppearance(buttonInfo, table);
                    
                    // Update text to show current state
                    UpdateButtonText(table, buttonInfo.TextComponent);
                }
            }
        }

        /// <summary>
        /// Updates the visual appearance of a button based on table state.
        /// </summary>
        private void UpdateButtonAppearance(ButtonInfo buttonInfo, TableData table)
        {
            var image = buttonInfo.ButtonObject.GetComponent<Image>();
            if (image == null) return;

            if (!table.IsOccupied)
            {
                // Empty table - gray and disabled
                image.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
                DebugLogger.Log(DebugLogger.Category.TABLE_UI, $"Table {table.TableNumber}: EMPTY (gray)");
            }
            else if (table.IsEating)
            {
                // Table is eating - yellow/orange and disabled
                image.color = new Color(1f, 0.7f, 0.2f, 0.8f);
                DebugLogger.Log(DebugLogger.Category.TABLE_UI, $"Table {table.TableNumber}: EATING (orange)");
            }
            else
            {
                // Table is ready for service - green and enabled
                image.color = new Color(0.3f, 1f, 0.3f, 1f);
                DebugLogger.Log(DebugLogger.Category.TABLE_UI, $"Table {table.TableNumber}: READY (green)");
            }
        }

        /// <summary>
        /// Updates the text display for a table button.
        /// </summary>
        private void UpdateButtonText(TableData table, TextMeshProUGUI textComponent)
        {
            if (textComponent == null) return;

            string text = $"Table {table.TableNumber}\n{table.PartySize} guests";
            
            if (table.CurrentTicketId.HasValue)
            {
                text += $"\nTicket #{table.CurrentTicketId.Value}";
            }
            
            if (table.IsEating)
            {
                text += $"\n(Eating...)";
            }
            else if (!table.IsOccupied)
            {
                text += "\n(Empty)";
            }
            
            textComponent.text = text;
        }

        /// <summary>
        /// Sets the callback for when a table is selected.
        /// This is called by ExpoController to register its callback.
        /// </summary>
        public void SetTableSelectedCallback(Action<int> onTableSelected)
        {
            _onTableSelected = onTableSelected;
        }

        /// <summary>
        /// Creates a button for a specific table.
        /// </summary>
        private void CreateTableButton(TableData table)
        {
            if (buttonContainer == null)
            {
                DebugLogger.LogError(DebugLogger.Category.TABLE_UI, "Button container is missing!");
                return;
            }

            // Create button GameObject
            GameObject buttonObj = new GameObject($"Table{table.TableNumber}Button");
            buttonObj.transform.SetParent(buttonContainer, false);

            // Add Image component for button background
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = Color.white;
            
            // Add Button component and configure visual feedback
            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            button.transition = Selectable.Transition.ColorTint;
            
            // Configure color transitions for visual feedback
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f); // Light gray on hover
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f); // Darker gray on press
            colors.selectedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            button.colors = colors;

            // Create text child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            
            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.fontSize = 14;
            textComponent.color = Color.black;
            
            // Set RectTransform to fill parent
            RectTransform textRect = textComponent.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            // Add button click listener
            int tableNumber = table.TableNumber;
            button.onClick.AddListener(() => OnTableButtonClicked(tableNumber));

            // Store button info for later updates
            _tableButtons[table.TableNumber] = new ButtonInfo
            {
                ButtonObject = buttonObj,
                ButtonComponent = button,
                TextComponent = textComponent
            };

            // Set initial text
            UpdateButtonText(table, textComponent);
        }

        /// <summary>
        /// Called when a table button is clicked.
        /// </summary>
        private void OnTableButtonClicked(int tableNumber)
        {
            DebugLogger.Log(DebugLogger.Category.TABLE_UI, $"Table {tableNumber} selected");
            
            // Invoke callback - note: we don't hide the menu anymore, it stays visible
            _onTableSelected?.Invoke(tableNumber);
            
            // Update button states after selection
            UpdateAllButtonStates();
        }

        /// <summary>
        /// Clears all spawned table buttons.
        /// </summary>
        private void ClearButtons()
        {
            foreach (var buttonInfo in _tableButtons.Values)
            {
                if (buttonInfo.ButtonObject != null)
                {
                    Destroy(buttonInfo.ButtonObject);
                }
            }
            _tableButtons.Clear();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<DishesServedEvent>(OnDishesServed);
            EventBus.Unsubscribe<CourseCompletedEvent>(OnCourseCompleted);
            EventBus.Unsubscribe<TicketCreatedEvent>(OnTicketCreated);
            EventBus.Unsubscribe<DishAssignedToTableEvent>(OnDishAssignedToTable);
            ClearButtons();
        }
    }
}
