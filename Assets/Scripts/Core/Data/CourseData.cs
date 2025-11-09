using System;
using System.Collections.Generic;
using Expo.Runtime;
using Expo.Core.Debug;

namespace Expo.Data
{
    /// <summary>
    /// Represents a single course within a ticket.
    /// A course groups DishState instances that can be fired from this ticket.
    ///
    /// REFACTORED: Expectations removed - now live in TableOrderState.
    /// CourseData now ONLY contains:
    /// - DishState instances that can be fired (the "menu")
    /// - Unlocking state (controls fire button availability)
    ///
    /// The table owns what it NEEDS (TableOrderState).
    /// The ticket owns what it CAN FIRE (CourseData).
    /// </summary>
    [Serializable]
    public class CourseData
    {
        /// <summary>Course number (1, 2, 3, etc.)</summary>
        public readonly int CourseNumber;

        /// <summary>DishState instances that can be fired from this ticket's course</summary>
        public readonly List<DishState> Dishes = new();

        /// <summary>Whether this course is unlocked for firing</summary>
        public bool IsUnlocked { get; private set; }

        /// <summary>When this course was marked as served (for timing)</summary>
        public float? ServedTime { get; private set; }

        public CourseData(int courseNumber)
        {
            CourseNumber = courseNumber;
            // First course is always unlocked
            IsUnlocked = (courseNumber == 1);
        }
        
        /// <summary>
        /// Unlocks this course, allowing dishes to be fired.
        /// </summary>
        public void Unlock()
        {
            IsUnlocked = true;
        }
        
        /// <summary>
        /// Marks the course as served at a specific time.
        /// Used for progression/timing tracking.
        /// </summary>
        public void MarkAsServed(float time)
        {
            ServedTime = time;
        }

        /// <summary>
        /// Checks if any dish in this course has died.
        /// </summary>
        public bool AnyDishDead()
        {
            foreach (var dish in Dishes)
            {
                if (dish.Status == DishStatus.Dead)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the count of dishes ready to be served (status: OnPass or Walking).
        /// </summary>
        public int GetReadyDishCount()
        {
            int count = 0;
            foreach (var dish in Dishes)
            {
                if (dish.Status == DishStatus.OnPass || dish.Status == DishStatus.Walking)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Checks if all dishes in the course are ready on the pass.
        /// </summary>
        public bool AllDishesReady()
        {
            foreach (var dish in Dishes)
            {
                if (dish.Status != DishStatus.OnPass && dish.Status != DishStatus.Walking)
                    return false;
            }
            return true;
        }
    }
}
