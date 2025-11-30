using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Expo.Core;
using Expo.Data;
using Expo.Runtime;
using Expo.Core.Events;
using Expo.Core.Managers;
using Expo.Core.Debug;
using Expo.Core.Progression;

namespace Expo.Managers
{
    /// <summary>
    /// Manages tables in the restaurant.
    /// RESPONSIBILITIES:
    /// - Owns all table state (occupied, eating, etc.)
    /// - Requests ticket spawning from TicketManager when tables need orders
    /// - Tracks eating timers for course pacing
    /// - Allows manual assignment of dishes to specific tables
    /// </summary>
    public class TableManager : CoreManager
    {
        [Header("Configuration")]
        [Tooltip("Number of tables in the restaurant")]
        [SerializeField] private int totalTableCount = 10;
        
        [Tooltip("Table size distribution: % of 2-tops, 4-tops, etc.")]
        [SerializeField] private int percent2Tops = 60; // 60% are 2-tops
        [SerializeField] private int percent4Tops = 30; // 30% are 4-tops
        // Remaining % are 6-tops (larger parties/PDR)
        
        [Header("Seating Behavior")]
        [Tooltip("How often to check for available tables (in seconds). TicketManager's spawn curve controls actual spawn probability.")]
        [SerializeField] private float tableCheckInterval = 2f;
        private float _nextTableCheckTime;
        
        [Header("Manager References")]
        [Tooltip("Reference to TicketManager")]
        [SerializeField] private Expo.Core.Managers.TicketManager ticketManager;
        
        [Tooltip("Reference to ShiftTimerManager")]
        [SerializeField] private ShiftTimerManager shiftTimerManager;

        private readonly List<TableData> _allTables = new();

        /// <summary>
        /// SINGLE SOURCE OF TRUTH: Maps table number to its order state.
        /// Replaces the old dual-dictionary system (_tableDishAssignments + _tableExpectedDishes).
        /// Each TableOrderState tracks what the table needs by instance ID.
        /// </summary>
        private readonly Dictionary<int, TableOrderState> _tableOrders = new();

        protected override void OnInitialize()
        {
            _tableOrders.Clear();
            _allTables.Clear();
            
            // Generate tables at runtime
            GenerateTables();
            
            _nextTableCheckTime = GameTime.Time;
            
            // Removed: DishAssignedToTableEvent subscription (would cause recursion)
            // AssignDishToTable is called directly and publishes the event itself
            EventBus.Subscribe<CourseCompletedEvent>(OnCourseCompleted);
            EventBus.Subscribe<DishesServedEvent>(OnDishesServed);
        }
        
        protected override void Update()
        {
            // Check if any tables are available and try to seat parties
            if (GameTime.Time >= _nextTableCheckTime)
            {
                CheckForNewSeating();
                _nextTableCheckTime = GameTime.Time + tableCheckInterval;
            }
            
            // Update eating timers for all tables
            UpdateEatingTimers();
        }
        
        private void CheckForNewSeating()
        {
            // Check if shift timer allows new tickets (before 9PM)
            if (shiftTimerManager != null && !shiftTimerManager.CanSpawnTickets())
            {
                // No new seating after 9PM cutoff
                return;
            }
            
            // Find an available table
            var availableTable = _allTables.FirstOrDefault(t => !t.IsOccupied);
            if (availableTable == null)
            {
                // No available tables
                return;
            }
            
            // Try to spawn a ticket based on spawn probability curve
            // TicketManager now controls spawn probability via AnimationCurve
            if (ticketManager != null)
            {
                bool spawned = ticketManager.TrySpawnTicketForTable(availableTable);
                if (!spawned)
                {
                    DebugLogger.Log(DebugLogger.Category.TABLE_MANAGER, 
                        $"Table {availableTable.TableNumber} available but spawn probability check failed");
                }
            }
        }
        
