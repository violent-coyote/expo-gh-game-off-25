using UnityEngine;
using Expo.Core.Events;
using Expo.Core.Debug;
using Expo.Data;
using Expo.Runtime;
using Expo.UI;
using Lean.Common;
using Lean.Touch;

namespace Expo.Core.GameObjects
{
    public class PassedDish : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer dishSprite;
        [SerializeField] private DishProgressBar progressBar; // Progress bar for die timer

        public int DishInstanceId { get;  private set; }
        public int? AssignedTableNumber { get; private set; } // Track manual table assignment

        private DishState _dishState;
        private bool _walkingMarked;
        private LeanSelectableByFinger _leanSelectable;
        private Color _originalColor;
        private LeanDragTranslate _dragTranslate;

        public void Init(DishState dish)
        {
            _dishState = dish;
            DishInstanceId = dish.DishInstanceId;
            dishSprite.sprite = dish.Data.icon;
            
            // Get LeanSelectableByFinger from child object (1 level down)
            _leanSelectable = GetComponentInChildren<LeanSelectableByFinger>();
            if (_leanSelectable != null)
            {
                // Hook into Lean's selection events
                // These fire when the object is selected/deselected via the scene's LeanSelectByFinger
                _leanSelectable.OnSelected.AddListener(OnSelected);
                _leanSelectable.OnDeselected.AddListener(OnDeselected);
                
                // Configure drag to use ManuallyAddedFingers for per-instance isolation
                _dragTranslate = _leanSelectable.GetComponent<LeanDragTranslate>();
                if (_dragTranslate != null)
                {
                    // Create new filter instance for this dish
                    _dragTranslate.Use = new LeanFingerFilter(LeanFingerFilter.FilterType.ManuallyAddedFingers, true, 0, 0, null);
                    
                    // Hook into global finger down events to detect touches on THIS dish
                    LeanTouch.OnFingerDown += OnFingerTouchDish;
                }
                
                DebugLogger.Log(DebugLogger.Category.PASS, $"PassedDish {dish.Data.dishName}: Hooked into selection events");
            }
            else
            {
                UnityEngine.Debug.LogError($"PassedDish {dish.Data.dishName}: LeanSelectableByFinger not found in children!");
            }
            
            // Initialize progress bar for die timer
            if (progressBar != null)
            {
                progressBar.Initialize(dish.Data.dieTime);
            }
        }
        
        private void Update()
        {
            // Update progress bar with die timer
            if (_dishState != null && progressBar != null && _dishState.Status == DishStatus.OnPass)
            {
                progressBar.UpdateProgress(_dishState.ElapsedTime);
            }
        }

        private void OnSelected(LeanSelect select)
        {
            // Called when object becomes selected
            _walkingMarked = true;
            
            // LeanSelectableSpriteRendererColor handles the visual feedback automatically

            EventBus.Publish(new DishWalkingEvent
            {
                DishData = _dishState.Data,
                DishInstanceId = _dishState.DishInstanceId,
                Station = _dishState.Data.station,
                Timestamp = GameTime.Time
            });

            DebugLogger.Log(DebugLogger.Category.PASS, $"Dish SELECTED/WALKING: {_dishState.Data.dishName}");
        }
        
        private void OnDeselected(LeanSelect select)
        {
            // Called when object becomes deselected  
            _walkingMarked = false;
            
            // LeanSelectableSpriteRendererColor handles the visual feedback automatically
            
            DebugLogger.Log(DebugLogger.Category.PASS, $"Dish DESELECTED/STOPPED: {_dishState.Data.dishName}");
        }

        private void OnFingerTouchDish(LeanFinger finger)
        {
            // When a finger touches down, check if it's on THIS dish's collider
            if (_dragTranslate != null && _leanSelectable != null)
            {
                // Check if the finger is over this dish's collider
                var collider = _leanSelectable.GetComponent<Collider2D>();
                if (collider != null)
                {
                    Vector2 worldPoint = Camera.main.ScreenToWorldPoint(finger.ScreenPosition);
                    if (collider.OverlapPoint(worldPoint))
                    {
                        _dragTranslate.AddFinger(finger);
                        DebugLogger.Log(DebugLogger.Category.PASS, $"Finger added to drag for: {_dishState.Data.dishName}");
                    }
                }
            }
        }

        private void OnDestroy()
        {
            // Clean up event listeners
            if (_leanSelectable != null)
            {
                _leanSelectable.OnSelected.RemoveListener(OnSelected);
                _leanSelectable.OnDeselected.RemoveListener(OnDeselected);
            }
            
            // Clean up global finger events
            LeanTouch.OnFingerDown -= OnFingerTouchDish;
        }
    }
}