using UnityEngine;
using Expo.Core;
using Expo.Core.Events;
using Expo.Core.Debug;

namespace Expo.Managers
{
    /// <summary>
    /// Manages the shift timer system.
    /// RESPONSIBILITIES:
    /// - Tracks real-time elapsed and converts to simulated time (5PM-9PM)
    /// - 1 real-time minute = 1 simulated hour (at 1x speed)
    /// - Stops ticket spawning at 9PM (4 minutes elapsed)
    /// - Monitors all tables cleared condition to end the game
    /// - Publishes events for UI updates and game end
    /// </summary>
    public class ShiftTimerManager : CoreManager
    {
        [Header("Shift Configuration")]
        [Tooltip("Simulated hour when shift starts (17 = 5PM)")]
        [SerializeField] private int shiftStartHour = 17; // 5PM
        
        [Tooltip("Simulated hour when tickets stop spawning (21 = 9PM)")]
        [SerializeField] private int ticketCutoffHour = 21; // 9PM
        
        [Tooltip("Real-time minutes per simulated hour (at 1x speed)")]
        [SerializeField] private float realMinutesPerSimHour = 1f;
        
        [Header("Manager References")]
        [SerializeField] private TableManager tableManager;
        [SerializeField] private Expo.Core.Managers.TicketManager ticketManager;
        
        // State tracking
        private float _shiftStartTime;
        private bool _shiftActive = false;
        private bool _ticketSpawningActive = true;
        private bool _shiftEnded = false;
        private int _ticketsServedCount = 0;
        
        protected override void OnInitialize()
        {
            // Subscribe to ticket completion events to track progress
            EventBus.Subscribe<TicketCompletedEvent>(OnTicketCompleted);
            
            DebugLogger.Log(DebugLogger.Category.TIME, "✅ ShiftTimerManager initialized!");
            
            // Start the shift immediately when initialized
            StartShift();
        }
        
        protected override void OnShutdown()
        {
            EventBus.Unsubscribe<TicketCompletedEvent>(OnTicketCompleted);
        }
        
        protected override void Update()
        {
            if (!_shiftActive || _shiftEnded)
                return;
            
            // Calculate elapsed time and simulated time
            float realTimeElapsed = GameTime.Time - _shiftStartTime;
            
            // Convert real time to simulated hours (accounting for game speed)
            // At 1x speed: 60 seconds = 1 simulated hour
            // At 2x speed: 30 seconds = 1 simulated hour
            // At 3x speed: 20 seconds = 1 simulated hour
            float simulatedHoursElapsed = realTimeElapsed / (realMinutesPerSimHour * 60f);
            
            // Calculate current simulated time
            int currentSimHour = shiftStartHour + Mathf.FloorToInt(simulatedHoursElapsed);
            float fractionalMinutes = (simulatedHoursElapsed - Mathf.Floor(simulatedHoursElapsed)) * 60f;
            int currentSimMinute = Mathf.FloorToInt(fractionalMinutes);
            
            // Check if we've reached the cutoff time to stop spawning tickets
            if (currentSimHour >= ticketCutoffHour && _ticketSpawningActive)
            {
                _ticketSpawningActive = false;
                DebugLogger.Log(DebugLogger.Category.TIME, 
                    $"⏰ SHIFT CUTOFF: It's {currentSimHour}:{currentSimMinute:D2} - No more new tickets will spawn! [LAST CALL]");
            }
            
            // Publish update event for UI
            EventBus.Publish(new ShiftTimerUpdatedEvent
            {
                RealTimeElapsed = realTimeElapsed,
                SimulatedHour = currentSimHour,
                SimulatedMinute = currentSimMinute,
                CanSpawnTickets = _ticketSpawningActive,
                Timestamp = GameTime.Time
            });
            
            // Debug: Log time updates
            int prevMinute = Mathf.FloorToInt(((realTimeElapsed - GameTime.DeltaTime) / (realMinutesPerSimHour * 60f) - Mathf.Floor((realTimeElapsed - GameTime.DeltaTime) / (realMinutesPerSimHour * 60f))) * 60f);
            if (currentSimMinute != prevMinute || currentSimMinute % 10 == 0)
            {
                DebugLogger.Log(DebugLogger.Category.TIME, 
                    $"⏰ {currentSimHour}:{currentSimMinute:D2} | Real: {realTimeElapsed:F1}s | Sim Hours: {simulatedHoursElapsed:F2}h | Publishing event...");
            }
            
            // Check if all tables are clear after cutoff
            if (!_ticketSpawningActive && !_shiftEnded)
            {
                CheckForShiftEnd();
            }
        }
        
