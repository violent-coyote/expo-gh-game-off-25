using System;
using System.Collections.Generic;
using System.Linq;
using Expo.Core.Debug;
using Expo.Runtime;

namespace Expo.Data
{
    /// <summary>
    /// Tracks what a table's order needs.
    /// REFACTORED: Now uses dish TYPE (name) only - no instance ID tracking!
    /// Any dish of the correct type can fulfill any expectation.
    ///
    /// RESPONSIBILITIES:
    /// - Track dish expectations by type and course
    /// - Mark expectations as fulfilled when matching dish types are served
    /// - Track order completion
    /// - Detect mistakes (dishes sent to wrong course)
    /// </summary>
    [Serializable]
    public class TableOrderState
    {
        public int TableNumber { get; private set; }
        public int TicketId { get; private set; }

        /// <summary>
        /// Course-based expectations (ordered by course number).
        /// NEW: Uses dish TYPE (name) only, no instance IDs.
        /// </summary>
        public List<CourseOrder> Courses { get; private set; }

        /// <summary>
        /// Track mistakes: dishes sent to table before their course was ready.
        /// </summary>
        public int MistakeCount { get; private set; }

        public TableOrderState(int tableNumber, int ticketId)
        {
            TableNumber = tableNumber;
            TicketId = ticketId;
            Courses = new List<CourseOrder>();
            MistakeCount = 0;
        }

        /// <summary>
        /// Adds a dish expectation to this table's order.
        /// REFACTORED: Uses dish TYPE (name) only - no instance ID tracking!
        /// </summary>
        public void AddDishExpectation(int courseNumber, DishData dishType, int dishInstanceId)
        {
            // Find or create course
            var course = Courses.Find(c => c.CourseNumber == courseNumber);
            if (course == null)
            {
                course = new CourseOrder(courseNumber);
                Courses.Add(course);
                Courses.Sort((a, b) => a.CourseNumber.CompareTo(b.CourseNumber));
            }

            // Create expectation (still pass instance ID for backward compatibility, but we won't use it for matching)
            var expectation = new DishExpectation(dishType, dishInstanceId, courseNumber);
            course.Dishes.Add(expectation);

            DebugLogger.Log(DebugLogger.Category.TABLE,
                $"Table {TableNumber}: Added expectation for '{dishType.dishName}' to course {courseNumber}");
        }

        /// <summary>
        /// Marks a dish type as served by finding the first unserved expectation of that type.
        /// REFACTORED: Uses dish TYPE (name) matching, not instance ID!
        /// Returns true if a matching expectation was found and marked as served.
        /// </summary>
        /// <param name="dishTypeName">The name of the dish type (e.g., "Steak", "Salad")</param>
        /// <param name="serveTime">When the dish was served</param>
        /// <param name="courseNumber">Which course this dish was sent for (to detect mistakes)</param>
        /// <param name="currentTableCourse">Which course the table is currently on</param>
        public bool MarkDishServed(string dishTypeName, float serveTime, int courseNumber, int currentTableCourse)
        {
            // Find the first unserved expectation of this dish type
            DishExpectation matchingExpectation = null;
            
            foreach (var course in Courses)
            {
                foreach (var expectation in course.Dishes)
                {
                    if (expectation.DishType.dishName == dishTypeName && !expectation.IsServed)
                    {
                        matchingExpectation = expectation;
                        break;
                    }
                }
                if (matchingExpectation != null) break;
            }

            if (matchingExpectation == null)
            {
                DebugLogger.LogWarning(DebugLogger.Category.TABLE,
                    $"Table {TableNumber}: Received '{dishTypeName}' but has no unserved expectation for it!");
                return false;
            }

            // Check if this is a mistake (sent before the course was ready)
            if (courseNumber < currentTableCourse)
            {
                MistakeCount++;
                DebugLogger.LogWarning(DebugLogger.Category.TABLE,
                    $"Table {TableNumber}: ⚠️ MISTAKE! '{dishTypeName}' sent for course {courseNumber} but table is on course {currentTableCourse}");
            }

            matchingExpectation.MarkServed(serveTime);

            DebugLogger.Log(DebugLogger.Category.TABLE,
                $"Table {TableNumber}: Marked '{dishTypeName}' from course {matchingExpectation.CourseNumber} as SERVED");

            return true;
        }

