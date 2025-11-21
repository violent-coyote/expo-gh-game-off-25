using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Expo.Core;
using Expo.Core.Debug;
using Expo.Core.Events;
using Expo.Data;
using Expo.Runtime;

namespace Expo.UI
{
    /// <summary>
    /// Visual representation of a ticket showing what dishes a table currently needs.
    /// REFACTORED: Displays dishes from TableOrderState (dynamic) instead of original ticket (static).
    /// Shows what the table CURRENTLY needs, including dishes reassigned from other tickets.
    /// </summary>
    public class TicketUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI ticketTitle;
        [SerializeField] private Transform dishListParent;
        [SerializeField] private GameObject dishRowPrefab;

        private TicketData _ticketData;
        private int _tableNumber; // Track which table this ticket represents
        private Expo.Managers.TableManager _tableManager; // Reference to query TableOrderState

        private readonly Dictionary<int, GameObject> _dishRows = new(); // instanceId -> row GameObject
        private readonly Dictionary<int, TextMeshProUGUI> _dishTexts = new(); // instanceId -> text component

        public void Init(TicketData data, Expo.Managers.TableManager tableManager)
        {
            _ticketData = data;
            _tableManager = tableManager;
            _tableNumber = data.AssignedTable?.TableNumber ?? 0;
            
            DebugLogger.Log(DebugLogger.Category.TICKET_UI, 
                $"Initializing TicketUI for Table {_tableNumber} (Ticket #{data.TicketId})");
            
            // Update title to show table number
            if (data.AssignedTable != null)
            {
                ticketTitle.text = $"#{data.AssignedTable.TableNumber}";
                
                // Check for mistakes and color accordingly
                UpdateTicketTitleColor();
            }
            else
            {
                ticketTitle.color = Color.red;
                DebugLogger.LogWarning(DebugLogger.Category.TICKET_UI, 
                    $"Ticket #{data.TicketId} has no assigned table!");
            }
            
        // Build UI from TableOrderState instead of original ticket
        RebuildUIFromTableOrderState();
        
        // Subscribe to events
        EventBus.Subscribe<DishesServedEvent>(OnDishesServed);
        EventBus.Subscribe<DishAssignedToTableEvent>(OnDishAssignedToTable);
        EventBus.Subscribe<CourseUnlockedEvent>(OnCourseUnlocked);
    }
    
    /// <summary>
    /// Called when this UI object is enabled. Ensures UI is always up to date.
    /// </summary>
    private void OnEnable()
    {
        // Refresh UI whenever this ticket UI becomes visible/active
        // This ensures the UI reflects current state when switching between tickets
        if (_ticketData != null && _tableManager != null)
        {
            RebuildUIFromTableOrderState();
        }
    }
    
    /// <summary>
    /// Updates the ticket title color based on mistake count.
    /// RED = has mistakes (dishes sent to wrong course)
    /// Default = no mistakes
    /// </summary>
    private void UpdateTicketTitleColor()
    {
        if (_tableManager == null || _ticketData?.AssignedTable == null) return;
        
        var orderState = _tableManager.GetTableOrderState(_tableNumber);
        if (orderState != null && orderState.MistakeCount > 0)
        {
            ticketTitle.color = Color.red;
            DebugLogger.LogWarning(DebugLogger.Category.TICKET_UI,
                $"Table {_tableNumber}: ⚠️ {orderState.MistakeCount} mistake(s) - ticket marked RED");
        }
        else
        {
            ticketTitle.color = Color.white; // or your default color
        }
    }
    
    /// <summary>
    /// Rebuilds the entire dish list UI by querying the current TableOrderState.
        /// This shows what the table CURRENTLY needs (including reassigned dishes).
        /// </summary>
        private void RebuildUIFromTableOrderState()
        {
            // Clear existing UI - destroy ALL children (including course separators)
            foreach (Transform child in dishListParent)
            {
                if (child != null) Destroy(child.gameObject);
            }
            
            _dishRows.Clear();
            _dishTexts.Clear();
            
            if (_tableManager == null)
            {
                DebugLogger.LogError(DebugLogger.Category.TICKET_UI, 
                    $"Table {_tableNumber}: Cannot rebuild UI - TableManager is null!");
                return;
            }
            
            // Get the table's current order state
            var orderState = _tableManager.GetTableOrderState(_tableNumber);
            if (orderState == null)
            {
                DebugLogger.LogWarning(DebugLogger.Category.TICKET_UI, 
                    $"Table {_tableNumber}: No order state found!");
                return;
            }
            
            // Get all dish expectations from the table's order state
            var allExpectations = orderState.GetAllExpectations();
            DebugLogger.Log(DebugLogger.Category.TICKET_UI, 
                $"Table {_tableNumber}: Building UI from {allExpectations.Count} expectations in TableOrderState");
            
            // Group by course number
            var courseGroups = allExpectations.GroupBy(e => e.CourseNumber).OrderBy(g => g.Key);
            
            foreach (var courseGroup in courseGroups)
            {
                CreateCourseSeparator(courseGroup.Key);
                
                foreach (var expectation in courseGroup)
                {
                    AddDishExpectationToDisplay(expectation);
                }
            }
            
            DebugLogger.Log(DebugLogger.Category.TICKET_UI, 
                $"Table {_tableNumber}: UI rebuild complete - showing {_dishTexts.Count} dishes");
            
            // Update title color based on mistakes
            UpdateTicketTitleColor();
        }
        
