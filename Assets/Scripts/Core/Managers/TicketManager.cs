using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Expo.Core;
using Expo.Core.Debug;
using Expo.Data;
using Expo.Runtime;
using Expo.Core.Events;
using Expo.UI;
using Expo.Managers;
using Expo.GameFeel;

namespace Expo.Core.Managers
{
    /// <summary>
    /// Manages ticket creation, tracking, and completion.
    /// RESPONSIBILITIES:
    /// - Spawns tickets probabilistically based on AnimationCurve (configurable difficulty curve)
    /// - Tracks active tickets and their dishes
    /// - Checks for ticket completion and removes finished tickets
    /// - Creates and destroys TicketUI instances
    /// 
    /// SPAWN SYSTEM:
    /// - Uses AnimationCurve to control spawn probability over normalized shift time (0-1)
    /// - Spawn probability determines likelihood of ticket creation when table is available
    /// - Max active tickets adjusts based on curve: >= 1.0 = 5 max, otherwise uses maxActiveTickets
    /// - TableManager checks for available tables at fixed intervals (tableCheckInterval)
    /// - TicketManager evaluates spawn probability when table is found
    /// 
    /// NOTE: Does NOT change dish states - only checks status for completion.
    /// </summary>
    public class TicketManager : CoreManager
    {
        [Header("Spawn Configuration")]
        [Tooltip("Curve that controls spawn probability over time. X-axis = time (0-1 normalized shift), Y-axis = spawn chance (0-1). Higher values = more aggressive spawning.")]
        [SerializeField] private AnimationCurve spawnProbabilityCurve = AnimationCurve.EaseInOut(0f, 0.2f, 1f, 0.8f);
        
        [Tooltip("Maximum number of active tickets allowed at once")]
        [SerializeField] private int maxActiveTickets = 10;
        
        [Header("References")]
        [Tooltip("Reference to TableManager for assigning tables to tickets")]
        [SerializeField] private TableManager tableManager;
        
        [Tooltip("Reference to ScoringManager for tracking course completion")]
        [SerializeField] private ScoringManager scoringManager;
        
        [Tooltip("Reference to ShiftTimerManager for normalized time")]
        [SerializeField] private ShiftTimerManager shiftTimerManager;
        
        // Loaded dynamically from disk and filtered by player selection
        [HideInInspector] private List<DishData> availableDishes;


        private readonly List<TicketData> _activeTickets = new();
		private readonly Dictionary<int, TicketUI> _ticketUIs = new();

        private int _ticketCounter = 0;
		
		[SerializeField] private GameObject ticketUIPrefab;
		[SerializeField] private Transform ticketRailParent;


		protected override void OnInitialize()
		{
			_activeTickets.Clear();
			_ticketCounter = 0;
			EventBus.Subscribe<DishesServedEvent>(OnDishesServed);
			EventBus.Subscribe<CourseCompletedEvent>(OnCourseCompleted);
			
			// Load all dishes from disk, then filter by progression/selection
			LoadAllDishesFromDisk();
			LoadAvailableDishesFromProgression();
		}
		protected override void OnShutdown()
		{
			_activeTickets.Clear();
			EventBus.Unsubscribe<DishesServedEvent>(OnDishesServed);
			EventBus.Unsubscribe<CourseCompletedEvent>(OnCourseCompleted);
		}

