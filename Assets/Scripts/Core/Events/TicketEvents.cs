namespace Expo.Core.Events
{
	public struct TicketCreatedEvent : IEvent
	{
		public int TicketId;
		public float Timestamp;
	}
}
