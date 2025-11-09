using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Expo.Core;
using Expo.Core.Debug;
using Expo.Runtime;
using Expo.Data;
using Expo.Core.Events;
using Expo.Core.GameObjects;

namespace Expo.Managers
{
    /// <summary>
    /// Manages the pass (expo station) where completed dishes await service.
    /// RESPONSIBILITIES:
    /// - Spawns visual dishes on the pass when cooking completes
    /// - Tracks die timers and marks dishes as Dead if they expire
    /// - Owns calling dish.Serve() when DishesServedEvent fires (physical dish state)
    /// - Cleans up visual dish GameObjects when served or dead
    /// </summary>
    public class PassManager : CoreManager
    {
        [SerializeField] private Transform passSpawnRoot;
        [SerializeField] private GameObject passedDishPrefab;

        private readonly List<DishState> _onPass = new();

        private readonly Dictionary<int, PassedDish> _passedDishes = new();

        // --------------------------------------------------
        // INITIALIZATION
        // --------------------------------------------------
        protected override void OnInitialize()
        {
            EventBus.Subscribe<DishReadyEvent>(OnDishReady);
            EventBus.Subscribe<DishWalkingEvent>(OnDishWalking);
            EventBus.Subscribe<DishesServedEvent>(OnDishesServed);
        }

        protected override void OnShutdown()
        {
            EventBus.Unsubscribe<DishReadyEvent>(OnDishReady);
            EventBus.Unsubscribe<DishWalkingEvent>(OnDishWalking);
            EventBus.Unsubscribe<DishesServedEvent>(OnDishesServed);
            _onPass.Clear();
        }

        // --------------------------------------------------
        // WHEN A DISH ARRIVES ON THE PASS
        // --------------------------------------------------
        private void OnDishReady(DishReadyEvent e)
        {
            DebugLogger.Log(DebugLogger.Category.PASS, $"Dish ready: {e.DishData.dishName} from {e.Station}");

            // Use the DishState reference from the event instead of creating a new one
            var dish = e.DishState;
            dish.MoveToPass();
            _onPass.Add(dish);

            // Spawn the physical dish prefab on the pass
            var worldDish = Instantiate(passedDishPrefab, passSpawnRoot);
            var passedDish = worldDish.GetComponent<PassedDish>();
            passedDish.Init(dish);
            _passedDishes[dish.DishInstanceId] = passedDish;

            // Randomize position on pass, within bounds of the spawn root, for chaos and visual variety
            var bounds = passSpawnRoot.GetComponentInChildren<SpriteRenderer>().bounds;
            // avoid the edges, inset by 30%
            var insetX = bounds.size.x * 0.3f;
            var insetY = bounds.size.y * 0.3f;
            bounds.min += new Vector3(insetX, insetY, 0);
            bounds.max -= new Vector3(insetX, insetY, 0);
            var randomX = Random.Range(bounds.min.x, bounds.max.x);
            var randomY = Random.Range(bounds.min.y, bounds.max.y);
            worldDish.transform.position = new Vector3(randomX, randomY, worldDish.transform.position.z);
            // ensure dish is visible and not overlapping others too closely
            // TODO IMPROVE


            // foreach (var spawnedPassedDish in _passedDishes.Values)
            // {
            //     if (Vector3.Distance(worldDish.transform.position, passedDish.transform.position) < 0.5f)
            //     {
            //         randomX = Random.Range(bounds.min.x, bounds.max.x);
            //         randomY = Random.Range(bounds.min.y, bounds.max.y);
            //         worldDish.transform.position = new Vector3(randomX, randomY, worldDish.transform.position.z);
            //     }
            // }

            // Notify UI/systems
            EventBus.Publish(new DishOnPassEvent
            {
                DishData = e.DishData,
                DishState = dish,
                DishInstanceId = e.DishInstanceId,
                Station = e.Station,
                Timestamp = GameTime.Time
            });
        }

