using UnityEngine;

namespace Expo.Data
{
    /// <summary>
    /// Tracks timing information for a ticket to determine if it's overdue
    /// and what pulse intensity should be shown on the UI.
    /// </summary>
    public class TicketTimingData
    {
        /// <summary>
        /// The game time when this ticket was spawned/created.
        /// </summary>
        public float SpawnTime { get; private set; }
        
        /// <summary>
        /// The optimal time (in game seconds) to complete this ticket based on dish count.
        /// Formula: 30 + (dishCount - 1) * 7.5 minutes (in-game), capped at 60 minutes.
        /// Converted to game seconds (multiply by 60).
        /// </summary>
        public float OptimalCompletionTime { get; private set; }
        
        /// <summary>
        /// Whether the overdue mistake has been triggered for this ticket.
        /// Only triggers once when ticket goes over optimal time.
        /// </summary>
        public bool OverdueMistakeTriggered { get; set; }
        
        /// <summary>
        /// Whether this ticket has been completed (to stop pulse animations).
        /// </summary>
        public bool IsCompleted { get; set; }

        public TicketTimingData(float spawnTime, int dishCount)
        {
            SpawnTime = spawnTime;
            OptimalCompletionTime = CalculateOptimalTime(dishCount);
            OverdueMistakeTriggered = false;
            IsCompleted = false;
        }

        /// <summary>
        /// Calculates optimal completion time based on dish count.
        /// Time is in real seconds, which maps to simulated time:
        /// - 60 real seconds = 1 simulated hour = 60 simulated minutes
        /// - 1 real second = 1 simulated minute
        /// 
        /// So: 1 dish = 30 simulated minutes = 30 real seconds
        ///     5+ dishes = 60 simulated minutes = 60 real seconds
        /// Linear interpolation between.
        /// </summary>
        private float CalculateOptimalTime(int dishCount)
        {
            // Clamp dish count to minimum of 1
            dishCount = Mathf.Max(1, dishCount);
            
            // Linear formula: 30 + (dishCount - 1) * 7.5 simulated minutes
            // Since 1 real second = 1 simulated minute, this is also the real seconds
            float simulatedMinutes = Mathf.Min(30f + (dishCount - 1) * 7.5f, 60f);
            
            return simulatedMinutes; // Returns real seconds, which equals simulated minutes
        }

        /// <summary>
        /// Gets the elapsed time since the ticket was spawned.
        /// </summary>
        public float GetElapsedTime(float currentGameTime)
        {
            return currentGameTime - SpawnTime;
        }

        /// <summary>
        /// Gets the remaining time before ticket becomes overdue.
        /// Returns negative value if already overdue.
        /// </summary>
        public float GetRemainingTime(float currentGameTime)
        {
            return OptimalCompletionTime - GetElapsedTime(currentGameTime);
        }

        /// <summary>
        /// Gets the normalized time remaining (0 = out of time, 1 = full time remaining).
        /// Returns negative values if overdue.
        /// </summary>
        public float GetNormalizedTimeRemaining(float currentGameTime)
        {
            float elapsed = GetElapsedTime(currentGameTime);
            return 1f - (elapsed / OptimalCompletionTime);
        }

        /// <summary>
        /// Checks if the ticket is currently overdue.
        /// </summary>
        public bool IsOverdue(float currentGameTime)
        {
            return GetRemainingTime(currentGameTime) <= 0f;
        }

        /// <summary>
        /// Gets the current pulse intensity based on time remaining.
        /// Returns 0 if completed or if more than 50% time remains.
        /// Returns 0-1 for 50% down to 10% time remaining (linear increase).
        /// Returns 1.0 (maximum) when less than 10% time remaining or overdue.
        /// </summary>
        public float GetPulseIntensity(float currentGameTime)
        {
            if (IsCompleted)
                return 0f;

            float normalizedTime = GetNormalizedTimeRemaining(currentGameTime);
            
            // No pulse if more than 50% time remaining
            if (normalizedTime > 0.5f)
                return 0f;
            
            // Maximum pulse if less than 10% time remaining or overdue
            if (normalizedTime <= 0.1f)
                return 1f;
            
            // Linear interpolation between 50% and 10% time remaining
            // Map 0.5 -> 0.0 intensity, 0.1 -> 1.0 intensity
            float intensityRange = 0.5f - 0.1f; // = 0.4
            float timeInRange = normalizedTime - 0.1f;
            return 1f - (timeInRange / intensityRange);
        }

        /// <summary>
        /// Gets a human-readable string for remaining time.
        /// Shows simulated minutes (1 real second = 1 simulated minute).
        /// </summary>
        public string GetRemainingTimeString(float currentGameTime)
        {
            float remaining = GetRemainingTime(currentGameTime);
            
            if (remaining <= 0)
                return "OVERDUE";
            
            // Since 1 real second = 1 simulated minute, just show as minutes
            return $"{Mathf.FloorToInt(remaining)}m";
        }
    }
}