		private void OnDishesServed(DishesServedEvent e)
		{
			// REFACTORED: Ticket completion now checks TableOrderState.
			// The table owns completion state, not the ticket!
			//
			// When dishes are served:
			// 1. TableManager marks them in TableOrderState (already done in TableManager.OnDishesServed)
			// 2. We check if the table's order is complete
			// 3. If complete, we remove the ticket

			DebugLogger.Log(DebugLogger.Category.TICKET_MANAGER,
				$"OnDishesServed: Table {e.TableNumber} received {e.DishInstanceIds.Count} dishes");

			// Check if any tickets are now complete by querying their table's order state
			for (int i = _activeTickets.Count - 1; i >= 0; i--)
			{
				var ticket = _activeTickets[i];

				// Get the table's order state from TableManager
				if (ticket.AssignedTable == null)
				{
					DebugLogger.LogWarning(DebugLogger.Category.TICKET_MANAGER,
						$"Ticket #{ticket.TicketId} has no assigned table!");
					continue;
				}

				var orderState = tableManager?.GetTableOrderState(ticket.AssignedTable.TableNumber);

				if (orderState == null)
				{
					// Table might have been cleared already
					continue;
				}

				// Check if this table's order is complete
				if (orderState.IsOrderComplete())
				{
					DebugLogger.Log(DebugLogger.Category.TICKET,
						$"Ticket #{ticket.TicketId} COMPLETED - table {ticket.AssignedTable.TableNumber} order fulfilled");

					// Check if any dishes were dead for progression tracking
					bool hadDeadDishes = ticket.AnyDishDead();

					// Publish progression event
					EventBus.Publish(new TicketCompletedEvent
					{
						TicketId = ticket.TicketId,
						TableNumber = ticket.AssignedTable.TableNumber,
						CompletionTime = GameTime.Time - ticket.SpawnTime,
						HadDeadDishes = hadDeadDishes,
						TotalDishes = ticket.TotalDishCount()
					});
					
					// Trigger game feel feedback for ticket completion
					EventBus.Publish(new GameFeelEvent
					{
						EventType = GameFeelEventType.TicketCompleted,
						Timestamp = GameTime.Time,
						Context = ticket
					});

					_activeTickets.RemoveAt(i);
					_currentTicketCount--;

					// Clear the table now that we've detected completion
					if (tableManager != null)
					{
						tableManager.ClearTableByNumber(ticket.AssignedTable.TableNumber);
					}

					// Unregister from scoring manager
					if (scoringManager != null)
					{
						scoringManager.UnregisterTicket(ticket.TicketId);
					}

					// Destroy UI
					if (_ticketUIs.TryGetValue(ticket.TicketId, out var ui))
					{
						if (ui != null) Destroy(ui.gameObject);
						_ticketUIs.Remove(ticket.TicketId);
						DebugLogger.Log(DebugLogger.Category.TICKET, $"Ticket #{ticket.TicketId} UI destroyed.");
					}
				}
			}
		}
		
	private void OnCourseCompleted(CourseCompletedEvent e)
	{
		DebugLogger.Log(DebugLogger.Category.TICKET_MANAGER, $"CourseCompletedEvent received for ticket #{e.TicketId}, course {e.CourseNumber}");
		
		// Find the ticket and mark the course as served
		var ticket = _activeTickets.Find(t => t.TicketId == e.TicketId);
		if (ticket == null)
		{
			DebugLogger.LogWarning(DebugLogger.Category.TICKET_MANAGER, $"Could not find ticket #{e.TicketId} in active tickets!");
			return;
		}
		
		// Find the completed course
		var completedCourse = ticket.Courses.Find(c => c.CourseNumber == e.CourseNumber);
		if (completedCourse != null)
		{
			completedCourse.MarkAsServed(e.CompletionTime);
			DebugLogger.Log(DebugLogger.Category.TICKET_MANAGER, $"Course {e.CourseNumber} on ticket #{e.TicketId} marked as served at {e.CompletionTime:F2}");
		}
		
		// TableManager now handles eating timers and course unlocking
	}
		
		private int _currentTicketCount = 0;

		protected override void Update()
		{
			// Ticket spawning is now handled by TableManager with probability checks
			// This Update method can be used for other time-based logic if needed
		}

