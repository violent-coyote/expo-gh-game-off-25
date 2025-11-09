using Expo.Data;

namespace Expo.Core.Events
{
    /// <summary>
    /// Published when a course is completed (all dishes in the course are served together).
    /// </summary>
    public struct CourseCompletedEvent : IEvent
    {
        public int TicketId;
        public int CourseNumber;
        public int DishCount;
        public float CompletionTime;
        public bool AllDishesTogether; // True if all dishes were sent at the same time
        public float TimingScore; // Score based on how well-timed the course was
    }
    
    /// <summary>
    /// Published when scoring is calculated for a course.
    /// </summary>
    public struct CourseScoreEvent : IEvent
    {
        public int TicketId;
        public int CourseNumber;
        public float Score;
        public string Feedback; // e.g., "Perfect timing!", "Staggered", etc.
    }
    
    /// <summary>
    /// Published when a course is unlocked after a table finishes eating.
    /// This allows UI and other systems to update when new dishes become available to fire.
    /// </summary>
    public struct CourseUnlockedEvent : IEvent
    {
        public int TicketId;
        public int TableNumber;
        public int CourseNumber; // The course that was just unlocked
        public float Timestamp;
    }
}
