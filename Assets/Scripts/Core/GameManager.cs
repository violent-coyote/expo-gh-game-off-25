using UnityEngine;
using Expo.Core.Debug;

namespace Expo.Core
{
    /// <summary>
    /// Central orchestrator for the game. 
    /// Initializes systems, processes command queue, and provides a global access point.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Audio Settings")]
        [Tooltip("Background audio to play during the expo scene")]
        [SerializeField] private AudioClip expoBackgroundaudio;
        
        [Tooltip("Should the audio volume be controlled by the spawn probability curve?")]
        [SerializeField] private bool linkVolumeToSpawnCurve = false;
        
        [Tooltip("Base volume for background audio (0-1). Used as max volume when linked to spawn curve.")]
        [SerializeField] [Range(0f, 1f)] private float audioVolume = 0.7f;
        
        [Tooltip("Minimum volume when linked to spawn curve (0-1)")]
        [SerializeField] [Range(0f, 1f)] private float minVolumeWhenLinked = 0.3f;

        private AudioSource _audioSource;
        private Expo.Managers.ShiftTimerManager _shiftTimerManager;
        private Expo.Core.Managers.TicketManager _ticketManager;

        [Header("Debug Logger Settings")]
        [SerializeField] private bool enableTableLogs = true;
        [SerializeField] private bool enableTableManagerLogs = true;
        [SerializeField] private bool enableTableDebugLogs = true;
        [SerializeField] private bool enableTableUILogs = true;
        [SerializeField] private bool enableExpoLogs = true;
        [SerializeField] private bool enablePassLogs = true;
        [SerializeField] private bool enableTicketLogs = true;
        [SerializeField] private bool enableTicketUILogs = true;
        [SerializeField] private bool enableTicketManagerLogs = true;
        [SerializeField] private bool enableCourseLogs = true;
        [SerializeField] private bool enableStationLogs = true;
        [SerializeField] private bool enableScoreLogs = true;
        [SerializeField] private bool enableMistakeLogs = true;
        [SerializeField] private bool enableUILogs = true;
        [SerializeField] private bool enableTimeLogs = true;
        [SerializeField] private bool enableGeneralLogs = true;

        private bool _initialized;

        private void Awake()
        {
            // Singleton enforcement
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Setup audio source
            SetupAudioSource();

            // Initialize once
            Initialize();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            Shutdown();
        }

        private void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            // Configure debug logger based on inspector settings
            ConfigureDebugLogger();

            EventBus.Clear();

            OnInitialize();

