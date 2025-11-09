using TMPro;
using UnityEngine;
using Expo.Core;
using Expo.Core.Events;
using Expo.Managers;

namespace Expo.UI
{
    public class GameClockUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI clockText;
        [SerializeField] private bool showSpeed = true;
        [SerializeField] private bool showShiftTime = true; // New option to show simulated shift time
        [SerializeField] private ShiftTimerManager shiftTimerManager;

        // Cache shift time data from events
        private int _currentSimHour = 17; // Default to 5PM
        private int _currentSimMinute = 0;
        private bool _canSpawnTickets = true;

        private void Start()
        {
            Expo.Core.Debug.DebugLogger.Log(Expo.Core.Debug.DebugLogger.Category.TIME, 
                "[GameClockUI] Start - Subscribing to ShiftTimerUpdatedEvent");
            EventBus.Subscribe<ShiftTimerUpdatedEvent>(OnShiftTimerUpdated);
            EventBus.Subscribe<AllTablesServedEvent>(OnAllTablesServed);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<ShiftTimerUpdatedEvent>(OnShiftTimerUpdated);
            EventBus.Unsubscribe<AllTablesServedEvent>(OnAllTablesServed);
        }

        private void Update()
        {
            HandleInput();
            UpdateClock();
        }

        private void HandleInput()
        {
            // Quick test controls: 1x, 2x, 3x
            if (Input.GetKeyDown(KeyCode.Alpha1))
                GameTime.SetSpeed(1f);
            else if (Input.GetKeyDown(KeyCode.Alpha2))
                GameTime.SetSpeed(2f);
            else if (Input.GetKeyDown(KeyCode.Alpha3))
                GameTime.SetSpeed(3f);
            // go up to 40x speed up to 9
            else if (Input.GetKeyDown(KeyCode.Alpha4))
                GameTime.SetSpeed(4f);
            else if (Input.GetKeyDown(KeyCode.Alpha5))
                GameTime.SetSpeed(5f);
            else if (Input.GetKeyDown(KeyCode.Alpha6))
                GameTime.SetSpeed(10f);
            else if (Input.GetKeyDown(KeyCode.Alpha7))
                GameTime.SetSpeed(20f);
            else if (Input.GetKeyDown(KeyCode.Alpha8))
                GameTime.SetSpeed(30f);
            else if (Input.GetKeyDown(KeyCode.Alpha9))
                GameTime.SetSpeed(40f);
        }

        private void UpdateClock()
        {
            float speed = GameTime.Scale;

            if (showShiftTime)
            {
                // Display simulated shift time (5PM-9PM)
                int displayHour = _currentSimHour > 12 ? _currentSimHour - 12 : _currentSimHour;
                string ampm = _currentSimHour >= 12 ? "PM" : "AM";
                string statusIndicator = _canSpawnTickets ? "" : " [LAST CALL]";
                
                // Debug: Log what we're about to display (every 60 frames)
                if (Time.frameCount % 60 == 0)
                {
                    Expo.Core.Debug.DebugLogger.Log(Expo.Core.Debug.DebugLogger.Category.TIME, 
                        $"[UpdateClock] Displaying: {displayHour}:{_currentSimMinute:D2} {ampm} (cached: {_currentSimHour}:{_currentSimMinute})");
                }
                
                clockText.text = showSpeed
                    ? $"{displayHour}:{_currentSimMinute:D2} {ampm}{statusIndicator}  x{speed:0.0}x"
                    : $"{displayHour}:{_currentSimMinute:D2} {ampm}{statusIndicator}";
            }
            else
            {
                // Display raw game time (for debugging)
                float simTime = GameTime.Time;
                int minutes = Mathf.FloorToInt(simTime / 60f);
                int seconds = Mathf.FloorToInt(simTime % 60f);

                clockText.text = showSpeed
                    ? $"TIME {minutes:00}:{seconds:00}  x{speed:0.0}x"
                    : $"{minutes:00}:{seconds:00}";
            }
        }

        private void OnShiftTimerUpdated(ShiftTimerUpdatedEvent e)
        {
            // Debug: This should be called EVERY FRAME but it's not!
            Expo.Core.Debug.DebugLogger.Log(Expo.Core.Debug.DebugLogger.Category.TIME, 
                $"[GameClockUI] ✅ RECEIVED EVENT - Hour: {e.SimulatedHour}, Minute: {e.SimulatedMinute}");
            
            int prevMinute = _currentSimMinute;
            _currentSimHour = e.SimulatedHour;
            _currentSimMinute = e.SimulatedMinute;
            _canSpawnTickets = e.CanSpawnTickets;
        }

        private void OnAllTablesServed(AllTablesServedEvent e)
        {
            // Optionally display shift complete message
            if (clockText != null)
            {
                clockText.text = "SHIFT COMPLETE!";
            }
        }
    }
}
