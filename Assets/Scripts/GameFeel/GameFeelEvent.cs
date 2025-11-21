using Expo.Core.Events;

namespace Expo.GameFeel
{
    /// <summary>
    /// Types of game feel triggers.
    /// Each type can have different effect profiles.
    /// </summary>
    public enum GameFeelEventType
    {
        Mistake,           // Any mistake occurs
        TicketSpawned,     // New ticket arrives
        TicketCompleted,   // Ticket fully served
        CourseCompleted,   // Course successfully served
        PerfectService     // No mistakes made
    }

    /// <summary>
    /// Published whenever a game event occurs that should trigger game feel effects.
    /// This is the main event that GameFeelManager listens to.
    /// </summary>
    public struct GameFeelEvent : IEvent
    {
        public GameFeelEventType EventType;
        public float Timestamp;
        
        // Optional context data
        public object Context; // Can be Mistake, TicketData, etc.
    }
}
