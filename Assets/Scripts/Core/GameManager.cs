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
            DebugLogger.SetCategoryEnabled(DebugLogger.Category.UI, enableUILogs);
            DebugLogger.SetCategoryEnabled(DebugLogger.Category.TIME, enableTimeLogs);
            DebugLogger.SetCategoryEnabled(DebugLogger.Category.GENERAL, enableGeneralLogs);

            DebugLogger.Log(DebugLogger.Category.GENERAL, $"Debug Logger Configured: {DebugLogger.GetCategoryStatesString()}");
        }

        private void Update()
        {
            GameTime.Tick();
            OnUpdate();
        }

        private void Shutdown()
        {
            if (!_initialized) return;
            _initialized = false;

            OnShutdown();
            EventBus.Clear();
        }

        /// <summary>
        /// Hooks for derived or future specialized managers.
        /// </summary>
        protected virtual void OnInitialize() { }
        protected virtual void OnUpdate() { }
        protected virtual void OnShutdown() { }
    }
}