        private void UpdateEatingTimers()
        {
            foreach (var table in _allTables)
            {
                if (table.IsEating && table.HasFinishedEating())
                {
                    DebugLogger.Log(DebugLogger.Category.TABLE_MANAGER, 
                        $"Table {table.TableNumber} finished eating course {table.CurrentCourseBeingEaten}");
                    
                    table.FinishEating();
                    
                    // REFACTORED: Tables now own course progression!
                    // Check if they're on the last course
                    if (table.IsOnLastCourse())
                    {
                        DebugLogger.Log(DebugLogger.Category.TABLE_MANAGER, 
                            $"Table {table.TableNumber} finished their LAST course - dinner complete!");
                        
                        // Check if the order is complete (all dishes served)
                        if (_tableOrders.TryGetValue(table.TableNumber, out var orderState) && 
                            orderState.IsOrderComplete())
                        {
                            DebugLogger.Log(DebugLogger.Category.TABLE_MANAGER, 
                                $"Table {table.TableNumber} order is complete - notifying TicketManager");
                            
                            // Publish event to trigger ticket cleanup
                            // We publish a DishesServedEvent with empty dishes to trigger the completion check
                            EventBus.Publish(new DishesServedEvent
                            {
                                TableNumber = table.TableNumber,
                                DishInstanceIds = new List<int>(),
                                ServedDishTypes = new List<DishData>(),
                                Timestamp = GameTime.Time
                            });
                        }
                    }
                    else
                    {
                        // Unlock next course
                        bool unlocked = table.UnlockNextCourse();
                        if (unlocked && table.CurrentTicketId.HasValue)
                        {
                            DebugLogger.Log(DebugLogger.Category.TABLE_MANAGER, 
                                $"Table {table.TableNumber}: Course {table.CurrentCourseNumber} is now unlocked!");
                            
                            // Publish event for UI and other systems
                            EventBus.Publish(new CourseUnlockedEvent
                            {
                                TicketId = table.CurrentTicketId.Value,
                                TableNumber = table.TableNumber,
                                CourseNumber = table.CurrentCourseNumber,
                                Timestamp = GameTime.Time
                            });
                        }
                    }
                }
            }
        }
        
        private void OnCourseCompleted(CourseCompletedEvent e)
        {
            DebugLogger.Log(DebugLogger.Category.TABLE_MANAGER, $"CourseCompletedEvent received: Ticket #{e.TicketId}, Course {e.CourseNumber}");
            
            // Find the table with this ticket
            var table = _allTables.FirstOrDefault(t => t.CurrentTicketId == e.TicketId);
            if (table == null)
            {
                DebugLogger.LogWarning(DebugLogger.Category.TABLE_MANAGER, $"Could not find table for ticket #{e.TicketId}");
                return;
            }
            
            DebugLogger.Log(DebugLogger.Category.TABLE_MANAGER, $"Found table {table.TableNumber} for ticket #{e.TicketId}");
            
            // Start eating timer (get eating duration from ticket data)
            if (ticketManager != null)
            {
                var ticket = ticketManager.GetTicket(e.TicketId);
                if (ticket != null)
                {
                    DebugLogger.Log(DebugLogger.Category.TABLE_MANAGER, $"Starting eating timer: Course {e.CourseNumber}, Duration {ticket.EatingTimePerCourse}s");
                    table.StartEating(e.CourseNumber, ticket.EatingTimePerCourse);
                }
                else
                {
                    DebugLogger.LogWarning(DebugLogger.Category.TABLE_MANAGER, $"Could not find ticket #{e.TicketId} in TicketManager!");
                }
            }
            else
            {
                DebugLogger.LogWarning(DebugLogger.Category.TABLE_MANAGER, "TicketManager reference is null!");
            }
        }

        private void GenerateTables()
        {
            // Use progression system table count if available
            int actualTableCount = totalTableCount;
            // if (ProgressionManager.Instance != null)
            // {
            //     actualTableCount = ProgressionManager.Instance.GetMaxTableCount();
            //     DebugLogger.Log(DebugLogger.Category.TABLE_MANAGER, $"Using progression table count: {actualTableCount}");
            // }
            // else
            // {
            //     DebugLogger.LogWarning(DebugLogger.Category.TABLE_MANAGER, "ProgressionManager not found, using default table count");
            // }
            
            int twoTopCount = Mathf.RoundToInt(actualTableCount * (percent2Tops / 100f));
            int fourTopCount = Mathf.RoundToInt(actualTableCount * (percent4Tops / 100f));
            int sixTopCount = actualTableCount - twoTopCount - fourTopCount;

            DebugLogger.Log(DebugLogger.Category.TABLE_MANAGER, $"Generating {actualTableCount} tables: {twoTopCount} 2-tops, {fourTopCount} 4-tops, {sixTopCount} 6-tops");

            int tableNumber = 1;
            
            // Create 2-tops
            for (int i = 0; i < twoTopCount; i++)
            {
                var table = new TableData(tableNumber++, 2);
                _allTables.Add(table);
            }

            // Create 4-tops
            for (int i = 0; i < fourTopCount; i++)
            {
                var table = new TableData(tableNumber++, 4);
                _allTables.Add(table);
            }

            // Create 6-tops
            for (int i = 0; i < sixTopCount; i++)
            {
                var table = new TableData(tableNumber++, 6);
                _allTables.Add(table);
            }

            // TableOrderStates are created dynamically when tables are seated (in SeatPartyAtTable)
        }