		/// <summary>
		/// Called by TableManager when a table is ready for a new party.
		/// Uses spawn probability curve to determine if ticket should spawn.
		/// </summary>
		/// <returns>True if ticket was spawned, false otherwise</returns>
	public bool TrySpawnTicketForTable(TableData table)
	{
		if (availableDishes == null || availableDishes.Count == 0)
		{
			DebugLogger.LogWarning(DebugLogger.Category.TICKET, "No available dishes to spawn.");
			return false;
		}

		// Get current spawn limit based on curve value
		int currentSpawnLimit = GetCurrentSpawnLimit();
		
		if (_currentTicketCount >= currentSpawnLimit)
		{
			DebugLogger.Log(DebugLogger.Category.TICKET, $"Spawn limit reached ({_currentTicketCount}/{currentSpawnLimit}), skipping table {table.TableNumber}");
			return false;
		}
		
		// Check spawn probability based on curve
		float spawnProbability = GetCurrentSpawnProbability();
		float roll = UnityEngine.Random.value;
		
		if (roll > spawnProbability)
		{
			DebugLogger.Log(DebugLogger.Category.TICKET, $"Spawn roll failed: {roll:F2} > {spawnProbability:F2}, skipping table {table.TableNumber}");
			return false;
		}
		
		DebugLogger.Log(DebugLogger.Category.TICKET, $"Spawn roll success: {roll:F2} <= {spawnProbability:F2}, spawning for table {table.TableNumber}");
		
		int ticketId = ++_ticketCounter;
			
			// Create ticket with table recommendations
			var ticket = CreateTicket(ticketId, table);
			ticket.AssignedTable = table;

			// Seat the party at the table and pass the full ticket data
			// TableManager will create the TableOrderState from the ticket's courses
			if (tableManager != null)
			{
				tableManager.SeatPartyAtTable(table.TableNumber, ticketId, ticket);
			}

			_activeTickets.Add(ticket);
			_currentTicketCount++;
			DebugLogger.Log(DebugLogger.Category.TICKET, $"Spawned ticket #{ticketId} with {ticket.TotalDishCount()} dishes across {ticket.Courses.Count} course(s) for table {table.TableNumber} ({table.PartySize}-top)");
			
			// Register with scoring manager
			if (scoringManager != null)
			{
				scoringManager.RegisterTicket(ticket);
			}

			EventBus.Publish(new TicketCreatedEvent
			{
				TicketId = ticketId,
				Timestamp = GameTime.Time
			});
			
			// Trigger game feel feedback for ticket spawn
			EventBus.Publish(new GameFeelEvent
			{
				EventType = GameFeelEventType.TicketSpawned,
				Timestamp = GameTime.Time,
				Context = ticket
			});

			// Instantiate UI
			var ui = Instantiate(ticketUIPrefab, ticketRailParent);
			var ticketUI = ui.GetComponent<TicketUI>();
			ticketUI.Init(ticket, tableManager);
			_ticketUIs[ticketId] = ticketUI;
			
			return true;
		}
		
		/// <summary>
		/// Gets the current spawn probability from the curve based on normalized shift time.
		/// </summary>
		private float GetCurrentSpawnProbability()
		{
			if (shiftTimerManager == null)
			{
				DebugLogger.LogWarning(DebugLogger.Category.TICKET, "ShiftTimerManager reference missing, using default spawn probability 0.5");
				return 0.5f;
			}
			
			// Get normalized time (0-1) from shift timer
			float normalizedTime = shiftTimerManager.GetNormalizedShiftTime();
			float probability = spawnProbabilityCurve.Evaluate(normalizedTime);
			
			return Mathf.Clamp01(probability);
		}

		/// <summary>
		/// Public accessor for spawn probability curve value.
		/// Used by GameManager for audio volume control.
		/// </summary>
		public float GetSpawnProbabilityValue()
		{
			return GetCurrentSpawnProbability();
		}
		
		/// <summary>
		/// Gets the current spawn limit based on curve value.
		/// When curve value is >= 1.0, limit is capped at 5.
		/// </summary>
		private int GetCurrentSpawnLimit()
		{
			float curveValue = GetCurrentSpawnProbability();
			
			if (curveValue >= 1.0f)
			{
				return 5; // Hard cap at 5 when at max pressure
			}
			
			return maxActiveTickets;
		}
		
		/// <summary>
		/// Gets a ticket by ID. Used by TableManager to access ticket data.
		/// </summary>
		public TicketData GetTicket(int ticketId)
		{
			return _activeTickets.Find(t => t.TicketId == ticketId);
		}
		
		/// <summary>
		/// Gets the ticket assigned to a specific table.
		/// </summary>
		public TicketData GetTicketForTable(int tableNumber)
		{
			return _activeTickets.Find(t => t.AssignedTable?.TableNumber == tableNumber);
		}
		
		/// <summary>
		/// Gets the list of available dishes (filtered by player's pre-shift selection).
		/// Used by MenuMenuUI and other systems that need to know what dishes can be spawned.
		/// </summary>
		public List<DishData> GetAvailableDishes()
		{
			return availableDishes ?? new List<DishData>();
		}
		
