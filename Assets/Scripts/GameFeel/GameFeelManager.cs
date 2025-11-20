using System.Collections.Generic;
using UnityEngine;
using Expo.Core;
using Expo.GameFeel.Effects;

namespace Expo.GameFeel
{
    /// <summary>
    /// Central manager for all game feel effects.
    /// Subscribes to game events and triggers appropriate visual/audio feedback.
    /// Handles combo detection and intensity scaling.
    /// </summary>
    public class GameFeelManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private GameFeelConfig config;

        [Header("References")]
        [SerializeField] private Camera targetCamera;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        // Effect instances
        private CameraShakeEffect _cameraShake;
        private ScreenFlashEffect _screenFlash;

        // Combo tracking
        private Queue<float> _recentEventTimes = new Queue<float>();
        private int _currentComboCount = 0;

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError("GameFeelManager: No GameFeelConfig assigned!");
                enabled = false;
                return;
            }

            // Auto-find main camera if not assigned
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            InitializeEffects();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<GameFeelEvent>(OnGameFeelEvent);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameFeelEvent>(OnGameFeelEvent);
            StopAllEffects();
        }

        private void InitializeEffects()
        {
            // Initialize camera shake
            _cameraShake = new CameraShakeEffect(targetCamera, config);
            _cameraShake.Initialize();

            // Initialize screen flash
            _screenFlash = new ScreenFlashEffect(config);
            _screenFlash.Initialize();

            if (showDebugLogs)
            {
                Debug.Log("GameFeelManager: Effects initialized");
            }
        }

        /// <summary>
        /// Main event handler for all game feel triggers.
        /// </summary>
        private void OnGameFeelEvent(GameFeelEvent e)
        {
            if (!config.enableGameFeel) return;

            // Update combo tracking
            float intensity = CalculateIntensityWithCombo(e.Timestamp);

            if (showDebugLogs)
            {
                Debug.Log($"GameFeelEvent: {e.EventType} | Combo: {_currentComboCount} | Intensity: {intensity:F2}x");
            }

            // Route to appropriate handler
            switch (e.EventType)
            {
                case GameFeelEventType.Mistake:
                    TriggerMistakeEffects(intensity);
                    break;

                case GameFeelEventType.TicketSpawned:
                    TriggerTicketSpawnedEffects(intensity);
                    break;

                case GameFeelEventType.TicketCompleted:
                    TriggerTicketCompletedEffects(intensity);
                    break;

                case GameFeelEventType.CourseCompleted:
                    // TODO: Implement course completion effects
                    break;

                case GameFeelEventType.PerfectService:
                    // TODO: Implement perfect service effects
                    break;
            }
        }

        /// <summary>
        /// Calculate intensity multiplier based on recent events (combo system).
        /// </summary>
        private float CalculateIntensityWithCombo(float currentTime)
        {
            // Remove events outside the combo window
            while (_recentEventTimes.Count > 0 && 
                   currentTime - _recentEventTimes.Peek() > config.comboTimeWindow)
            {
                _recentEventTimes.Dequeue();
            }

            // Add current event
            _recentEventTimes.Enqueue(currentTime);

            // Calculate combo count (number of events in window)
            _currentComboCount = _recentEventTimes.Count;

            // Calculate intensity: base 1.0 + (combo multiplier * combo count)
            float intensity = 1.0f + (config.comboIntensityMultiplier * (_currentComboCount - 1));
            
            // Clamp to max multiplier
            intensity = Mathf.Min(intensity, config.maxComboMultiplier);

            return intensity;
        }

        /// <summary>
        /// Trigger camera shake + red screen flash for mistakes.
        /// </summary>
        private void TriggerMistakeEffects(float intensity)
        {
            var profile = config.mistakeProfile;

            // Camera shake
            if (profile.enableCameraShake && _cameraShake != null)
            {
                _cameraShake.SetMistakeProfile(profile);
                _cameraShake.Trigger(intensity);
            }

            // Screen flash
            if (profile.enableScreenFlash && _screenFlash != null)
            {
                _screenFlash.SetMistakeProfile(profile);
                _screenFlash.Trigger(intensity);
            }
        }

        /// <summary>
        /// Trigger effects when a new ticket spawns.
        /// </summary>
        private void TriggerTicketSpawnedEffects(float intensity)
        {
            var profile = config.ticketSpawnedProfile;

            if (profile.enableCameraShake && _cameraShake != null)
            {
                _cameraShake.SetTicketProfile(profile);
                _cameraShake.Trigger(intensity);
            }

            if (profile.enableScreenFlash && _screenFlash != null)
            {
                _screenFlash.SetTicketProfile(profile);
                _screenFlash.Trigger(intensity);
            }
        }

        /// <summary>
        /// Trigger effects when a ticket is completed.
        /// </summary>
        private void TriggerTicketCompletedEffects(float intensity)
        {
            var profile = config.ticketCompletedProfile;

            if (profile.enableCameraShake && _cameraShake != null)
            {
                _cameraShake.SetTicketProfile(profile);
                _cameraShake.Trigger(intensity);
            }

            if (profile.enableScreenFlash && _screenFlash != null)
            {
                _screenFlash.SetTicketProfile(profile);
                _screenFlash.Trigger(intensity);
            }
        }

        /// <summary>
        /// Stop all currently active effects.
        /// </summary>
        private void StopAllEffects()
        {
            _cameraShake?.Stop();
            _screenFlash?.Stop();
        }

        /// <summary>
        /// Public API: Manually trigger a game feel event (for testing or direct calls).
        /// </summary>
        public void TriggerEffect(GameFeelEventType eventType, object context = null)
        {
            EventBus.Publish(new GameFeelEvent
            {
                EventType = eventType,
                Timestamp = Time.time,
                Context = context
            });
        }
    }
}
