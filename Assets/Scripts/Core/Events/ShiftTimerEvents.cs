namespace Expo.Core.Events
{
    /// <summary>
    /// Published when the shift timer updates (every frame or at significant intervals).
    /// Used by UI to display current simulated time.
    /// </summary>
    public struct ShiftTimerUpdatedEvent : IEvent
    {
        /// <summary>Real-time elapsed since shift start (0 = 5PM, 4 = 9PM)</summary>
        public float RealTimeElapsed;
        
        /// <summary>Simulated hour (17 = 5PM, 21 = 9PM)</summary>
        public int SimulatedHour;
        
        /// <summary>Simulated minute (0-59)</summary>
        public int SimulatedMinute;
        
        /// <summary>Whether new tickets can still spawn (false after 9PM)</summary>
        public bool CanSpawnTickets;
        
        public float Timestamp;
    }
    
    /// <summary>
    /// Published when all tables have been served after 9PM cutoff.
    /// Signals the end of the shift/game.
    /// </summary>
    public struct AllTablesServedEvent : IEvent
    {
        /// <summary>Total shift duration in real time (minutes)</summary>
        public float ShiftDuration;
        
        /// <summary>Number of tickets completed during shift</summary>
        public int TotalTicketsServed;
        
        /// <summary>Number of active tables at shift end</summary>
        public int RemainingTables;
        
        public float Timestamp;
    }
}