            // Start background audio for expo scene
            StartBackgroundAudio();
        }

        /// <summary>
        /// Apply debug logger settings from inspector
        /// </summary>
        private void ConfigureDebugLogger()
        {
            DebugLogger.SetCategoryEnabled(DebugLogger.Category.TABLE, enableTableLogs);
            DebugLogger.SetCategoryEnabled(DebugLogger.Category.TABLE_MANAGER, enableTableManagerLogs);
            DebugLogger.SetCategoryEnabled(DebugLogger.Category.TABLE_DEBUG, enableTableDebugLogs);
            DebugLogger.SetCategoryEnabled(DebugLogger.Category.TABLE_UI, enableTableUILogs);
            DebugLogger.SetCategoryEnabled(DebugLogger.Category.EXPO, enableExpoLogs);
            DebugLogger.SetCategoryEnabled(DebugLogger.Category.PASS, enablePassLogs);
            DebugLogger.SetCategoryEnabled(DebugLogger.Category.TICKET, enableTicketLogs);
            DebugLogger.SetCategoryEnabled(DebugLogger.Category.TICKET_UI, enableTicketUILogs);
            DebugLogger.SetCategoryEnabled(DebugLogger.Category.TICKET_MANAGER, enableTicketManagerLogs);
            DebugLogger.SetCategoryEnabled(DebugLogger.Category.COURSE, enableCourseLogs);
            DebugLogger.SetCategoryEnabled(DebugLogger.Category.STATION, enableStationLogs);
            DebugLogger.SetCategoryEnabled(DebugLogger.Category.SCORE, enableScoreLogs);
            DebugLogger.SetCategoryEnabled(DebugLogger.Category.MISTAKE, enableMistakeLogs);
            DebugLogger.SetCategoryEnabled(DebugLogger.Category.UI, enableUILogs);
            DebugLogger.SetCategoryEnabled(DebugLogger.Category.TIME, enableTimeLogs);
            DebugLogger.SetCategoryEnabled(DebugLogger.Category.GENERAL, enableGeneralLogs);

            DebugLogger.Log(DebugLogger.Category.GENERAL, $"Debug Logger Configured: {DebugLogger.GetCategoryStatesString()}");
        }

        private void Update()
        {
            GameTime.Tick();
            UpdateAudioVolumeBasedOnSpawnCurve();
            OnUpdate();
        }

        private void Shutdown()
        {
            if (!_initialized) return;
            _initialized = false;

            // Stop background audio when leaving expo scene
            StopBackgroundAudio();

            OnShutdown();
            EventBus.Clear();
        }

        /// <summary>
        /// Hooks for derived or future specialized managers.
        /// </summary>
        protected virtual void OnInitialize() { }
        protected virtual void OnUpdate() { }
        protected virtual void OnShutdown() { }

        /// <summary>
        /// Sets up the AudioSource component for background audio.
        /// </summary>
        private void SetupAudioSource()
        {
            // Get or add AudioSource component
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }

            // Configure AudioSource for background audio
            _audioSource.playOnAwake = false;
            _audioSource.loop = true;
            _audioSource.volume = audioVolume;
        }

        /// <summary>
        /// Starts playing the background audio if available.
        /// Called when entering the expo scene.
        /// </summary>
        public void StartBackgroundAudio()
        {
            if (expoBackgroundaudio == null)
            {
                DebugLogger.LogWarning(DebugLogger.Category.GENERAL, "No background audio clip assigned to GameManager");
                return;
            }

            if (_audioSource == null)
            {
                DebugLogger.LogError(DebugLogger.Category.GENERAL, "AudioSource not initialized in GameManager");
                return;
            }

            _audioSource.clip = expoBackgroundaudio;
            _audioSource.Play();
            
            DebugLogger.Log(DebugLogger.Category.GENERAL, "Background audio started");
        }

        /// <summary>
        /// Stops playing the background audio.
        /// Called when leaving the expo scene.
        /// </summary>
        public void StopBackgroundAudio()
        {
            if (_audioSource != null && _audioSource.isPlaying)
            {
                _audioSource.Stop();
                DebugLogger.Log(DebugLogger.Category.GENERAL, "Background audio stopped");
            }
        }

        /// <summary>
        /// Updates the audio volume based on spawn probability curve if enabled.
        /// Called during Update when linkVolumeToSpawnCurve is true.
        /// </summary>
        private void UpdateAudioVolumeBasedOnSpawnCurve()
        {
            if (!linkVolumeToSpawnCurve || _audioSource == null || !_audioSource.isPlaying)
                return;

            // Try to find managers if not cached
            if (_ticketManager == null)
            {
                _ticketManager = FindFirstObjectByType<Expo.Core.Managers.TicketManager>();
            }

            if (_shiftTimerManager == null)
            {
                _shiftTimerManager = FindFirstObjectByType<Expo.Managers.ShiftTimerManager>();
            }

            // If we can't find the managers, use default volume
            if (_ticketManager == null)
            {
                _audioSource.volume = audioVolume;
                return;
            }

            // Get spawn probability from TicketManager's curve
            float spawnProbability = _ticketManager.GetSpawnProbabilityValue();
            
            // Map spawn probability to volume range
            float targetVolume = Mathf.Lerp(minVolumeWhenLinked, audioVolume, spawnProbability);
            
            // Smooth transition
            _audioSource.volume = Mathf.Lerp(_audioSource.volume, targetVolume, Time.deltaTime * 2f);
        }
    }
}
