using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Expo.Core;
using Expo.Core.Debug;
using Expo.Data;
using Expo.Runtime;
using Expo.Core.Events;
using Expo.GameFeel;
using Expo.Core.Progression;

namespace Expo.Managers
{
    /// <summary>
    /// Tracks mistakes made during service.
    /// RESPONSIBILITIES:
    /// - Tracks staggered course deliveries (dishes not sent together)
    /// - Tracks dishes that die on the pass
    /// - Tracks dishes sent to wrong tables (no expectation)
    /// - Tracks premature dishes (sent before course is unlocked)
    /// - Provides end-of-shift report data
    /// 
    /// REFACTORED: Changed from scoring system to mistake tracking system
    /// </summary>
    public class ScoringManager : CoreManager
    {
        [Header("Mistake Tracking Parameters")]
        [Tooltip("Time window (seconds) within which dishes must be served to count as 'together'")]
        [SerializeField] private float togetherThreshold = 2f;

        [Header("References")]
        [SerializeField] private TableManager tableManager;
        [SerializeField] private ShiftTimerManager shiftTimerManager;
        [SerializeField] private Expo.UI.EndOfShiftReportUI endOfShiftReportUI;
        // Note: ProgressionManager uses singleton pattern - access via ProgressionManager.Instance

        // Track all mistakes made during the shift
        private readonly List<Mistake> _mistakesThisShift = new();
        
        // Track when each dish was served for course timing analysis
        private readonly Dictionary<int, float> _dishServedTimes = new();
        
        // Track active tickets and their courses
        private readonly Dictionary<int, TicketData> _activeTickets = new();
        
        // Track which courses have been checked to avoid duplicate mistake logging
        private readonly HashSet<string> _checkedCourses = new(); // "ticketId_courseNumber"

        protected override void OnInitialize()
        {
            EventBus.Subscribe<DishesServedEvent>(OnDishesServed);
            EventBus.Subscribe<TicketCreatedEvent>(OnTicketCreated);
            EventBus.Subscribe<DishDiedEvent>(OnDishDied);
            EventBus.Subscribe<AllTablesServedEvent>(OnAllTablesServed);
            
            _mistakesThisShift.Clear();
        }

        protected override void OnShutdown()
        {
            EventBus.Unsubscribe<DishesServedEvent>(OnDishesServed);
            EventBus.Unsubscribe<TicketCreatedEvent>(OnTicketCreated);
            EventBus.Unsubscribe<DishDiedEvent>(OnDishDied);
            EventBus.Unsubscribe<AllTablesServedEvent>(OnAllTablesServed);
            
            _dishServedTimes.Clear();
            _activeTickets.Clear();
            _checkedCourses.Clear();
            _mistakesThisShift.Clear();
        }

        private void OnTicketCreated(TicketCreatedEvent e)
        {
            // Ticket data will be registered via RegisterTicket() method
        }

        private void OnDishesServed(DishesServedEvent e)
        {
            float currentTime = GameTime.Time;
            
            // Record serve times for all dishes
            foreach (var dishId in e.DishInstanceIds)
            {
                _dishServedTimes[dishId] = currentTime;
            }
            
            // Check for wrong table and premature dish mistakes
            CheckForWrongTableMistakes(e);
            
            // Check for staggered course mistakes
            CheckCourseCompletion(e.DishInstanceIds);
        }
        
        private void OnDishDied(DishDiedEvent e)
        {
            // Log a mistake when a dish dies on the pass
            var mistake = new Mistake
            {
                Type = MistakeType.DeadDish,
                DishData = e.DishData,
                Timestamp = e.Timestamp,
                Description = $"{e.DishData.dishName} died on pass"
            };
            
            _mistakesThisShift.Add(mistake);
            
            DebugLogger.Log(DebugLogger.Category.MISTAKE, 
                $"❌ MISTAKE: {mistake.Description}");
            
            // Trigger game feel feedback
            PublishGameFeelEvent(mistake);
        }
        
