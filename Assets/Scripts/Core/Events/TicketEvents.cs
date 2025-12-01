namespace Expo.Core.Events
{
	public struct TicketCreatedEvent : IEvent
	{
		public int TicketId;
		public float Timestamp;
	}
	
	public struct TicketOverdueEvent : IEvent
	{
		public int TicketId;
		public int? TableNumber;
		public float OptimalTime;
		public float ActualTime;
		public float Timestamp;
	}
}
