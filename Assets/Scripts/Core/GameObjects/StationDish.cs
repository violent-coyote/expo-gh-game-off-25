using UnityEngine;
using Expo.Core.Debug;
using Expo.Runtime;
using Expo.UI;

namespace Expo.Core.GameObjects
{
    /// <summary>
    /// Visual representation of a dish cooking at a station.
    /// Shows the dish sprite and a progress bar indicating cooking progress.
    /// </summary>
    public class StationDish : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer dishSprite;
        [SerializeField] private DishProgressBar progressBar;

        public int DishInstanceId { get; private set; }
        
        private DishState _dishState;

        /// <summary>
        /// Initializes the station dish with a dish state.
        /// </summary>
        public void Init(DishState dish)
        {
            _dishState = dish;
            DishInstanceId = dish.DishInstanceId;
            
            // Set the dish sprite
            if (dishSprite != null && dish.Data.icon != null)
            {
                dishSprite.sprite = dish.Data.icon;
            }
            
            // Initialize progress bar for cook timer
            if (progressBar != null)
            {
                progressBar.Initialize(dish.Data.pickupTime);
            }
            
            DebugLogger.Log(DebugLogger.Category.STATION, 
                $"Station dish initialized: {dish.Data.dishName} ({DishInstanceId})");
        }

        private void Update()
        {
            // Update progress bar with cooking time
            if (_dishState != null && progressBar != null && _dishState.Status == DishStatus.Cooking)
            {
                progressBar.UpdateProgress(_dishState.ElapsedTime);
            }
        }

        /// <summary>
        /// Gets the current dish state for this station dish.
        /// </summary>
        public DishState GetDishState()
        {
            return _dishState;
        }
    }
}
