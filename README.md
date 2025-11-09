# Expo Game

A restaurant kitchen coordination game where you play as the Expeditor (Expo), managing dish timing across multiple cooking stations to deliver complete courses to tables.

## Quick Start

### Opening the Project
1. Open in **Unity 6.2** or newer
2. Open the main game scene: `Assets/Scenes/ExpoScene.unity`
3. Press Play to test gameplay

### Playing the Game
- **Fire dishes** from tickets to send them to cooking stations
- **Watch the Pass** for finished dishes (they have die timers!)
- **Mark dishes Walking** when ready to deliver
- **Click a table** from the always-visible table menu to deliver the food
- Goal: Get all dishes in a course to arrive at the table together

### Game Scenes
- `TitleScene.unity` - Main menu and progression hub
- `PreShiftScene.unity` - Pre-game configuration screen
- `ExpoScene.unity` - Main gameplay (the kitchen/expo station)

## Project Structure

### Unity Hierarchy
```
Assets/
├── Art/           - Sprites, textures, visual assets
├── Data/          - ScriptableObjects (dishes, tables, config)
│   ├── Dishes/    - DishData assets defining all dishes
│   └── Tables/    - TableData configurations
├── Prefabs/       - Reusable UI and game object prefabs
│   └── UI/        - UI component prefabs
├── Scenes/        - Game scenes
├── Scripts/       - All C# code
│   ├── Controllers/  - Scene-level controllers
│   └── Core/         - Core game systems (see below)
└── Settings/      - Unity project settings
```

### Code Architecture (`Assets/Scripts/Core/`)
```
Core/
├── GameManager.cs       - Central game orchestrator
├── CoreManager.cs       - Base class for all managers
├── EventBus.cs          - Publish/subscribe event system
├── GameTime.cs          - Game time and speed control
│
├── Data/                - Data structures
│   ├── DishData.cs      - ScriptableObject: dish properties
│   ├── DishState.cs     - Runtime dish state during gameplay
│   ├── TableData.cs     - Table configuration
│   ├── TicketData.cs    - Runtime ticket/order data
│   ├── CourseData.cs    - Course grouping data
│   └── PlayerSaveData.cs - Progression save data
│
├── Managers/            - Core gameplay systems
│   ├── TableManager.cs  - Table lifecycle and state
│   ├── TicketManager.cs - Ticket generation and course management
│   ├── StationManager.cs - Cooking station logic and dish cooking
│   ├── PassManager.cs   - "The Pass" where cooked dishes wait
│   └── ScoringManager.cs - Score calculation and tracking
│
├── Progression/         - Meta-progression system
│   ├── ProgressionManager.cs - Player XP, unlocks, cook management
│   ├── SaveSystem.cs    - Persistent save/load
│   └── ProgressionConfigLoader.cs
│
├── UI/                  - UI controllers
│   ├── TicketUI.cs      - Displays tickets and fire buttons
│   ├── TableSelectionUI.cs - Always-visible table menu (auto-updates state)
│   ├── GameClockUI.cs   - Time display and speed control
│   └── Progression/     - Progression scene UI components
│
├── Events/              - Event definitions
│   ├── DishEvents.cs    - Dish lifecycle events
│   ├── TicketEvents.cs  - Ticket-related events
│   ├── CourseEvents.cs  - Course completion events
│   └── ProgressionEvents.cs - XP and unlock events
│
└── Debug/
    └── DebugLogger.cs   - Categorized debug logging
```

## Key Systems at a Glance

### Manager System
All core systems inherit from `CoreManager` and follow a consistent lifecycle:
- **Initialize** → **Update** → **Shutdown**
- Managers communicate via the `EventBus` (decoupled)
- Access managers via `FindObjectOfType<ManagerName>()` or singleton patterns

### Event-Driven Architecture
Systems communicate through typed events:
```csharp
// Publishing an event
EventBus.Publish(new DishFiredEvent(dishState, ticketId));

// Subscribing to events
EventBus.Subscribe<DishReadyEvent>(OnDishReady);
```

### Data Flow
1. **DishData** (ScriptableObject) → defines dish properties
2. **DishState** (runtime) → created when dish is fired, tracks cooking/timing
3. **Events** → notify systems of state changes
4. **Managers** → react to events and update game state

## Where Things Live

### In Unity Editor
- **Create new dishes**: Right-click in Project → Create → Expo → Dish Data
- **Edit existing dishes**: `Assets/Data/Dishes/`
- **Configure tables**: `Assets/Data/Tables/`
- **Progression config**: `Assets/Data/progression_config.json`
- **Debug settings**: Select GameManager in hierarchy during Play mode

### In Code
- **Add new dish behaviors**: Modify `StationManager.cs` (cooking logic)
- **Add scoring rules**: Modify `ScoringManager.cs`
- **Create new events**: Add to `Assets/Scripts/Core/Events/`
- **Add UI elements**: `Assets/Scripts/Core/UI/`

## Common Tasks

### Adding a New Dish
1. **In Unity**: Right-click → Create → Expo → Dish Data
2. Fill in dish name, station, cook time (`pickupTime`), and die time
3. Place the asset in `Assets/Data/Dishes/`
4. The dish will automatically appear in gameplay

### Adding New Behavior/Consequences
1. **Create an event** in `Assets/Scripts/Core/Events/` if needed
2. **Modify the relevant manager** to publish the event when conditions are met
3. **Subscribe to the event** in systems that need to react
4. Example: Add a "dish burned" consequence
   - Create `DishBurnedEvent` in `DishEvents.cs`
   - Publish it from `StationManager` when cook time exceeds threshold
   - Subscribe in `ScoringManager` to apply penalty

### Modifying Game Rules
- **Timing rules**: `StationManager.cs` (cooking), `PassManager.cs` (die timers)
- **Scoring rules**: `ScoringManager.cs`
- **Table behavior**: `TableManager.cs`
- **Ticket generation**: `TicketManager.cs`

### Debugging
- Enable/disable debug categories on `GameManager` in the Inspector
- Categories: Table, Pass, Ticket, Station, Score, UI, etc.
- Logs are categorized using `DebugLogger.Log(Category, message)`

## Controls
- **Mouse**: Click UI buttons to fire, mark walking, select tables
- **1/2/3 Keys**: Game speed (1x, 2x, 3x)

## Architecture Notes

### Why Event-Driven?
- Decouples systems (managers don't need direct references)
- Easy to add new behaviors without modifying existing code
- Clear data flow and debugging

### Why ScriptableObjects?
- Dishes are data assets, not code
- Easy to create/modify in Unity without programming
- Supports progression system unlocks

### Manager Lifecycle
- Managers initialize in `Start()`
- `GameManager` updates `GameTime` each frame
- Managers can use internal state machines via `IManagerState`

## Development Tips

1. **Start in ExpoScene** - Most active development happens here
2. **Use the EventBus** - Don't tightly couple systems with direct references
3. **Check DebugLogger** - Enable relevant categories to see what's happening
4. **ScriptableObjects are your friend** - Use them for data, not code
5. **Test with speed controls** - Use 2x/3x speed to test timing quickly

---

**Current Version**: Unity 2022.3 LTS  
**Repository**: violent-coyote/expo-game-unity  
**Branch**: feature/tables
