using System;
using System.Collections.Generic;
using Expo.Runtime;

namespace Expo.Data
{
    [Serializable]
    public class TicketData
    {
        public readonly int TicketId;
        public readonly float SpawnTime;
        public readonly List<DishState> Dishes = new();
        public readonly List<CourseData> Courses = new();
        public TableData AssignedTable { get; set; }
        
        // Time guests take to eat each course before next can be fired (in seconds)
        public float EatingTimePerCourse { get; set; } = 15f;
        
        // Timing data for tracking optimal completion time and pulse state
        public TicketTimingData TimingData { get; set; }

        public TicketData(int ticketId, float spawnTime)
        {
            TicketId = ticketId;
            SpawnTime = spawnTime;
        }

        /// <summary>
        /// REFACTORED: Ticket completion is now determined by the table's order state.
        /// This method is kept for backward compatibility but should not be used directly.
        /// Use TableOrderState.IsOrderComplete() instead.
        ///
        /// NOTE: The ticket doesn't "know" when it's complete anymore - the TABLE does.
        /// A ticket is complete when the table it's assigned to has received all dishes.
        /// </summary>
        [Obsolete("Use TableOrderState.IsOrderComplete() instead. Tickets don't own completion state anymore.")]
        public bool AllExpectationsServed()
        {
            // This method is deprecated but kept for compilation compatibility.
            // It will be removed once all callers are updated to use TableOrderState.
            return false;
        }

        public bool AnyDishDead()
        {
            foreach (var d in Dishes)
                if (d.Status == DishStatus.Dead) return true;
            return false;
        }

        /// <summary>
        /// Gets all dishes for the ticket by iterating through courses.
        /// This is the canonical way to access dishes when courses are used.
        /// </summary>
        public List<DishState> GetAllDishes()
        {
            var allDishes = new List<DishState>();
            foreach (var course in Courses)
            {
                allDishes.AddRange(course.Dishes);
            }
            return allDishes;
        }

        /// <summary>
        /// Gets the total number of dishes across all courses.
        /// </summary>
        public int TotalDishCount()
        {
            int count = 0;
            foreach (var course in Courses)
            {
                count += course.Dishes.Count;
            }
            return count;
        }
        
        /// <summary>
        /// Gets the next course that should be unlocked, or null if none.
        /// </summary>
        public CourseData GetNextCourseToUnlock()
        {
            for (int i = 0; i < Courses.Count; i++)
            {
                if (!Courses[i].IsUnlocked)
                {
                    return Courses[i];
                }
            }
            return null;
        }
    }
}