        // --------------------------------------------------
        // WHEN A DISH IS MARKED AS WALKING
        // --------------------------------------------------
        private void OnDishWalking(DishWalkingEvent e)
        {
            foreach (var dish in _onPass)
            {
                if (dish.DishInstanceId == e.DishInstanceId)
                {
                    dish.MarkWalking();
                    DebugLogger.Log(DebugLogger.Category.PASS, $"Dish {e.DishData.dishName} marked as WALKING.");
                    break;
                }
            }
        }

        // --------------------------------------------------
        // UPDATE LOOP - DISH DECAY
        // --------------------------------------------------
        protected override void Update()
        {
            float dt = GameTime.DeltaTime;

            for (int i = _onPass.Count - 1; i >= 0; i--)
            {
                var dish = _onPass[i];
                // Only increment time and check death for OnPass status (not Walking)
                if (dish.Status != DishStatus.OnPass) continue;

                dish.IncrementElapsed(dt);

                if (dish.ElapsedTime >= dish.Data.dieTime)
                {
                    dish.Kill();
                    _onPass.RemoveAt(i);

                    // Clean up the visual GameObject
                    if (_passedDishes.TryGetValue(dish.DishInstanceId, out var visual))
                    {
                        if (visual != null)
                            Destroy(visual.gameObject);
                        _passedDishes.Remove(dish.DishInstanceId);
                    }

                    DebugLogger.Log(DebugLogger.Category.PASS, $"{dish.Data.dishName} DIED on pass.");

                    EventBus.Publish(new DishDiedEvent
                    {
                        DishData = dish.Data,
                        DishState = dish,
                        DishInstanceId = dish.DishInstanceId,
                        Station = dish.Data.station,
                        Timestamp = GameTime.Time
                    });
                }
            }
        }

        // --------------------------------------------------
        // WHEN HANDS IS CALLED (DISHES SERVED)
        // --------------------------------------------------
        private void OnDishesServed(DishesServedEvent e)
        {
            // NOTE: PassManager owns calling dish.Serve() since it manages physical dishes on the pass.
            // This is the ONLY place dish.Serve() should be called.
            
            for (int i = _onPass.Count - 1; i >= 0; i--)
            {
                var dish = _onPass[i];
                if (e.DishInstanceIds.Contains(dish.DishInstanceId))
                {
                    dish.Serve();
                    
                    // Publish progression event for each served dish
                    EventBus.Publish(new DishServedEvent
                    {
                        DishId = dish.Data.dishName,
                        DishInstanceId = dish.DishInstanceId,
                        TableNumber = e.TableNumber,
                        ServeTime = GameTime.Time
                    });
                    
                    _onPass.RemoveAt(i);

                    // destroy the visual
                    if (_passedDishes.TryGetValue(dish.DishInstanceId, out var visual))
                    {
                        if (visual != null)
                            Destroy(visual.gameObject);
                        _passedDishes.Remove(dish.DishInstanceId);
                    }
                }
            }

            DebugLogger.Log(DebugLogger.Category.PASS, $"Cleared {e.DishInstanceIds.Count} served dishes.");
        }

        // --------------------------------------------------
        // PUBLIC API
        // --------------------------------------------------
        
        /// <summary>
        /// Gets the dish data for a given instance ID (for cross-table matching).
        /// </summary>
        public DishData GetDishDataByInstanceId(int dishInstanceId)
        {
            DebugLogger.Log(DebugLogger.Category.PASS, $"GetDishDataByInstanceId({dishInstanceId}): Searching in {_onPass.Count} dishes on pass");
            
            var dish = _onPass.FirstOrDefault(d => d.DishInstanceId == dishInstanceId);
            
            if (dish != null)
            {
                DebugLogger.Log(DebugLogger.Category.PASS, $"✓ Found dish {dishInstanceId}: {dish.Data.dishName}");
            }
            else
            {
                DebugLogger.LogWarning(DebugLogger.Category.PASS, $"✗ Dish {dishInstanceId} NOT FOUND on pass! Current dishes: [{string.Join(", ", _onPass.Select(d => d.DishInstanceId))}]");
            }
            
            return dish?.Data;
        }

    }
}