        private void OnAllTablesServed(AllTablesServedEvent e)
        {
            // Shift is over - publish the end-of-shift report event
            DebugLogger.Log(DebugLogger.Category.MISTAKE, 
                $"🏁 Shift complete! Total mistakes: {_mistakesThisShift.Count}");
            
            // Calculate grade and award XP
            var progressionConfig = ProgressionConfigLoader.LoadProgressionConfig();
            var gradingThresholds = progressionConfig.gradingThresholds;
            int mistakeCount = _mistakesThisShift.Count;
            string grade = gradingThresholds.CalculateGrade(mistakeCount);
            int xpEarned = gradingThresholds.GetXPForGrade(grade);
            
            // Award XP to player through ProgressionManager
            bool leveledUp = false;
            int oldLevel = 0;
            int newLevel = 0;
            
            if (ProgressionManager.Instance != null)
            {
                oldLevel = ProgressionManager.Instance.GetCurrentPlayerLevel();
                leveledUp = ProgressionManager.Instance.AwardXP(xpEarned);
                newLevel = ProgressionManager.Instance.GetCurrentPlayerLevel();
                
                DebugLogger.Log(DebugLogger.Category.PROGRESSION, 
                    $"Awarded {xpEarned} XP for grade {grade}. Level: {oldLevel} → {newLevel}" + 
                    (leveledUp ? " 🎉 LEVEL UP!" : ""));
            }
            else
            {
                DebugLogger.LogWarning(DebugLogger.Category.PROGRESSION, 
                    "ProgressionManager.Instance is null! Cannot award XP. Make sure ProgressionManager exists in the scene.");
            }
            
            EventBus.Publish(new ShowEndOfShiftReportEvent
            {
                Mistakes = _mistakesThisShift,
                TotalTicketsServed = e.TotalTicketsServed,
                ShiftDuration = e.ShiftDuration,
                Timestamp = e.Timestamp
            });
            
            // Display the UI report with XP info
            if (endOfShiftReportUI != null)
            {
                endOfShiftReportUI.DisplayReport(_mistakesThisShift, e.TotalTicketsServed, e.ShiftDuration, 
                    xpEarned, leveledUp, oldLevel, newLevel);
            }
            else
            {
                DebugLogger.LogWarning(DebugLogger.Category.MISTAKE, 
                    "EndOfShiftReportUI reference is missing! Cannot display report.");
            }
        }

        /// <summary>
        /// Checks if dishes were sent to the wrong table.
        /// </summary>
        private void CheckForWrongTableMistakes(DishesServedEvent e)
        {
            var tableNumber = e.TableNumber;
            var orderState = tableManager?.GetTableOrderState(tableNumber);
            
            if (orderState == null)
            {
                DebugLogger.LogWarning(DebugLogger.Category.MISTAKE, 
                    $"No order state found for table {tableNumber} - this shouldn't happen!");
                return;
            }
            
            // Check each served dish for wrong table mistakes
            foreach (var dishData in e.ServedDishTypes)
            {
                // Check if this dish type exists in the table's order at all
                // We need to check across ALL expectations (served and unserved) to detect wrong table
                bool hasExpectationAnywhere = false;
                foreach (var courseOrder in orderState.Courses)
                {
                    foreach (var expectation in courseOrder.Dishes)
                    {
                        if (expectation.DishType.dishName == dishData.dishName)
                        {
                            hasExpectationAnywhere = true;
                            break;
                        }
                    }
                    if (hasExpectationAnywhere) break;
                }
                
                if (!hasExpectationAnywhere)
                {
                    // Wrong table - dish not expected on this table at all
                    var mistake = new Mistake
                    {
                        Type = MistakeType.WrongTable,
                        DishData = dishData,
                        TableNumber = tableNumber,
                        Timestamp = e.Timestamp,
                        Description = $"{dishData.dishName} wasn't expected on Table {tableNumber}"
                    };
                    
                    _mistakesThisShift.Add(mistake);
                    
                    DebugLogger.Log(DebugLogger.Category.MISTAKE, 
                        $"❌ MISTAKE: {mistake.Description}");
                    
                    // Trigger game feel feedback
                    PublishGameFeelEvent(mistake);
                }
            }
        }
        
        /// <summary>
        /// Checks if any courses were completed with the recently served dishes.
        /// Now checks for staggered course mistakes instead of scoring.
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
                    
                    // Skip if already checked
                    if (_checkedCourses.Contains(courseKey))
                        continue;
                    
                    // Check if all dishes in this course have been served
                    bool allDishesServed = course.Dishes.All(d => d.Status == DishStatus.Served);
                    