		/// <summary>
		/// DEPRECATED: This method is no longer needed.
		/// Dishes are now marked as served directly in TableOrderState by TableManager.
		/// </summary>
		[Obsolete("No longer needed - TableManager handles marking dishes as served in TableOrderState")]
		public void MarkDishesServedForTable(int tableNumber, List<DishData> servedDishTypes)
		{
			DebugLogger.LogWarning(DebugLogger.Category.TICKET_MANAGER,
				"MarkDishesServedForTable called but is deprecated - use TableOrderState.MarkDishServed instead");
		}		/// <summary>
		/// Unlocks the next course for a ticket. Called by TableManager after eating time completes.
		/// </summary>
	public void UnlockNextCourseForTicket(int ticketId)
	{
		DebugLogger.Log(DebugLogger.Category.TICKET_MANAGER, $"UnlockNextCourseForTicket called for ticket #{ticketId}");
		
		var ticket = _activeTickets.Find(t => t.TicketId == ticketId);
		if (ticket == null)
		{
			DebugLogger.LogWarning(DebugLogger.Category.TICKET_MANAGER, $"Could not find ticket #{ticketId}");
			return;
		}
		
		DebugLogger.Log(DebugLogger.Category.TICKET_MANAGER, $"Found ticket #{ticketId}, checking for next course...");
		
	var nextCourse = ticket.GetNextCourseToUnlock();
	if (nextCourse != null)
	{
		nextCourse.Unlock();
		DebugLogger.Log(DebugLogger.Category.TICKET_MANAGER, $"✓ Course {nextCourse.CourseNumber} on ticket #{ticketId} is now UNLOCKED!");
		
		// Notify UI to update fire buttons
		if (_ticketUIs.TryGetValue(ticketId, out var ui))
		{
			ui.RefreshCourseButtons();
			DebugLogger.Log(DebugLogger.Category.TICKET_MANAGER, $"Refreshed course buttons for ticket #{ticketId}");
		}
		
		// Publish event for other systems that need to know about course unlocks
		EventBus.Publish(new CourseUnlockedEvent
		{
			TicketId = ticketId,
			TableNumber = ticket.AssignedTable?.TableNumber ?? 0,
			CourseNumber = nextCourse.CourseNumber,
			Timestamp = GameTime.Time
		});
	}
	else
	{
		DebugLogger.Log(DebugLogger.Category.TICKET_MANAGER, $"No more courses to unlock for ticket #{ticketId}");
	}
}	/// <summary>
	/// Fires all available dishes from all active tickets on the board.
	/// This is the "Fire the Board" functionality - fires everything currently available
	/// across all tickets, not future dishes from locked courses.
	/// </summary>
	public void FireTheBoard()
	{
		int ticketsProcessed = 0;
		
		DebugLogger.Log(DebugLogger.Category.TICKET_MANAGER, $"Fire the Board: Processing {_ticketUIs.Count} active ticket UIs");
		
		foreach (var kvp in _ticketUIs)
		{
			var ticketUI = kvp.Value;
			if (ticketUI != null)
			{
				ticketUI.FireAllAvailableDishesOnThisTicket();
				ticketsProcessed++;
			}
		}
		
		DebugLogger.Log(DebugLogger.Category.TICKET_MANAGER, $"Fire the Board completed: Processed {ticketsProcessed} tickets");
	}