        /// <summary>
        /// Checks if this table's entire order is complete (all dishes served).
        /// REFACTORED: Iterates through all expectations since we don't have a lookup map anymore.
        /// </summary>
        public bool IsOrderComplete()
        {
            var allExpectations = GetAllExpectations();
            
            if (allExpectations.Count == 0)
            {
                DebugLogger.LogWarning(DebugLogger.Category.TABLE,
                    $"Table {TableNumber}: Order has no dishes!");
                return false;
            }

            bool allServed = allExpectations.All(exp => exp.IsServed);

            if (allServed)
            {
                DebugLogger.Log(DebugLogger.Category.TABLE,
                    $"Table {TableNumber}: Order COMPLETE - all {allExpectations.Count} dishes served!");
            }

            return allServed;
        }

        /// <summary>
        /// Checks if a specific course is complete (all dishes in that course served).
        /// </summary>
        public bool IsCourseComplete(int courseNumber)
        {
            var course = Courses.Find(c => c.CourseNumber == courseNumber);
            if (course == null)
            {
                // Course not found - this can happen if all dishes were reassigned away
                // Return false silently (not a warning, just means this table doesn't have this course)
                return false;
            }

            return course.IsComplete;
        }

        /// <summary>
        /// Checks if this table's order contains a specific dish type.
        /// REFACTORED: Checks by dish TYPE name, not instance ID.
        /// </summary>
        public bool HasDishType(string dishTypeName)
        {
            foreach (var course in Courses)
            {
                foreach (var expectation in course.Dishes)
                {
                    if (expectation.DishType.dishName == dishTypeName && !expectation.IsServed)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Gets all dish expectations for this table (across all courses).
        /// </summary>
        public List<DishExpectation> GetAllExpectations()
        {
            var all = new List<DishExpectation>();
            foreach (var course in Courses)
            {
                all.AddRange(course.Dishes);
            }
            return all;
        }

        /// <summary>
        /// Gets all expectations for a specific course.
        /// </summary>
        public List<DishExpectation> GetCourseExpectations(int courseNumber)
        {
            var course = Courses.Find(c => c.CourseNumber == courseNumber);
            return course?.Dishes ?? new List<DishExpectation>();
        }

        /// <summary>
        /// Gets the total number of dishes in this order.
        /// REFACTORED: Counts all expectations across all courses.
        /// </summary>
        public int GetTotalDishCount()
        {
            return GetAllExpectations().Count;
        }

        /// <summary>
        /// Gets the number of dishes that have been served.
        /// REFACTORED: Counts served expectations across all courses.
        /// </summary>
        public int GetServedDishCount()
        {
            return GetAllExpectations().Count(exp => exp.IsServed);
        }

        /// <summary>
        /// Gets debug string showing current order state.
        /// </summary>
        public string GetDebugString()
        {
            var served = GetServedDishCount();
            var total = GetTotalDishCount();

            return $"Table {TableNumber} (Ticket #{TicketId}): {served}/{total} dishes served, {Courses.Count} courses";
        }
    }

    /// <summary>
    /// Represents a single course within a table's order.
    /// Groups dish expectations by course number.
    /// </summary>
    [Serializable]
    public class CourseOrder
    {
        public int CourseNumber { get; private set; }
        public List<DishExpectation> Dishes { get; private set; }

        public CourseOrder(int courseNumber)
        {
            CourseNumber = courseNumber;
            Dishes = new List<DishExpectation>();
        }

        /// <summary>
        /// Checks if all dishes in this course have been served.
        /// </summary>
        public bool IsComplete
        {
            get { return Dishes.Count > 0 && Dishes.All(d => d.IsServed); }
        }

        /// <summary>
        /// Gets the number of dishes in this course.
        /// </summary>
        public int DishCount
        {
            get { return Dishes.Count; }
        }
    }
}
