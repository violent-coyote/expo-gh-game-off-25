using UnityEngine;

namespace Expo.Data
{
    [CreateAssetMenu(menuName = "Expo/Dish Data", fileName = "NewDishData")]
    public class DishData : ScriptableObject
    {
        [Header("Identity")]
        public string dishName;
        [Tooltip("Short name for ticket display (e.g., 'Shrimp' for 'Shrimp Pasta'). Leave empty to use first word of dishName.")]
        public string slug;
        public string station;           // e.g., "Grill", "Pasta", "GardeManger"

        [Header("Timing (seconds)")]
        public float pickupTime = 10f;   // ideal total time to prepare
        public float dieTime = 5f;       // how long dish lasts on pass

        [Header("Visuals / Audio (optional)")]
        public Sprite icon;
        
        /// <summary>
        /// Gets the slug for ticket display, automatically in ALL CAPS.
        /// If slug is not set, uses the first word of dishName.
        /// </summary>
        public string GetSlug()
        {
            if (!string.IsNullOrEmpty(slug))
            {
                return slug.ToUpper();
            }
            
            // Fallback: use first word of dishName
            if (!string.IsNullOrEmpty(dishName))
            {
                string firstWord = dishName.Split(' ')[0];
                return firstWord.ToUpper();
            }
            
            return "DISH";
        }
    }
}