        /// <summary>
        /// Adds a dish expectation from TableOrderState to the display.
        /// This shows what the table currently needs (including reassigned dishes).
        /// </summary>
        private void AddDishExpectationToDisplay(DishExpectation expectation)
        {
            // Don't add if already displayed
            if (_dishRows.ContainsKey(expectation.DishInstanceId))
            {
                return;
            }
            
            var row = Instantiate(dishRowPrefab, dishListParent);
            var text = row.GetComponentInChildren<TextMeshProUGUI>();
            text.text = expectation.DishType.dishName;
            
            // If already served, show struck through
            if (expectation.IsServed)
            {
                text.fontStyle = FontStyles.Strikethrough;
                text.color = Color.gray;
            }

            // Find the DishState from the original ticket for tracking purposes
            DishState dishState = FindDishStateByInstanceId(expectation.DishInstanceId);
            
            // Update visual appearance for locked courses
            if (dishState != null)
            {
                var table = _ticketData.AssignedTable;
                if (table != null)
                {
                    bool isCourseUnlocked = table.IsCourseUnlocked(expectation.CourseNumber);
                    
                    if (!isCourseUnlocked)
                    {
                        text.color = Color.gray;
                        // text.text = $"{expectation.DishType.dishName}(C{expectation.CourseNumber})";
                    }
                }
            }
            
            _dishRows[expectation.DishInstanceId] = row;
            _dishTexts[expectation.DishInstanceId] = text;

            DebugLogger.Log(DebugLogger.Category.TICKET_UI,
                $"Table {_tableNumber}: Added '{expectation.DishType.dishName}' (instance {expectation.DishInstanceId}, served: {expectation.IsServed})");
        }
        
        /// <summary>
        /// Finds a DishState in the original ticket by instance ID.
        /// Returns null if this dish is from another ticket.
        /// </summary>
        private DishState FindDishStateByInstanceId(int instanceId)
        {
            foreach (var course in _ticketData.Courses)
            {
                foreach (var dish in course.Dishes)
                {
                    if (dish.DishInstanceId == instanceId)
                        return dish;
                }
            }
            return null;
        }
        
        /// <summary>
        /// Refreshes the entire UI by rebuilding from TableOrderState.
        /// Called when a course is unlocked after eating time.
        /// </summary>
        public void RefreshCourseButtons()
        {
            // Rebuild the entire UI to reflect current TableOrderState and course unlock status
            RebuildUIFromTableOrderState();
        }
        
        /// <summary>
        /// Creates a simple course separator with text dynamically.
        /// </summary>
        private void CreateCourseSeparator(int courseNumber)
        {
            // Create a simple GameObject with TextMeshProUGUI
            var separatorObj = new GameObject($"CourseSeparator_{courseNumber}");
            separatorObj.transform.SetParent(dishListParent, false);
            
            // Add RectTransform
            var rectTransform = separatorObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(200, 25);
            
            // Add TextMeshProUGUI
            var textComponent = separatorObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = $"═══ Course {courseNumber} ═══";
            textComponent.fontSize = 16;
            textComponent.fontStyle = FontStyles.Bold | FontStyles.Italic;
            textComponent.color = Color.black;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.margin = new Vector4(0, 5, 0, 5);
            
            // Optional: Add LayoutElement for better spacing
            var layoutElement = separatorObj.AddComponent<LayoutElement>();
            layoutElement.minHeight = 25;
            layoutElement.preferredHeight = 25;
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<DishesServedEvent>(OnDishesServed);
            EventBus.Unsubscribe<DishAssignedToTableEvent>(OnDishAssignedToTable);
            EventBus.Unsubscribe<CourseUnlockedEvent>(OnCourseUnlocked);
        }
        
        private void OnCourseUnlocked(CourseUnlockedEvent e)
        {
            // Only rebuild if this is our ticket
            if (e.TicketId != _ticketData?.TicketId)
            {
                return;
            }
            
            DebugLogger.Log(DebugLogger.Category.TICKET_UI,
                $"Table {_tableNumber}: Course {e.CourseNumber} unlocked - rebuilding UI");
            
            // Rebuild UI to show newly unlocked course with enabled buttons
            RebuildUIFromTableOrderState();
        }
        
