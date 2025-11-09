using UnityEngine;

namespace Expo.Core
{
    /// <summary>
    /// Abstract base class for all game managers.
    /// Handles lifecycle and state pattern integration.
    /// </summary>
    public abstract class CoreManager : MonoBehaviour
    {
        private IManagerState _currentState;
        private bool _initialized;

        protected virtual void Start()
        {
            Initialize();
        }

        protected virtual void OnDestroy()
        {
            Shutdown();
        }

        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            OnInitialize();
        }

        public void Shutdown()
        {
            if (!_initialized) return;
            ChangeState(null);
            OnShutdown();
            _initialized = false;
        }

        /// <summary>
        /// Transition to a new internal state object.
        /// </summary>
        protected void ChangeState(IManagerState newState)
        {
            _currentState?.Exit();
            _currentState = newState;
            _currentState?.Enter();
        }

        protected virtual void Update()
        {
            _currentState?.Tick(GameTime.DeltaTime);
        }

        // Hooks for derived classes
        protected abstract void OnInitialize();
        protected abstract void OnShutdown();
    }
}
