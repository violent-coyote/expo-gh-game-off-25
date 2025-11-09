using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Expo.Core;
using Expo.Core.Debug;
using Expo.Data;
using Expo.Runtime;
using Expo.Core.Events;

namespace Expo.Managers
{
    /// <summary>
    /// Manages scoring for course-based service.
    /// RESPONSIBILITIES:
    /// - Tracks when dishes in a course are served
    /// - Calculates scores based on course timing (dishes sent together vs. staggered)
    /// - Publishes course completion and scoring events
    /// 
    /// REFACTORED: Now works with TableOrderState instead of CourseData.Expectations
    /// </summary>
    public class ScoringManager : CoreManager
    {
        [Header("Scoring Parameters")]
        [Tooltip("Time window (seconds) within which dishes must be served to count as 'together'")]
        [SerializeField] private float togetherThreshold = 2f;
        
        [Tooltip("Score multiplier for perfect course timing (all dishes together)")]
        [SerializeField] private float perfectCourseMultiplier = 2f;
        
        [Tooltip("Base score per dish")]
        [SerializeField] private float baseScorePerDish = 10f;

        [Header("References")]
        [SerializeField] private TableManager tableManager;

        // Track when each dish was served
        private readonly Dictionary<int, float> _dishServedTimes = new();
        
        // Track active tickets and their courses
        private readonly Dictionary<int, TicketData> _activeTickets = new();
        
        // Track which courses have been scored to avoid duplicates
        private readonly HashSet<string> _scoredCourses = new(); // "ticketId_courseNumber"

        protected override void OnInitialize()
        {
            EventBus.Subscribe<DishesServedEvent>(OnDishesServed);
            EventBus.Subscribe<TicketCreatedEvent>(OnTicketCreated);
        }

        protected override void OnShutdown()
        {
            EventBus.Unsubscribe<DishesServedEvent>(OnDishesServed);
            EventBus.Unsubscribe<TicketCreatedEvent>(OnTicketCreated);
            _dishServedTimes.Clear();
            _activeTickets.Clear();
            _scoredCourses.Clear();
        }

        private void OnTicketCreated(TicketCreatedEvent e)
        {
            // We'll need access to ticket data - this should be expanded to receive TicketData
            // For now, we'll rely on other systems passing us the data
        }

        private void OnDishesServed(DishesServedEvent e)
        {
            float currentTime = GameTime.Time;
            
            // Record serve times for all dishes
            foreach (var dishId in e.DishInstanceIds)
            {
                _dishServedTimes[dishId] = currentTime;
            }
            
            // Check each active ticket to see if any courses were completed
            CheckCourseCompletion(e.DishInstanceIds);
        }

        /// <summary>
        /// Registers a ticket for scoring tracking.
        /// Should be called by TicketManager when a new ticket is created.
        /// </summary>
        public void RegisterTicket(TicketData ticket)
        {
            _activeTickets[ticket.TicketId] = ticket;
            DebugLogger.Log(DebugLogger.Category.SCORE, $"Registered ticket #{ticket.TicketId} with {ticket.Courses.Count} course(s)");
        }

