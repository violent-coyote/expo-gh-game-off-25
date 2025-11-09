using System.Collections.Generic;
using UnityEngine;
using Expo.Core;
using Expo.Core.Debug;
using Expo.Core.Events;
using Expo.UI;
using Expo.Data;
using Expo.Runtime;

namespace Expo.Core.Managers
{
    /// <summary>
    /// Manages a single cooking station (e.g., Grill, Pasta, Garde Manger).
    /// RESPONSIBILITIES:
    /// - Listens for dishes assigned to this station (via DishFiredEvent)
    /// - Ticks dish cook timers until ready
    /// - Spawns progress bars for each cooking dish in a vertical layout group
    /// - Publishes DishReadyEvent when cooking completes (handled in DishState)
    /// NOTE: Each station instance manages one station type.
    /// </summary>
    public class StationManager : CoreManager
    {
        [SerializeField] private string stationName;
        [SerializeField] private Transform progressBarContainer; // Vertical Layout Group to hold progress bars
        [SerializeField] private GameObject progressBarPrefab; // DishProgressBar prefab to instantiate
        
        private readonly List<DishState> _activeDishes = new();
        private readonly Dictionary<int, DishProgressBar> _dishProgressBars = new(); // Maps dish ID to its progress bar

        protected override void OnInitialize()
        {
            EventBus.Subscribe<DishFiredEvent>(OnDishFired);
        }

        protected override void OnShutdown()
        {
            EventBus.Unsubscribe<DishFiredEvent>(OnDishFired);
            _activeDishes.Clear();
            
            // Clean up all progress bars
            foreach (var progressBar in _dishProgressBars.Values)
            {
                if (progressBar != null)
                    Destroy(progressBar.gameObject);
            }
            _dishProgressBars.Clear();
        }

        private void OnDishFired(DishFiredEvent e)
        {
            if (e.Station != stationName)
            {
                // DebugLogger.Log(DebugLogger.Category.STATION, $"{stationName} ignoring dish for station {e.Station}");
                return; // not our station
            }

            DebugLogger.Log(DebugLogger.Category.STATION, $"{stationName} received {e.DishData.dishName} ({e.DishInstanceId})");

            var data = e.DishData;
            if (data == null)
            {
                DebugLogger.LogWarning(DebugLogger.Category.STATION, $"Missing DishData for {e.DishData.dishName}");
                return;
            }

            // Use the DishState reference from the event instead of creating a new one
            var dish = e.DishState;
            dish.Fire();
            _activeDishes.Add(dish);
            
            // Spawn a progress bar for this dish
            SpawnProgressBarForDish(dish);
        }
        
        /// <summary>
        /// Spawns a progress bar for a specific dish and adds it to the vertical layout group.
        /// </summary>
        private void SpawnProgressBarForDish(DishState dish)
        {
            if (progressBarPrefab == null || progressBarContainer == null)
            {
                DebugLogger.LogWarning(DebugLogger.Category.STATION, 
                    $"{stationName}: Missing progress bar prefab or container!");
                return;
            }
            
            // Instantiate progress bar in the container (vertical layout group)
            var progressBarObj = Instantiate(progressBarPrefab, progressBarContainer);
            var progressBar = progressBarObj.GetComponent<DishProgressBar>();
            
            if (progressBar != null)
            {
                progressBar.Initialize(dish.Data.pickupTime);
                _dishProgressBars[dish.DishInstanceId] = progressBar;
                
                DebugLogger.Log(DebugLogger.Category.STATION, 
                    $"{stationName} spawned progress bar for {dish.Data.dishName}");
            }
            else
            {
                DebugLogger.LogWarning(DebugLogger.Category.STATION, 
                    $"Progress bar prefab missing DishProgressBar component!");
                Destroy(progressBarObj);
            }
        }

        protected override void Update()
        {
            float dt = GameTime.DeltaTime;
            
            // Tick all cooking dishes and update their progress bars
            for (int i = _activeDishes.Count - 1; i >= 0; i--)
            {
                var dish = _activeDishes[i];
                
                if (dish.Status == DishStatus.Cooking)
                {
                    dish.Tick(dt);
                    
                    // Update the progress bar for this dish
                    if (_dishProgressBars.TryGetValue(dish.DishInstanceId, out var progressBar))
                    {
                        progressBar.UpdateProgress(dish.ElapsedTime);
                    }
                }
                
                // Remove dish when it moves to the pass
                if (dish.Status == DishStatus.OnPass)
                {
                    _activeDishes.RemoveAt(i);
                    
                    // Destroy its progress bar
                    if (_dishProgressBars.TryGetValue(dish.DishInstanceId, out var progressBar))
                    {
                        if (progressBar != null)
                            Destroy(progressBar.gameObject);
                        _dishProgressBars.Remove(dish.DishInstanceId);
                    }
                }
            } 
        }
    }
}
