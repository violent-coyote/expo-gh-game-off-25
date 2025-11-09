using System;
using Expo.Core.Debug;

namespace Expo.Data
{
    /// <summary>
    /// Runtime data representing a table in the restaurant.
    /// Tables have a number, party size (2-top, 4-top, etc.), and influence ticket size.
    /// Tables own their tickets and manage eating state.
    /// </summary>
    [Serializable]
    public class TableData
    {
        public int TableNumber { get; private set; }
        public int PartySize { get; private set; } // 2, 4, 6, etc.
        public bool IsOccupied { get; private set; }
        public int? CurrentTicketId { get; private set; } // Track which ticket is at this table
        
        // Course progression - Tables own their course state!
        public int CurrentCourseNumber { get; private set; } // Which course the table is currently on
        public int TotalCourses { get; private set; } // Total courses for this table's ticket
        
        // Eating state tracking
        public bool IsEating { get; private set; }
        public int CurrentCourseBeingEaten { get; private set; }
        public float EatingStartTime { get; private set; }
        public float EatingDuration { get; private set; }

        public TableData(int tableNumber, int partySize)
        {
            TableNumber = tableNumber;
            PartySize = partySize;
            IsOccupied = false;
            CurrentTicketId = null;
            CurrentCourseNumber = 0; // Not yet started
            TotalCourses = 0;
            IsEating = false;
            CurrentCourseBeingEaten = 0;
            EatingStartTime = 0f;
            EatingDuration = 0f;
        }

        /// <summary>
        /// Gets the recommended dish count for this party size.
        /// 2-top: 2-3 dishes, 4-top: 4-6 dishes, etc.
        /// Capped at 4 dishes per ticket as per game design.
        /// </summary>
        public int GetRecommendedDishCount()
        {
            // Rule: roughly 1-1.5 dishes per person, capped at 4
            int min = PartySize;
            int max = (int)(PartySize * 1.5f);
            int count = UnityEngine.Random.Range(min, max + 1);
            return UnityEngine.Mathf.Clamp(count, 1, 9);
        }

        /// <summary>
        /// Gets the recommended course count for this party size.
        /// 2-top: 1-2 courses, 4-top: 2-3 courses.
        /// </summary>
        public int GetRecommendedCourseCount()
        {
            if (PartySize <= 2)
                return UnityEngine.Random.Range(1, 3); // 1-2 courses
            else
                return UnityEngine.Random.Range(2, 4); // 2-3 courses
        }

        /// <summary>
        /// Seats a party at this table and assigns them a ticket.
        /// REFACTORED: Now also initializes course progression.
        /// </summary>
        public void SeatParty(int ticketId, int totalCourses)
        {
            if (IsOccupied)
            {
                DebugLogger.LogWarning(DebugLogger.Category.TABLE, $"Table {TableNumber} is already occupied with ticket {CurrentTicketId}!");
                return;
            }
            
            IsOccupied = true;
            CurrentTicketId = ticketId;
            CurrentCourseNumber = 1; // Start on course 1
            TotalCourses = totalCourses;
            
            DebugLogger.Log(DebugLogger.Category.TABLE, 
                $"Table {TableNumber} ({PartySize}-top) seated party with ticket #{ticketId} - {totalCourses} course(s)");
        }

        /// <summary>
        /// Starts the eating timer for a specific course.
        /// </summary>
        public void StartEating(int courseNumber, float duration)
        {
            IsEating = true;
            CurrentCourseBeingEaten = courseNumber;
            EatingStartTime = Expo.Core.GameTime.Time;
            EatingDuration = duration;
            DebugLogger.Log(DebugLogger.Category.TABLE, $"Table {TableNumber} started eating course {courseNumber} (duration: {duration}s)");
        }

        /// <summary>
        /// Checks if the table has finished eating the current course.
        /// </summary>
        public bool HasFinishedEating()
        {
            if (!IsEating) return false;
            
            float elapsed = Expo.Core.GameTime.Time - EatingStartTime;
            return elapsed >= EatingDuration;
        }

        /// <summary>
        /// Completes the eating state (call this after HasFinishedEating returns true).
        /// </summary>
        public void FinishEating()
        {
            DebugLogger.Log(DebugLogger.Category.TABLE, $"Table {TableNumber} finished eating course {CurrentCourseBeingEaten}");
            IsEating = false;
            CurrentCourseBeingEaten = 0;
            EatingStartTime = 0f;
            EatingDuration = 0f;
        }
        
        /// <summary>
        /// Unlocks the next course for this table.
        /// Returns true if a course was unlocked, false if all courses are already complete.
        /// REFACTORED: Tables now own course progression!
        /// </summary>
        public bool UnlockNextCourse()
        {
            if (CurrentCourseNumber >= TotalCourses)
            {
                DebugLogger.Log(DebugLogger.Category.TABLE, 
                    $"Table {TableNumber}: No more courses to unlock (at {CurrentCourseNumber}/{TotalCourses})");
                return false;
            }
            
            CurrentCourseNumber++;
            
            DebugLogger.Log(DebugLogger.Category.TABLE, 
                $"Table {TableNumber}: Course {CurrentCourseNumber} unlocked!");
            
            return true;
        }
        
        /// <summary>
        /// Checks if this table is on their last course.
        /// </summary>
        public bool IsOnLastCourse()
        {
            return CurrentCourseNumber >= TotalCourses;
        }
        
        /// <summary>
        /// Checks if the current course is unlocked (can fire dishes from it).
        /// </summary>
        public bool IsCourseUnlocked(int courseNumber)
        {
            return courseNumber <= CurrentCourseNumber;
        }

        /// <summary>
        /// Clears the table when the party leaves.
        /// REFACTORED: Also resets course progression.
        /// </summary>
        public void ClearTable()
        {
            if (IsEating)
            {
                DebugLogger.LogWarning(DebugLogger.Category.TABLE, $"Table {TableNumber} cleared while eating!");
            }
            
            DebugLogger.Log(DebugLogger.Category.TABLE, $"Table {TableNumber} cleared (was serving ticket #{CurrentTicketId})");
            IsOccupied = false;
            CurrentTicketId = null;
            CurrentCourseNumber = 0;
            TotalCourses = 0;
            IsEating = false;
            CurrentCourseBeingEaten = 0;
            EatingStartTime = 0f;
            EatingDuration = 0f;
        }

        public void AssignTicket(int ticketId)
        {
            IsOccupied = true;
            CurrentTicketId = ticketId;
        }

        public void ClearTicket()
        {
            ClearTable();
        }
    }
}