	private TicketData CreateTicket(int ticketId, TableData table = null)
		{
			// Create a new ticket
			var ticket = new TicketData(ticketId, GameTime.Time);

			// Use table recommendations if available, otherwise use defaults
			int courseCount;
			int totalDishCount;
			
		if (table != null)
		{
			courseCount = table.GetRecommendedCourseCount();
			totalDishCount = table.GetRecommendedDishCount();
			DebugLogger.Log(DebugLogger.Category.TICKET, $"Using table recommendations: {courseCount} courses, {totalDishCount} dishes for {table.PartySize}-top");
		}
		else
		{
			// Fallback defaults
			courseCount = UnityEngine.Random.Range(1, 2); // 1, 2, or 3 courses
			totalDishCount = UnityEngine.Random.Range(courseCount, 3); // At least 1 per course, max 4 total
			DebugLogger.Log(DebugLogger.Category.TICKET, "Using default ticket generation: {courseCount} courses, {totalDishCount} dishes");
		}
		
		// SAFETY CHECK: If only first-course-only dishes are available, force single course
		// This prevents breaking the rule that first-course-only dishes cannot appear in later courses
		bool hasNonFirstCourseDishes = availableDishes.Any(d => !d.isFirstCourseOnly);
		if (!hasNonFirstCourseDishes && courseCount > 1)
		{
			DebugLogger.LogWarning(DebugLogger.Category.TICKET_MANAGER, 
				$"Only first-course-only dishes available! Forcing single course ticket instead of {courseCount} courses.");
			courseCount = 1;
			totalDishCount = Mathf.Min(totalDishCount, 4); // Cap at 4 dishes for single course
		}
		
	// Distribute dishes across courses
		List<int> dishesPerCourse = DistributeDishesAcrossCourses(courseCount, totalDishCount);
		
		// Track dishes used in previous courses to prevent repeats
		HashSet<DishData> usedDishes = new HashSet<DishData>();
		
		int dishIndex = 0;
	for (int courseNum = 1; courseNum <= courseCount; courseNum++)
	{
		var course = new CourseData(courseNum);
		int dishCountForCourse = dishesPerCourse[courseNum - 1];
		bool isFirstCourse = (courseNum == 1);
		
		// Check how many eligible dishes are available for this course
		int availableUniqueDishes = GetAvailableDishCountForCourse(isFirstCourse, usedDishes);
		
		// If we don't have enough unique dishes, reduce the order size for this course
		if (availableUniqueDishes < dishCountForCourse)
		{
			int originalCount = dishCountForCourse;
			dishCountForCourse = availableUniqueDishes;
			DebugLogger.Log(DebugLogger.Category.TICKET_MANAGER,
				$"Course {courseNum}: Reduced dish count from {originalCount} to {dishCountForCourse} (not enough unique dishes available)");
		}
		
		// Skip this course entirely if no dishes are available
		if (dishCountForCourse == 0)
		{
			DebugLogger.Log(DebugLogger.Category.TICKET_MANAGER,
				$"Course {courseNum}: Skipping course (no unique dishes available)");
			continue;
		}
		
		// Track which dish types are used in THIS course (allows duplicates within the course)
		HashSet<DishData> dishTypesInThisCourse = new HashSet<DishData>();
		
		for (int i = 0; i < dishCountForCourse; i++)
		{
			// Get a dish appropriate for this course that hasn't been used in PREVIOUS courses
			var dishData = GetRandomDishForCourse(isFirstCourse, usedDishes);
			var dishState = new DishState(dishData, GenerateDishInstanceId(ticketId, dishIndex));

			// Track this dish type for this course
			dishTypesInThisCourse.Add(dishData);

			// Add DishState instance (for firing from this ticket)
			course.Dishes.Add(dishState);
			ticket.Dishes.Add(dishState); // Keep backward compatibility

			// REFACTORED: Expectations are no longer created here.
			// TableManager creates them in TableOrderState when SeatPartyAtTable is called.
			// This separates concerns: tickets own "what can be fired", tables own "what's needed".

			EventBus.Publish(new DishAddedToTicketEvent
			{
				TicketId = ticketId,
				DishInstanceId = dishState.DishInstanceId,
				DishData = dishData,
				DishState = dishState,
				Station = dishData.station,
				Timestamp = GameTime.Time
			});

			dishIndex++;
		}
		
		// After the course is complete, mark all dish types from this course as used
		// This prevents them from appearing in future courses
		foreach (var dishType in dishTypesInThisCourse)
		{
			usedDishes.Add(dishType);
		}
		
		ticket.Courses.Add(course);
		DebugLogger.Log(DebugLogger.Category.TICKET,
			$"Course {courseNum} created with {dishCountForCourse} dish(es)");
	}		return ticket;
	}		/// <summary>
		/// Distributes dishes across courses ensuring each course has at least 1 dish.
		/// </summary>
		private List<int> DistributeDishesAcrossCourses(int courseCount, int totalDishes)
		{
			var distribution = new List<int>();
			
			// Start by giving each course 1 dish
			for (int i = 0; i < courseCount; i++)
			{
				distribution.Add(1);
			}
			
			// Distribute remaining dishes randomly
			int remainingDishes = totalDishes - courseCount;
			for (int i = 0; i < remainingDishes; i++)
			{
				int courseIndex = UnityEngine.Random.Range(0, courseCount);
				distribution[courseIndex]++;
			}
			
			return distribution;
		}


	private DishData GetRandomDish()
	{
		return availableDishes[UnityEngine.Random.Range(0, availableDishes.Count)];
	}
	