        protected override void OnShutdown()
        {
            _tableOrders.Clear();
            EventBus.Unsubscribe<CourseCompletedEvent>(OnCourseCompleted);
            EventBus.Unsubscribe<DishesServedEvent>(OnDishesServed);
        }
        
        private void OnDishesServed(DishesServedEvent e)
        {
            DebugLogger.Log(DebugLogger.Category.TABLE_DEBUG,
                $"OnDishesServed: Table {e.TableNumber} received {e.DishInstanceIds.Count} dishes");

            // Get the table and its order state
            var table = _allTables.FirstOrDefault(t => t.TableNumber == e.TableNumber);
            if (table == null)
            {
                DebugLogger.LogWarning(DebugLogger.Category.TABLE,
                    $"Table {e.TableNumber} not found!");
                return;
            }

            if (!_tableOrders.TryGetValue(e.TableNumber, out var orderState))
            {
                DebugLogger.LogWarning(DebugLogger.Category.TABLE,
                    $"No order state found for table {e.TableNumber}!");
                return;
            }

            // Mark dishes as served by TYPE (any dish can fulfill any expectation of that type)
            for (int i = 0; i < e.DishInstanceIds.Count; i++)
            {
                var dishInstanceId = e.DishInstanceIds[i];
                var dishData = e.ServedDishTypes[i];
                
                // Determine which course this dish belongs to by finding it in the ticket
                int dishCourseNumber = 0;
                if (ticketManager != null && table.CurrentTicketId.HasValue)
                {
                    var ticket = ticketManager.GetTicket(table.CurrentTicketId.Value);
                    if (ticket != null)
                    {
                        foreach (var course in ticket.Courses)
                        {
                            var dish = course.Dishes.FirstOrDefault(d => d.DishInstanceId == dishInstanceId);
                            if (dish != null)
                            {
                                dishCourseNumber = course.CourseNumber;
                                // Mark the DishState as served for scoring
                                dish.Serve();
                                break;
                            }
                        }
                    }
                }
                
                // Mark expectation as served (this handles mistake tracking automatically)
                bool marked = orderState.MarkDishServed(
                    dishData.dishName, 
                    e.Timestamp, 
                    dishCourseNumber, 
                    table.CurrentCourseNumber
                );
                
                if (marked)
                {
                    DebugLogger.Log(DebugLogger.Category.TABLE,
                        $"Table {e.TableNumber}: '{dishData.dishName}' fulfilled an expectation");
                }
                else
                {
                    DebugLogger.LogWarning(DebugLogger.Category.TABLE,
                        $"Table {e.TableNumber}: Received '{dishData.dishName}' but has no matching expectation!");
                }
            }

            DebugLogger.Log(DebugLogger.Category.TABLE_DEBUG, orderState.GetDebugString());

            // NEW: Check if the current course is complete, then start eating
            if (orderState.IsCourseComplete(table.CurrentCourseNumber))
            {
                DebugLogger.Log(DebugLogger.Category.TABLE,
                    $"Table {e.TableNumber}: Course {table.CurrentCourseNumber} COMPLETE!");
                
                // Get ticket for eating duration
                if (ticketManager != null && table.CurrentTicketId.HasValue)
                {
                    var ticket = ticketManager.GetTicket(table.CurrentTicketId.Value);
                    if (ticket != null)
                    {
                        // Start eating immediately
                        table.StartEating(table.CurrentCourseNumber, ticket.EatingTimePerCourse);
                        
                        // Publish course completed event for scoring
                        EventBus.Publish(new CourseCompletedEvent
                        {
                            TicketId = ticket.TicketId,
                            CourseNumber = table.CurrentCourseNumber,
                            DishCount = orderState.GetCourseExpectations(table.CurrentCourseNumber).Count,
                            CompletionTime = e.Timestamp,
                            AllDishesTogether = true, // ScoringManager will calculate this
                            TimingScore = 0f // ScoringManager will calculate this
                        });
                    }
                }
            }

            // Check if the entire order is complete (all courses served)
            if (orderState.IsOrderComplete())
            {
                DebugLogger.Log(DebugLogger.Category.TABLE,
                    $"Table {e.TableNumber} order COMPLETE! (TicketManager will handle cleanup)");
                // NOTE: Don't clear the table here - let TicketManager detect completion
                // and clean up the table after publishing TicketCompletedEvent
            }
        }

