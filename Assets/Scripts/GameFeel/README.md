# Game Feel Framework

Comprehensive and extensible game feel system for **Expo Game** using DOTween. Provides visual feedback (camera shake, screen flash) for game events with combo support.

---

## 📁 Structure

```
Assets/Scripts/GameFeel/
├── GameFeelManager.cs          # Main coordinator
├── GameFeelConfig.cs           # ScriptableObject configuration
├── GameFeelEvent.cs            # Event definitions
├── Effects/
│   ├── IGameFeelEffect.cs      # Effect interface
│   ├── CameraShakeEffect.cs    # Camera shake implementation
│   └── ScreenFlashEffect.cs    # Screen flash implementation
└── Data/
    └── (Config assets go here)
```

---

## 🚀 Setup

### 1. Create Configuration Asset
1. Right-click in Project window
2. Navigate to `Create > Expo > Game Feel Config`
3. Name it (e.g., `DefaultGameFeelConfig`)
4. Configure parameters in Inspector

### 2. Add GameFeelManager to Scene
1. Create empty GameObject (e.g., "GameFeelManager")
2. Add `GameFeelManager` component
3. Assign your config asset to the `Config` field
4. Optionally assign `Target Camera` (auto-finds Main Camera if not set)

### 3. That's It!
The system automatically subscribes to game events and triggers effects.

---

## ⚙️ Configuration

### Master Controls
- **Enable Game Feel**: Master toggle for all effects

### Mistake Profile (Red Flash + Shake)
- **Camera Shake**:
  - Duration (default: 0.3s)
  - Strength (default: 0.5 units)
  - Vibrato (default: 10 vibrations/sec)
  - Randomness (default: 90°)
- **Screen Flash**:
  - Color (default: Red 30% alpha)
  - Fade In Duration (default: 0.05s)
  - Fade Out Duration (default: 0.25s)

### Ticket Spawned Profile (Subtle Shake)
- Lighter shake, no flash by default
- Customizable per event type

### Ticket Completed Profile
- Similar to spawn, tunable separately

### Combo System
- **Combo Time Window**: Events within this window count as combos (default: 2s)
- **Combo Intensity Multiplier**: How much each combo event adds (default: 0.5x)
- **Max Combo Multiplier**: Cap for intensity scaling (default: 3.0x)

**Example**: 3 mistakes in 2 seconds → 1.0 + 0.5 + 0.5 = 2.0x intensity

---

## 🎮 Supported Events

### Currently Implemented
| Event Type | Trigger | Default Effect |
|------------|---------|----------------|
| **Mistake** | Any mistake (wrong table, dead dish, staggered, premature) | Red flash + medium shake |
| **TicketSpawned** | New ticket arrives | Subtle shake |
| **TicketCompleted** | Ticket fully served | Subtle shake |

### Planned / Extensible
- `CourseCompleted` - When a course is successfully served
- `PerfectService` - When shift ends with no mistakes
- Custom events you define!

---

## 🔧 How It Works

### Event Flow
```
1. Game event occurs (e.g., mistake)
   ↓
2. System publishes GameFeelEvent to EventBus
   ↓
3. GameFeelManager receives event
   ↓
4. Combo system calculates intensity multiplier
   ↓
5. Appropriate effects triggered with intensity
   ↓
6. DOTween animations play (shake + flash)
```

### Combo System
Events within the **combo time window** increase effect intensity:
- First event: 1.0x intensity (normal)
- Second event within window: 1.5x intensity
- Third event: 2.0x intensity
- Capped at 3.0x by default

Perfect for rapid mistakes or ticket spawns!

---

## 📝 Integration Points

### Automatic Integration
The system automatically listens to:
- `ScoringManager` - Publishes `GameFeelEvent` on all mistakes
- `TicketManager` - Publishes on ticket spawn/completion

### Manual Triggering (Optional)
```csharp
// Get reference to GameFeelManager
var gameFeelManager = FindObjectOfType<GameFeelManager>();

// Trigger an effect manually
gameFeelManager.TriggerEffect(GameFeelEventType.Mistake);

// Or publish via EventBus
EventBus.Publish(new GameFeelEvent
{
    EventType = GameFeelEventType.TicketSpawned,
    Timestamp = Time.time,
    Context = null // Optional context data
});
```

---

## 🎨 Customization

### Adding New Effects
1. Create class implementing `IGameFeelEffect`
2. Add instance to `GameFeelManager`
3. Call `Trigger()` in appropriate event handler

### Adding New Event Types
1. Add enum value to `GameFeelEventType`
2. Add profile to `GameFeelConfig` (optional)
3. Add handler case in `GameFeelManager.OnGameFeelEvent()`
4. Publish events from game systems

---

## 🐛 Debug Mode
Enable `Show Debug Logs` in GameFeelManager Inspector to see:
- When effects are triggered
- Combo counts and intensity multipliers
- Event flow

---

## ⚡ Performance Notes
- Effects use DOTween (highly optimized)
- Screen flash creates ONE canvas (reused)
- Camera shake operates on position only (2D optimized)
- Short duration effects (< 0.5s) - minimal impact
- Combo tracking uses queue (O(1) operations)

---

## 🎯 Design Philosophy
1. **Decoupled**: Doesn't pollute existing managers
2. **Extensible**: Easy to add new effects and events
3. **Configurable**: Designers control all parameters
4. **Non-blocking**: Effects play to completion (short duration)
5. **Combo-aware**: Escalates feedback for rapid events

---

## 📦 Dependencies
- **DOTween** (already installed in your project)
- **EventBus** (Expo.Core)
- **Unity UI** (for screen flash canvas)

---

## 🤝 Future Enhancements
- [ ] Particle effect support
- [ ] Audio feedback integration
- [ ] Chromatic aberration / post-processing
- [ ] Haptic feedback (mobile)
- [ ] UI element punch/scale effects
- [ ] Custom shake curves/profiles

---

**Made with 🎮 for Expo Game**
