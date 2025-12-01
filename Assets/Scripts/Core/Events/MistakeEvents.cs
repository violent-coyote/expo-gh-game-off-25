using System.Collections.Generic;
using Expo.Data;

namespace Expo.Core.Events
{
    /// <summary>
    /// Types of mistakes that can occur during service.
    /// </summary>
    public enum MistakeType
    {
        StaggeredCourse,  // Course dishes sent at different times (not in same send)
        DeadDish,         // Dish died on pass
        WrongTable,       // Dish sent to table with no expectation for it
        TicketOverdue     // Ticket took longer than optimal completion time
    }

    /// <summary>
    /// Represents a single mistake made during the shift.
    /// </summary>
    public class Mistake
    {
        public MistakeType Type;
        public int? TicketId;        // null for wrong table mistakes
        public int? CourseNumber;    // for staggered courses
        public DishData DishData;    // the dish involved
        public int? TableNumber;     // table where mistake occurred
        public float Timestamp;
        public string Description;   // Human-readable description
        
        // Additional data for staggered courses
        public List<DishServeInfo> StaggeredDishes; // For detailed timing info
    }

    /// <summary>
    /// Information about when a dish in a staggered course was served.
    /// </summary>
    public class DishServeInfo
    {
        public DishData DishData;
        public float ServeTime;
        public string FormattedTime; // e.g., "5:30 PM"
    }

    /// <summary>
    /// Published when the shift ends and the end-of-shift report should be displayed.
    /// Contains all mistakes made during the shift.
    /// </summary>
    public struct ShowEndOfShiftReportEvent : IEvent
    {
        public List<Mistake> Mistakes;
        public int TotalTicketsServed;
        public float ShiftDuration;
        public float Timestamp;
    }
}