        /// <summary>
        /// Seats a party at a specific table with a new ticket and initializes their order.
        /// Called by TicketManager after ticket is created.
        /// REFACTORED: Now creates a TableOrderState instead of using dual dictionaries.
        /// </summary>
        /// <param name="tableNumber">Table to seat at</param>
        /// <param name="ticketId">Ticket ID</param>
        /// <param name="ticketData">The ticket data containing courses and dishes</param>
        public void SeatPartyAtTable(int tableNumber, int ticketId, TicketData ticketData)
        {
            var table = _allTables.FirstOrDefault(t => t.TableNumber == tableNumber);
            if (table == null)
            {
                DebugLogger.LogError(DebugLogger.Category.TABLE_MANAGER, $"Table {tableNumber} not found!");
                return;
            }

            // REFACTORED: Pass total courses to table (tables own progression now!)
            table.SeatParty(ticketId, ticketData.Courses.Count);

            // Create the table's order state from the ticket's courses
            var orderState = new TableOrderState(tableNumber, ticketId);

            // Add all dishes from all courses to the order state
            foreach (var course in ticketData.Courses)
            {
                foreach (var dish in course.Dishes)
                {
                    orderState.AddDishExpectation(
                        course.CourseNumber,
                        dish.Data,
                        dish.DishInstanceId
                    );
                }
            }

            _tableOrders[tableNumber] = orderState;

            DebugLogger.Log(DebugLogger.Category.TABLE,
                $"Table {tableNumber} seated with ticket #{ticketId}: {orderState.GetDebugString()} - Starting on course 1/{ticketData.Courses.Count}");
        }

        /// <summary>
        /// Clears a table when the ticket is complete.
        /// Legacy method - now calls ClearTableByNumber after finding the table.
        /// </summary>
        public void ClearTable(int ticketId)
        {
            var table = _allTables.FirstOrDefault(t => t.CurrentTicketId == ticketId);
            if (table != null)
            {
                ClearTableByNumber(table.TableNumber);
            }
        }
        
        /// <summary>
        /// Clears a table by table number when all its dishes are served.
        /// REFACTORED: Now removes the TableOrderState.
        /// </summary>
        public void ClearTableByNumber(int tableNumber)
        {
            var table = _allTables.FirstOrDefault(t => t.TableNumber == tableNumber);
            if (table != null)
            {
                table.ClearTable();

                // Remove the order state for this table
                if (_tableOrders.Remove(tableNumber))
                {
                    DebugLogger.Log(DebugLogger.Category.TABLE,
                        $"Table {tableNumber} cleared and order state removed");
                }
            }
        }

        /// <summary>
        /// Gets the table for a specific ticket.
        /// </summary>
        public TableData GetTableForTicket(int ticketId)
        {
            return _allTables.FirstOrDefault(t => t.CurrentTicketId == ticketId);
        }