	/// <summary>
	/// Gets the count of available unique dishes for a given course.
	/// This is used to check if we need to reduce the order size.
	/// </summary>
	private int GetAvailableDishCountForCourse(bool isFirstCourse, HashSet<DishData> usedDishes)
	{
		if (isFirstCourse)
		{
			// First course can have ANY dish (that hasn't been used yet)
			return availableDishes.Count(d => !usedDishes.Contains(d));
		}
		else
		{
			// Non-first courses can ONLY have non-first-course dishes (that haven't been used yet)
			return availableDishes.Count(d => !d.isFirstCourseOnly && !usedDishes.Contains(d));
		}
	}
	
	/// <summary>
	/// Gets a random dish appropriate for the given course.
	/// For first course: can pick from ALL dishes (first-course-only AND non-first-course dishes)
	/// For other courses: can ONLY pick from non-first-course dishes
	/// Also excludes dishes that have been used in previous courses on this ticket.
	/// NOTE: This assumes GetAvailableDishCountForCourse has been checked first to ensure dishes are available.
	/// </summary>
	private DishData GetRandomDishForCourse(bool isFirstCourse, HashSet<DishData> usedDishes)
	{
		List<DishData> eligibleDishes;
		
		if (isFirstCourse)
		{
			// First course can have ANY dish (that hasn't been used yet)
			eligibleDishes = availableDishes.Where(d => !usedDishes.Contains(d)).ToList();
		}
		else
		{
			// Non-first courses can ONLY have non-first-course dishes (that haven't been used yet)
			eligibleDishes = availableDishes
				.Where(d => !d.isFirstCourseOnly && !usedDishes.Contains(d))
				.ToList();
		}
		
		// This should never happen since we check availability before calling this method,
		// but include safety fallback just in case
		if (eligibleDishes.Count == 0)
		{
			DebugLogger.LogError(DebugLogger.Category.TICKET_MANAGER, 
				"CRITICAL: GetRandomDishForCourse called but no eligible dishes available! " +
				"This should have been caught by GetAvailableDishCountForCourse.");
			// Emergency fallback: just return any available dish
			return availableDishes[UnityEngine.Random.Range(0, availableDishes.Count)];
		}
		
		return eligibleDishes[UnityEngine.Random.Range(0, eligibleDishes.Count)];
	}
	
	private int GenerateDishInstanceId(int ticketId, int index)
	{
		// Ensures uniqueness: e.g., ticket 3 → 3000, 3001, etc.
		return ticketId * 1000 + index;
	}		/// <summary>
		/// Load all dishes from disk using ProgressionConfigLoader.
		/// This replaces the manual Inspector configuration.
		/// </summary>
		private void LoadAllDishesFromDisk()
		{
			availableDishes = Expo.Core.Progression.ProgressionConfigLoader.LoadAllDishes();
			DebugLogger.Log(DebugLogger.Category.TICKET_MANAGER, 
				$"Loaded {availableDishes.Count} dishes from disk");
		}
		
		/// <summary>
		/// Filter available dishes based on the pre-shift selection.
		/// This narrows down the disk-loaded dishes to only what the player selected.
		/// </summary>
		private void LoadAvailableDishesFromProgression()
		{
			var selectedDishIds = Expo.UI.PreShiftUI.GetSelectedDishIds();
			if (selectedDishIds.Count > 0)
			{
				// Filter available dishes to only include selected ones
				var selectedDishes = availableDishes.Where(dish => selectedDishIds.Contains(dish.dishName)).ToList();
				if (selectedDishes.Count > 0)
				{
					availableDishes = selectedDishes;
					DebugLogger.Log(DebugLogger.Category.TICKET_MANAGER, 
						$"Loaded {availableDishes.Count} selected dishes from pre-shift: {string.Join(", ", selectedDishIds)}");
				}
				else
				{
					DebugLogger.LogWarning(DebugLogger.Category.TICKET_MANAGER, 
						"No matching dishes found for pre-shift selection, using all available dishes");
				}
			}
			else
			{
				DebugLogger.LogWarning(DebugLogger.Category.TICKET_MANAGER, 
					"No dishes selected in pre-shift, using all available dishes");
			}
		}


        public void ForceSpawn()
        {
            // Force spawn is deprecated - tables now drive spawning
            // If you need to manually spawn, trigger TableManager to check for seating
            DebugLogger.LogWarning(DebugLogger.Category.TICKET, "ForceSpawn is deprecated. Tickets are now spawned by TableManager based on available tables.");
        }
    }
}