                    if (allDishesServed)
                    {
                        DebugLogger.Log(DebugLogger.Category.SCORE, 
                            $"Course {course.CourseNumber} on ticket #{ticket.TicketId} complete! All {course.Dishes.Count} dishes served");
                        
                        // Mark as checked to avoid duplicates
                        _checkedCourses.Add(courseKey);
                        
                        // Check for staggered course mistakes
                        CheckForStaggeredCourseMistake(ticket, course, orderState);
                        
                        // Publish course completed event (keep for other systems)
                        var courseExpectations = orderState.GetCourseExpectations(course.CourseNumber);
                        var serveTimes = courseExpectations
                            .Where(e => e.ServedTime.HasValue)
                            .Select(e => e.ServedTime.Value)
                            .ToList();
                        
                        float maxTime = serveTimes.Count > 0 ? serveTimes.Max() : GameTime.Time;
                        // No threshold - must be exact same time
                        bool allTogether = serveTimes.Count > 1 && serveTimes.All(t => Mathf.Approximately(t, serveTimes[0]));
                        
                        EventBus.Publish(new CourseCompletedEvent
                        {
                            TicketId = ticket.TicketId,
                            CourseNumber = course.CourseNumber,
                            DishCount = courseExpectations.Count,
                            CompletionTime = maxTime,
                            AllDishesTogether = allTogether,
                            TimingScore = 0f // No longer used
                        });
                        
                        // Check if entire ticket is complete
                        if (ticket.Courses.All(c => c.Dishes.All(d => d.Status == DishStatus.Served)))
                        {
                            DebugLogger.Log(DebugLogger.Category.SCORE, 
                                $"All dishes served for ticket #{ticket.TicketId} - removing from active tickets");
                            _activeTickets.Remove(ticket.TicketId);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks if a completed course had dishes sent at staggered times (mistake).
        /// </summary>
        private void CheckForStaggeredCourseMistake(TicketData ticket, CourseData course, TableOrderState orderState)
        {
            // Skip courses with only one dish (can't be staggered)
            if (course.Dishes.Count <= 1)
                return;
            
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

            if (serveTimes.Count <= 1) return;

            // Check if all dishes were served at the EXACT same time (same send action)
            // If there are any differences in serve time, it's a mistake
            float firstTime = serveTimes[0];
            bool allSameTime = serveTimes.All(t => Mathf.Approximately(t, firstTime));
            
            if (!allSameTime)
            {
                // Staggered course - create mistake with detailed timing info
                var staggeredDishes = new List<DishServeInfo>();
                
                foreach (var expectation in courseExpectations)
                {
                    if (expectation.ServedTime.HasValue)
                    {
                        staggeredDishes.Add(new DishServeInfo
                        {
                            DishData = expectation.DishType,
                            ServeTime = expectation.ServedTime.Value,
                            FormattedTime = FormatGameTime(expectation.ServedTime.Value)
                        });
                    }
                }
                
                // Sort by serve time
                staggeredDishes.Sort((a, b) => a.ServeTime.CompareTo(b.ServeTime));
                
                // Create description with dish names and times
                var dishDetails = string.Join(", ", staggeredDishes.Select(d => 
                    $"{d.DishData.dishName} at {d.FormattedTime}"));
                
                var mistake = new Mistake
                {
                    Type = MistakeType.StaggeredCourse,
                    TicketId = ticket.TicketId,
                    CourseNumber = course.CourseNumber,
                    TableNumber = ticket.AssignedTable.TableNumber,
                    Timestamp = serveTimes.Max(),
                    StaggeredDishes = staggeredDishes,
                    Description = $"Course {course.CourseNumber} on Ticket #{ticket.TicketId} (Table {ticket.AssignedTable.TableNumber}): {dishDetails}"
                };
                
                _mistakesThisShift.Add(mistake);
                
                DebugLogger.Log(DebugLogger.Category.MISTAKE, 
                    $"❌ MISTAKE: Staggered course - {mistake.Description}");
                
                // Trigger game feel feedback
                PublishGameFeelEvent(mistake);
            }
        }
        
        /// <summary>
        /// Formats a game timestamp into human-readable time (e.g., "5:30 PM").
        /// </summary>
        private string FormatGameTime(float timestamp)
        {
            // Use ShiftTimerManager if available to get proper time formatting
            if (shiftTimerManager != null)
            {
                return shiftTimerManager.GetCurrentTimeString();
            }
            
            // Fallback: just return the raw timestamp
            return $"{timestamp:F1}s";
        }

        /// <summary>
        /// Registers a ticket for mistake tracking.
        /// Should be called by TicketManager when a new ticket is created.
        /// </summary>
        public void RegisterTicket(TicketData ticket)
        {
            _activeTickets[ticket.TicketId] = ticket;
            DebugLogger.Log(DebugLogger.Category.MISTAKE, 
                $"Registered ticket #{ticket.TicketId} for mistake tracking");
        }
        
        /// <summary>
        /// Unregisters a ticket (called when ticket is completed/removed).
        /// </summary>
        public void UnregisterTicket(int ticketId)
        {
            _activeTickets.Remove(ticketId);
        }
        
        /// <summary>
        /// Gets the list of all mistakes made during the current shift.
        /// Called by UI to display the end-of-shift report.
        /// </summary>
        public List<Mistake> GetMistakes()
        {
            return _mistakesThisShift;
        }
        
        /// <summary>
        /// Gets the count of mistakes made during the current shift.
        /// </summary>
        public int GetMistakeCount()
        {
            return _mistakesThisShift.Count;
        }
        
        /// <summary>
        /// Gets the count of mistakes by type.
        /// </summary>
        public int GetMistakeCountByType(MistakeType type)
        {
            return _mistakesThisShift.Count(m => m.Type == type);
        }

        /// <summary>
        /// Publishes a GameFeelEvent to trigger visual/audio feedback for mistakes.
        /// </summary>
        private void PublishGameFeelEvent(Mistake mistake)
        {
            EventBus.Publish(new GameFeelEvent
            {
                EventType = GameFeelEventType.Mistake,
                Timestamp = mistake.Timestamp,
                Context = mistake
            });
        }
    }
}