        /// <summary>
        /// Starts the shift timer.
        /// </summary>
        private void StartShift()
        {
            _shiftStartTime = GameTime.Time;
            _shiftActive = true;
            _ticketSpawningActive = true;
            _shiftEnded = false;
            _ticketsServedCount = 0;
            
            DebugLogger.Log(DebugLogger.Category.TIME, 
                $"🍽️  SHIFT STARTED at {shiftStartHour}:00 (5PM). Tickets will stop spawning at {ticketCutoffHour}:00 (9PM). Start time: {_shiftStartTime}");
        }
        
        /// <summary>
        /// Checks if all tables are clear and ends the shift if so.
        /// </summary>
        private void CheckForShiftEnd()
        {
            if (tableManager == null)
                return;
            
            var occupiedTables = tableManager.GetOccupiedTables();
            
            if (occupiedTables.Count == 0)
            {
                EndShift();
            }
        }
        
        /// <summary>
        /// Ends the shift and publishes the completion event.
        /// </summary>
        private void EndShift()
        {
            if (_shiftEnded)
                return;
            
            _shiftEnded = true;
            _shiftActive = false;
            
            float totalDuration = GameTime.Time - _shiftStartTime;
            
            DebugLogger.Log(DebugLogger.Category.TIME, 
                $"✅ SHIFT COMPLETE! All tables served. Duration: {totalDuration / 60f:F2} real minutes. Tickets served: {_ticketsServedCount}");
            
            EventBus.Publish(new AllTablesServedEvent
            {
                ShiftDuration = totalDuration,
                TotalTicketsServed = _ticketsServedCount,
                RemainingTables = 0,
                Timestamp = GameTime.Time
            });
        }
        
        /// <summary>
        /// Tracks ticket completions for end-of-shift stats.
        /// </summary>
        private void OnTicketCompleted(TicketCompletedEvent e)
        {
            _ticketsServedCount++;
            DebugLogger.Log(DebugLogger.Category.TIME, 
                $"Ticket #{e.TicketId} completed. Total served: {_ticketsServedCount}");
        }
        
        // Public getters for other systems
        public bool CanSpawnTickets() => _ticketSpawningActive && _shiftActive && !_shiftEnded;
        public bool IsShiftActive() => _shiftActive && !_shiftEnded;
        public bool IsShiftEnded() => _shiftEnded;
        
        /// <summary>
        /// Gets the normalized shift time (0-1) where 0 = shift start, 1 = ticket cutoff time.
        /// Used for spawn probability curves and difficulty scaling.
        /// </summary>
        public float GetNormalizedShiftTime()
        {
            if (!_shiftActive)
                return 0f;
            
            float realTimeElapsed = GameTime.Time - _shiftStartTime;
            float simulatedHoursElapsed = realTimeElapsed / (realMinutesPerSimHour * 60f);
            
            // Calculate total shift duration in simulated hours (5PM to 9PM = 4 hours)
            float totalShiftHours = ticketCutoffHour - shiftStartHour;
            
            // Return normalized time (0-1)
            return Mathf.Clamp01(simulatedHoursElapsed / totalShiftHours);
        }
        
        /// <summary>
        /// Gets the current simulated time as a formatted string (e.g., "5:30 PM").
        /// </summary>
        public string GetCurrentTimeString()
        {
            float realTimeElapsed = GameTime.Time - _shiftStartTime;
            float simulatedHoursElapsed = realTimeElapsed / (realMinutesPerSimHour * 60f);
            
            int currentSimHour = shiftStartHour + Mathf.FloorToInt(simulatedHoursElapsed);
            float fractionalMinutes = (simulatedHoursElapsed - Mathf.Floor(simulatedHoursElapsed)) * 60f;
            int currentSimMinute = Mathf.FloorToInt(fractionalMinutes);
            
            // Clamp to reasonable time
            currentSimHour = Mathf.Min(currentSimHour, ticketCutoffHour + 1);
            
            // Convert to 12-hour format
            int displayHour = currentSimHour > 12 ? currentSimHour - 12 : currentSimHour;
            string ampm = currentSimHour >= 12 ? "PM" : "AM";
            
            return $"{displayHour}:{currentSimMinute:D2} {ampm}";
        }
    }
}