        /// <summary>
        /// Checks if any courses were completed with the recently served dishes.
        /// REFACTORED: Now uses TableOrderState instead of CourseData.Expectations
        /// </summary>
        private void CheckCourseCompletion(List<int> servedDishIds)
        {
            foreach (var ticket in _activeTickets.Values.ToList())
            {
                // Skip if no assigned table
                if (ticket.AssignedTable == null)
                    continue;

                // Get the table's order state
                var orderState = tableManager?.GetTableOrderState(ticket.AssignedTable.TableNumber);
                if (orderState == null)
                    continue;

                foreach (var course in ticket.Courses)
                {
                    string courseKey = $"{ticket.TicketId}_{course.CourseNumber}";
                    
                    // Skip if already scored
                    if (_scoredCourses.Contains(courseKey))
                        continue;
                    
                    // Check if all dishes in this course have been served (by checking DishState)
                    // This works even if dishes were reassigned to different tables
                    bool allDishesServed = course.Dishes.All(d => d.Status == DishStatus.Served);
                    
                    if (allDishesServed)
                    {
                        DebugLogger.Log(DebugLogger.Category.SCORE, 
                            $"Course {course.CourseNumber} on ticket #{ticket.TicketId} complete! All {course.Dishes.Count} dishes served (possibly to different tables)");
                        
                        // Mark as scored to avoid duplicates
                        _scoredCourses.Add(courseKey);
                        
                        // Calculate course score using the original table's expectations
                        // (This gets expectations from wherever the dishes ended up)
                        ScoreCourse(ticket, course, orderState);
                        
                        // Check if entire ticket is complete
                        if (ticket.Courses.All(c => c.Dishes.All(d => d.Status == DishStatus.Served)))
                        {
                            DebugLogger.Log(DebugLogger.Category.SCORE, $"All dishes served for ticket #{ticket.TicketId} - removing from active tickets");
                            _activeTickets.Remove(ticket.TicketId);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Calculates and publishes the score for a completed course.
        /// REFACTORED: Uses TableOrderState expectations instead of CourseData.Expectations
        /// </summary>
        private void ScoreCourse(TicketData ticket, CourseData course, TableOrderState orderState)
        {
            var serveTimes = new List<float>();
            
            // Get expectations for this course from the table's order state
            var courseExpectations = orderState.GetCourseExpectations(course.CourseNumber);
            
            // Collect serve times from expectations
            foreach (var expectation in courseExpectations)
            {
                if (expectation.ServedTime.HasValue)
                {
                    serveTimes.Add(expectation.ServedTime.Value);
                }
            }

            if (serveTimes.Count == 0) return;

            // Check if all dishes were served within the threshold window
            float minTime = serveTimes.Min();
            float maxTime = serveTimes.Max();
            float timeSpread = maxTime - minTime;
            
            bool allTogether = timeSpread <= togetherThreshold;
            
            // Calculate score
            float baseScore = courseExpectations.Count * baseScorePerDish;
            float timingMultiplier = allTogether ? perfectCourseMultiplier : (1f - (timeSpread / 10f)); // Penalty for spread
            timingMultiplier = Mathf.Max(timingMultiplier, 0.5f); // Minimum 50% of base score
            
            float finalScore = baseScore * timingMultiplier;
            
            // Generate feedback
            string feedback;
            if (allTogether)
            {
                feedback = "Perfect timing! All dishes sent together.";
            }
            else if (timeSpread < 5f)
            {
                feedback = "Good timing, minor delays.";
            }
            else
            {
                feedback = "Staggered service, significant delays.";
            }
            
            DebugLogger.Log(DebugLogger.Category.SCORE, $"Course {course.CourseNumber} on ticket #{ticket.TicketId}: {feedback} (Score: {finalScore:F1}, Spread: {timeSpread:F2}s)");
            
            // Publish events
            EventBus.Publish(new CourseCompletedEvent
            {
                TicketId = ticket.TicketId,
                CourseNumber = course.CourseNumber,
                DishCount = courseExpectations.Count,
                CompletionTime = maxTime,
                AllDishesTogether = allTogether,
                TimingScore = finalScore
            });
            
            EventBus.Publish(new CourseScoreEvent
            {
                TicketId = ticket.TicketId,
                CourseNumber = course.CourseNumber,
                Score = finalScore,
                Feedback = feedback
            });
        }

        /// <summary>
        /// Unregisters a ticket (called when ticket is completed/removed).
        /// </summary>
        public void UnregisterTicket(int ticketId)
        {
            _activeTickets.Remove(ticketId);
            
            // Clean up dish served times for this ticket
            // (In a real implementation, you'd track which dishes belong to which tickets)
        }
    }
}
