using UnityEngine;
using UnityEngine.UI;
using Expo.Core.Events;
using Expo.Core.Debug;
using Expo.Data;
using Expo.Runtime;
using Expo.UI;
using TMPro;

namespace Expo.Core.GameObjects
{
    public class PassedDish : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer dishSprite;
        [SerializeField] private Button walkingButton;
        [SerializeField] private Button assignTableButton; // New: Button to manually assign to table
        [SerializeField] private DishProgressBar progressBar; // Progress bar for die timer

        public int DishInstanceId { get;  private set; }
        public int? AssignedTableNumber { get; private set; } // Track manual table assignment

        private DishState _dishState;
        private bool _walkingMarked;

        public void Init(DishState dish)
        {
            _dishState = dish;
            DishInstanceId = dish.DishInstanceId;
            dishSprite.sprite = dish.Data.icon;
            walkingButton.onClick.AddListener(OnWalkingPressed);
            
            if (assignTableButton != null)
            {
                assignTableButton.onClick.AddListener(OnAssignTablePressed);
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

        private void OnWalkingPressed()
        {
            if (_walkingMarked) return;
            _walkingMarked = true;
            walkingButton.interactable = false;
            // change the button to blue, and the text to "walking"
            var colors = walkingButton.colors;
            colors.normalColor = Color.cyan;
            walkingButton.colors = colors;
            var text = walkingButton.GetComponentInChildren<TextMeshProUGUI>();
            text.text = "WALKING";

            EventBus.Publish(new DishWalkingEvent
            {
                DishData = _dishState.Data,
                DishInstanceId = _dishState.DishInstanceId,
                Station = _dishState.Data.station,
                Timestamp = GameTime.Time
            });

            DebugLogger.Log(DebugLogger.Category.PASS, $"Dish WALKING: {_dishState.Data.dishName}");
        }
        
        /// <summary>
        /// Opens a UI to manually assign this dish to a specific table.
        /// For now, this is a placeholder - you would implement a table selection UI.
        /// </summary>
        private void OnAssignTablePressed()
        {
            // TODO: Implement table selection UI
            // For now, this would open a panel showing available tables
            // and allow the player to click one to assign the dish
            DebugLogger.Log(DebugLogger.Category.PASS, $"Assign table button pressed for dish {DishInstanceId}");
            
            // Placeholder: Assign to a random table for demonstration
            // In a real implementation, this would open a UI with table buttons
            // AssignToTable(randomTableNumber);
        }
        
        /// <summary>
        /// Assigns this dish to a specific table.
        /// </summary>
        public void AssignToTable(int tableNumber)
        {
            AssignedTableNumber = tableNumber;
            
            EventBus.Publish(new DishAssignedToTableEvent
            {
                DishInstanceId = DishInstanceId,
                TableNumber = tableNumber,
                Timestamp = GameTime.Time
            });
            
            DebugLogger.Log(DebugLogger.Category.PASS, $"Dish {DishInstanceId} assigned to table {tableNumber}");
            
            // Visual feedback
            if (assignTableButton != null)
            {
                var text = assignTableButton.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = $"T{tableNumber}";
                }
                assignTableButton.interactable = false;
            }
        }
    }
}