        private void OnDishAssignedToTable(DishAssignedToTableEvent e)
        {
            // NOTE: We don't need to rebuild UI here anymore because AssignDishToTable
            // doesn't actually add dishes to the table's expectations - it just validates.
            // The UI will be updated when OnDishesServed fires and marks expectations as served.
            
            // Log for debugging purposes only
            if (e.TableNumber == _tableNumber)
            {
                DebugLogger.Log(DebugLogger.Category.TICKET_UI,
                    $"Table {_tableNumber}: Dish {e.DishInstanceId} ({e.DishData?.dishName ?? "Unknown"}) assigned (will update on serve)");
            }
        }

        private void OnDishesServed(DishesServedEvent e)
        {
            // Update the UI to reflect what TableOrderState says is served
            DebugLogger.Log(DebugLogger.Category.TICKET_UI,
                $"[Table {_tableNumber}] OnDishesServed: {e.DishInstanceIds.Count} dishes served to table {e.TableNumber}");
            
            // If dishes were sent to a different table, ignore them
            if (e.TableNumber != _tableNumber)
            {
                return;
            }
            
            if (_tableManager == null)
            {
                DebugLogger.LogError(DebugLogger.Category.TICKET_UI, 
                    $"Table {_tableNumber}: Cannot update UI - TableManager is null!");
                return;
            }
            
            // Get the current order state to see what's actually been marked as served
            var orderState = _tableManager.GetTableOrderState(_tableNumber);
            if (orderState == null)
            {
                DebugLogger.LogWarning(DebugLogger.Category.TICKET_UI, 
                    $"Table {_tableNumber}: No order state found!");
                return;
            }
            
            var allExpectations = orderState.GetAllExpectations();
            int markedCount = 0;
            
            // Update each dish in our UI based on TableOrderState
            foreach (var kvp in _dishTexts.ToList()) // ToList to avoid modification during iteration
            {
                var dishInstanceId = kvp.Key;
                var text = kvp.Value;
                
                // Find this expectation in the order state
                var expectation = allExpectations.FirstOrDefault(exp => exp.DishInstanceId == dishInstanceId);
                
                if (expectation != null && expectation.IsServed)
                {
                    // This expectation is marked as served - strikethrough if not already
                    if ((text.fontStyle & FontStyles.Strikethrough) == 0)
                    {
                        text.fontStyle = FontStyles.Strikethrough;
                        text.color = Color.gray;
                        markedCount++;

                        DebugLogger.Log(DebugLogger.Category.TICKET_UI,
                            $"✓ [Table {_tableNumber}] Struck through expectation {dishInstanceId} ({expectation.DishType.dishName})");
                    }
                }
            }
            
            DebugLogger.Log(DebugLogger.Category.TICKET_UI,
                $"[Table {_tableNumber}] Strikethrough complete: {markedCount} dishes newly marked");
        }

        /// <summary>
        /// Fires all available dishes from unlocked courses on this specific ticket that haven't been fired yet.
        /// REFACTORED: Uses table's course unlock state instead of CourseData.IsUnlocked
        /// </summary>
        public void FireAllAvailableDishesOnThisTicket()
        {
            int firedCount = 0;
            var table = _ticketData.AssignedTable;
            
            if (table == null)
            {
                DebugLogger.LogWarning(DebugLogger.Category.UI, $"Cannot fire dishes - no table assigned to ticket #{_ticketData.TicketId}");
                return;
            }
            
            foreach (var course in _ticketData.Courses)
            {
                // REFACTORED: Check if course is unlocked via table state
                if (!table.IsCourseUnlocked(course.CourseNumber))
                {
                    DebugLogger.Log(DebugLogger.Category.UI, 
                        $"Skipping course {course.CourseNumber} - table {table.TableNumber} hasn't unlocked it yet");
                    continue;
                }
                
                foreach (var dish in course.Dishes)
                {
                    // Only fire dishes that haven't been fired yet
                    if (dish.Status == DishStatus.NotFired)
                    {
                        DebugLogger.Log(DebugLogger.Category.UI, $"Fire the Board: Firing {dish.Data.dishName}");
                        
                        // Fire the dish
                        EventBus.Publish(new DishFiredEvent
                        {
                            DishData = dish.Data,
                            DishState = dish,
                            Station = dish.Data.station,
                            DishInstanceId = dish.DishInstanceId,
                            Timestamp = GameTime.Time,
                            ExpectedReadyTime = GameTime.Time + dish.Data.pickupTime
                        });
                        
                        firedCount++;
                    }
                    else
                    {
                        DebugLogger.Log(DebugLogger.Category.UI, $"Fire the Board: Skipping {dish.Data.dishName} - status: {dish.Status}");
                    }
                }
            }
            
            DebugLogger.Log(DebugLogger.Category.UI, $"Fire all available dishes completed: {firedCount} dishes fired from table {_tableNumber}");
        }
    }
}
