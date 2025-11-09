using System.Collections.Generic;
using Expo.Data;
using Expo.Runtime;

namespace Expo.Core.Events
{
	public struct DishFiredEvent : IEvent
	{
		public int DishInstanceId;
		public DishData DishData;
		public DishState DishState;
		public string Station;
		public float Timestamp;
		public float ExpectedReadyTime;
	}

	// DISH is READY, but not yet picked up. In MVP it immediately goes to the pass.
	public struct DishReadyEvent : IEvent
	{
		public DishData DishData;
		public DishState DishState;
		public int DishInstanceId;
		public string Station;
		public float CookTime;
		public float Timestamp;
	}

	public struct DishOnPassEvent : IEvent
	{
		public DishData DishData;
		public DishState DishState;
		public int DishInstanceId;
		public string Station;
		public float Timestamp;
	}

	public struct DishDiedEvent : IEvent
	{
		public DishData DishData;
		public DishState DishState;
		public int DishInstanceId;
		public string Station;
		public float Timestamp;
	}

	public struct DishAddedToTicketEvent : IEvent
	{
		public int TicketId;
		public DishData DishData;
		public DishState DishState;
		public int DishInstanceId;
		public string Station;
		public float Timestamp;
	}

	// Dish is marked ready by the player, on the pass, ready to be picked up
	public struct DishWalkingEvent : IEvent
	{
		public DishData DishData;
		public DishState DishState;
		public int DishInstanceId;
		public string Station;
		public float Timestamp;
	}
	
	public struct DishesServedEvent : IEvent
	{
		public List<int> DishInstanceIds; // Still needed for PassManager to remove dishes
		public List<DishData> ServedDishTypes; // The types of dishes served (for cross-table matching)
		public int TableNumber; // Which table these dishes were served to
		public float Timestamp;
	}
	
	// Manual assignment of a dish to a specific table
	public struct DishAssignedToTableEvent : IEvent
	{
		public int DishInstanceId;
		public int TableNumber;
		public DishData DishData; // The dish type being assigned
		public float Timestamp;
	}
}
