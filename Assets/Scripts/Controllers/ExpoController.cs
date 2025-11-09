using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Expo.Core;
using Expo.Core.Events;
using Expo.Core.Debug;
using Expo.Data;
using Expo.UI;
using Expo.Managers;

namespace Expo.Controllers
{
    /// <summary>
    /// Player-facing controller for the Expeditor role.
    /// RESPONSIBILITIES:
    /// - Tracks which dishes are marked "WALKING" (ready for service)
    /// - Assigns walking dishes to selected table via always-visible table menu
    /// - Publishes DishesServedEvent which triggers cleanup in other managers
    /// NOTE: Does NOT manage tickets or change dish states directly.
    /// The table selection UI is always visible - no "HANDS" button needed.
    /// </summary>
    public class ExpoController : CoreManager
    {
        [Header("UI References")]
        [SerializeField] private TableSelectionUI tableSelectionUI;
        
        [Header("Manager References")]
        [SerializeField] private TableManager tableManager;
        [SerializeField] private PassManager passManager;

        private readonly HashSet<int> _walkingDishes = new(); // dishes marked as WALKING

        // --------------------------------------------------
        // INITIALIZATION
        // --------------------------------------------------
        protected override void OnInitialize()
        {
            EventBus.Subscribe<TicketCreatedEvent>(OnTicketCreated);
            EventBus.Subscribe<DishWalkingEvent>(OnDishWalking);
            
            // Register callback with the always-visible table selection UI
            if (tableSelectionUI != null)
            {
                tableSelectionUI.SetTableSelectedCallback(OnTableSelected);
            }
        }

        protected override void OnShutdown()
        {
            EventBus.Unsubscribe<TicketCreatedEvent>(OnTicketCreated);
            EventBus.Unsubscribe<DishWalkingEvent>(OnDishWalking);
            _walkingDishes.Clear();
        }

        // --------------------------------------------------
        // EVENT HANDLERS
        // --------------------------------------------------
        private void OnTicketCreated(TicketCreatedEvent e)
        {
            DebugLogger.Log(DebugLogger.Category.EXPO, $"New ticket #{e.TicketId} created.");
        }

        private void OnDishWalking(DishWalkingEvent e)
        {
            if (!_walkingDishes.Contains(e.DishInstanceId))
            {
                _walkingDishes.Add(e.DishInstanceId);
                DebugLogger.Log(DebugLogger.Category.EXPO, 
                    $"🚶 Dish {e.DishInstanceId} ({e.DishData.dishName}) marked WALKING. Total walking: {_walkingDishes.Count}");
            }
            else
            {
                DebugLogger.LogWarning(DebugLogger.Category.EXPO,
                    $"⚠ Dish {e.DishInstanceId} ({e.DishData.dishName}) already in walking dishes!");
            }
        }

        // --------------------------------------------------
        // TABLE SELECTION (via always-visible UI)
        // --------------------------------------------------
        
        /// <summary>
        /// Called when a table is selected from the always-visible menu.
        /// Assigns all walking dishes to the selected table and serves them.
        /// </summary>
        private void OnTableSelected(int tableNumber)
        {
            if (_walkingDishes.Count == 0)
            {
                DebugLogger.LogWarning(DebugLogger.Category.EXPO, "No walking dishes to assign!");
                return;
            }

            DebugLogger.Log(DebugLogger.Category.EXPO, 
                $"🤲 HANDS! Assigning {_walkingDishes.Count} walking dishes to table {tableNumber}");
            DebugLogger.Log(DebugLogger.Category.EXPO, 
                $"Walking dish IDs: [{string.Join(", ", _walkingDishes)}]");

            // Collect dish data for all walking dishes
            var servedDishTypes = new List<DishData>();
            
            // Assign all walking dishes to the selected table
            if (tableManager != null && passManager != null)
            {
                foreach (var dishId in _walkingDishes)
                {
                    DebugLogger.Log(DebugLogger.Category.EXPO, $"  → Processing dish {dishId}...");
                    
                    // Get dish data from PassManager
                    var dishData = passManager.GetDishDataByInstanceId(dishId);
                    
                    if (dishData != null)
                    {
                        DebugLogger.Log(DebugLogger.Category.EXPO, $"    ✓ Found: {dishData.dishName}");
                        servedDishTypes.Add(dishData);
                        tableManager.AssignDishToTable(dishId, tableNumber, dishData);
                    }
                    else
                    {
                        DebugLogger.LogError(DebugLogger.Category.EXPO, 
                            $"    ✗ Could not find dish data for instance {dishId} on pass!");
                    }
                }
                
                DebugLogger.Log(DebugLogger.Category.EXPO, 
                    $"Collected {servedDishTypes.Count} dish types: [{string.Join(", ", servedDishTypes.Select(d => d.dishName))}]");
            }
            else
            {
                DebugLogger.LogError(DebugLogger.Category.EXPO, 
                    $"CRITICAL: Manager references missing! TableManager={tableManager != null}, PassManager={passManager != null}");
            }

            // Publish the served event with both instance IDs and dish types
            DebugLogger.Log(DebugLogger.Category.EXPO,
                $"📢 Publishing DishesServedEvent: {_walkingDishes.Count} dishes → Table {tableNumber}");
            
            EventBus.Publish(new DishesServedEvent
            {
                DishInstanceIds = _walkingDishes.ToList(),
                ServedDishTypes = servedDishTypes,
                TableNumber = tableNumber,
                Timestamp = GameTime.Time
            });

            DebugLogger.Log(DebugLogger.Category.EXPO, 
                $"✅ HANDS complete! Served {_walkingDishes.Count} dishes to table {tableNumber}. Clearing walking dishes.");
            _walkingDishes.Clear();
        }
    }
}
