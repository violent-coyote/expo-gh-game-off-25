using UnityEngine;

namespace Expo.Data
{
    [CreateAssetMenu(menuName = "Expo/Dish Data", fileName = "NewDishData")]
    public class DishData : ScriptableObject
    {
        [Header("Identity")]
        public string dishName;
        public string station;           // e.g., "Grill", "Pasta", "GardeManger"

        [Header("Timing (seconds)")]
        public float pickupTime = 10f;   // ideal total time to prepare
        public float dieTime = 5f;       // how long dish lasts on pass

        [Header("Visuals / Audio (optional)")]
        public Sprite icon;
    }
}
