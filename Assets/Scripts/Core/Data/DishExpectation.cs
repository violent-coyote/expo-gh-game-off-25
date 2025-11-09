using System;
using Expo.Data;

namespace Expo.Runtime
{
    /// <summary>
    /// Represents a table's expectation for a SPECIFIC dish instance.
    /// CRITICAL CHANGE: Now tracks by INSTANCE ID, not just dish type.
    /// This prevents ambiguity when:
    /// - A table has duplicate dishes (2 steaks)
    /// - Dishes are reassigned between tables
    ///
    /// Each expectation maps to exactly ONE DishState instance.
    /// </summary>
    [Serializable]
    public class DishExpectation
    {
        /// <summary>The type of dish expected (for display/matching)</summary>
        public readonly DishData DishType;

        /// <summary>The SPECIFIC instance ID of this dish (guarantees uniqueness)</summary>
        public readonly int DishInstanceId;

        /// <summary>Which course this dish belongs to</summary>
        public readonly int CourseNumber;

        /// <summary>Whether this specific dish instance has been served</summary>
        public bool IsServed { get; private set; }

        /// <summary>When this dish was served (if served)</summary>
        public float? ServedTime { get; private set; }

        public DishExpectation(DishData dishType, int dishInstanceId, int courseNumber)
        {
            DishType = dishType;
            DishInstanceId = dishInstanceId;
            CourseNumber = courseNumber;
            IsServed = false;
        }
        
        /// <summary>
        /// Marks this dish expectation as fulfilled.
        /// </summary>
        public void MarkServed(float time)
        {
            IsServed = true;
            ServedTime = time;
        }
        
        /// <summary>
        /// Resets the served state (useful for testing).
        /// </summary>
        public void Reset()
        {
            IsServed = false;
            ServedTime = null;
        }
    }
}
