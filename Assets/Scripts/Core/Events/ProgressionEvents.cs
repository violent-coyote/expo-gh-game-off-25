namespace Expo.Core.Events
{
    /// <summary>
    /// Event fired when player levels up
    /// </summary>
    public struct PlayerLevelUpEvent : IEvent
    {
        public int NewLevel;
        public int TotalExperience;
    }
    
    /// <summary>
    /// Event fired when a dish is unlocked
    /// </summary>
    public struct DishUnlockedEvent : IEvent
    {
        public string DishId;
    }
    
    /// <summary>
    /// Event fired when a cook is unlocked
    /// </summary>
    public struct CookUnlockedEvent : IEvent
    {
        public string CookId;
    }
    
    /// <summary>
    /// Event fired when table count is increased
    /// </summary>
    public struct TableCountIncreasedEvent : IEvent
    {
        public int NewTableCount;
    }
    
    /// <summary>
    /// Event fired when a ticket is completed
    /// </summary>
    public struct TicketCompletedEvent : IEvent
    {
        public int TicketId;
        public int TableNumber;
        public float CompletionTime;
        public bool HadDeadDishes;
        public int TotalDishes;
    }
    
    /// <summary>
    /// Event fired when a dish is served (for progression tracking)
    /// </summary>
    public struct DishServedEvent : IEvent
    {
        public string DishId;
        public int DishInstanceId;
        public int TableNumber;
        public float ServeTime;
    }
}