        /// <summary>
        /// Manually assigns a dish to a specific table.
        /// CRITICAL for cross-table dish reassignment (firing from any ticket to any table).
        ///
        /// REFACTORED: Uses TableOrderState to move expectations between tables.
        /// This is now MUCH simpler and CANNOT cause desyncs.
        ///
        /// EXAMPLE: If Table 3's ticket has "Steak" but you send it to Table 2:
        /// - Remove "Steak" expectation from Table 3's order
        /// - Add "Steak" expectation to Table 2's order
        /// - Table 3 no longer waits for that steak
        /// - Table 2 now expects it
        /// </summary>
        /// <param name="dishInstanceId">The specific dish instance being reassigned</param>
        /// <param name="tableNumber">Target table to receive this dish</param>
        /// <param name="dishData">Dish type (for logging/events)</param>
        public void AssignDishToTable(int dishInstanceId, int tableNumber, DishData dishData = null)
        {
            // Validate target table exists
            if (!_tableOrders.ContainsKey(tableNumber))
            {
                DebugLogger.LogWarning(DebugLogger.Category.TABLE,
                    $"Cannot assign dish {dishInstanceId} - table {tableNumber} has no order!");
                return;
            }

            DebugLogger.Log(DebugLogger.Category.TABLE_DEBUG,
                $"AssignDishToTable: Dish {dishInstanceId} ({dishData?.dishName ?? "Unknown"}) → Table {tableNumber}");

            // NEW APPROACH: Check if the target table needs this TYPE of dish
            // If yes, we don't add it as a new expectation - we just let it get marked as served later
            var targetOrderState = _tableOrders[tableNumber];
            var targetExpectations = targetOrderState.GetAllExpectations();
            
            // Find an unserved expectation of this dish type
            var matchingExpectation = targetExpectations.FirstOrDefault(exp => 
                exp.DishType.dishName == dishData?.dishName && !exp.IsServed);
            
            if (matchingExpectation != null)
            {
                DebugLogger.Log(DebugLogger.Category.TABLE,
                    $"Table {tableNumber} needs '{dishData.dishName}' - dish {dishInstanceId} can fulfill expectation {matchingExpectation.DishInstanceId}");
                
                // The table already expects this type of dish - don't add a new expectation
                // When OnDishesServed fires, it will mark the matching expectation as served
            }
            else
            {
                DebugLogger.LogWarning(DebugLogger.Category.TABLE,
                    $"Table {tableNumber} does NOT need '{dishData?.dishName ?? "Unknown"}' - this dish doesn't match any expectation!");
                
                // Optionally: still allow sending dishes to tables that don't need them
                // For now, we'll just log and not add it
                return;
            }

            // Debug: Show table's current expectations
            DebugLogger.Log(DebugLogger.Category.TABLE,
                $"Table {tableNumber} expectations: [{string.Join(", ", targetExpectations.Select(e => $"{e.DishInstanceId}:{e.DishType.dishName}(served:{e.IsServed})"))}]");

            // Publish event for UI updates (though the table doesn't add this as a new expectation)
            EventBus.Publish(new DishAssignedToTableEvent
            {
                DishInstanceId = dishInstanceId,
                TableNumber = tableNumber,
                DishData = dishData,
                Timestamp = GameTime.Time
            });
        }

        /// <summary>
        /// Gets all dish instance IDs for a specific table's order.
        /// REFACTORED: Now queries TableOrderState.
        /// </summary>
        public List<int> GetDishesForTable(int tableNumber)
        {
            if (_tableOrders.TryGetValue(tableNumber, out var orderState))
            {
                return orderState.GetAllExpectations()
                    .Select(exp => exp.DishInstanceId)
                    .ToList();
            }

            return new List<int>();
        }

        /// <summary>
        /// Gets the TableOrderState for a specific table.
        /// Used by other systems (TicketUI, TicketManager) to query table expectations.
        /// </summary>
        public TableOrderState GetTableOrderState(int tableNumber)
        {
            _tableOrders.TryGetValue(tableNumber, out var orderState);
            return orderState;
        }

        /// <summary>
        /// DEPRECATED: Dish clearing is now handled automatically in OnDishesServed.
        /// This method is kept for backward compatibility but does nothing.
        /// </summary>
        [Obsolete("No longer needed - dishes are cleared automatically when served")]
        public void ClearServedDishes(List<int> dishInstanceIds)
        {
            // No-op: Dishes are now marked as served in TableOrderState via OnDishesServed
            DebugLogger.LogWarning(DebugLogger.Category.TABLE,
                "ClearServedDishes called but is deprecated - dishes clear automatically");
        }

        /// <summary>
        /// Gets all currently occupied tables (tables with active tickets).
        /// </summary>
        public List<TableData> GetOccupiedTables()
        {
            return _allTables.FindAll(t => t.IsOccupied);
        }

        /// <summary>
        /// Gets all tables (for debugging/admin purposes).
        /// </summary>
        public List<TableData> GetAllTables()
        {
            return new List<TableData>(_allTables);
        }
    }
}
