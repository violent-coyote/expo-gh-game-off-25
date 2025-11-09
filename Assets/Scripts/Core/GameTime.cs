using UnityEngine;

namespace Expo.Core
{
    /// <summary>
    /// Centralized time controller for global speed scaling and pause management.
    /// Replaces direct usage of Time.deltaTime in all systems.
    /// </summary>
    public static class GameTime
    {
        private static float _scale = 1f;
        private static bool _paused;
        private static float _time = 0f;

        /// <summary>
        /// Returns scaled delta time or zero if paused.
        /// </summary>
        public static float DeltaTime
            => _paused ? 0f : UnityEngine.Time.deltaTime * _scale;

        /// <summary>
        /// Returns scaled fixed delta time (for physics or deterministic updates).
        /// </summary>
        public static float FixedDeltaTime
            => _paused ? 0f : UnityEngine.Time.fixedDeltaTime * _scale;

        /// <summary>
        /// Returns the scaled game time (accumulated DeltaTime).
        /// Use this instead of Time.time for gameplay logic.
        /// Call Tick() once per frame to accumulate time properly.
        /// </summary>
        public static float Time => _time;

        /// <summary>
        /// Accumulates time for this frame. Call this once per frame from GameManager.
        /// </summary>
        public static void Tick()
        {
            _time += DeltaTime;
        }

        /// <summary>
        /// Adjusts global time scale. Default = 1.0f.
        /// </summary>
        public static void SetSpeed(float scale)
        {
            _scale = Mathf.Max(0f, scale);
        }

        /// <summary>
        /// Pauses or resumes all game time progression.
        /// </summary>
        public static void SetPaused(bool paused)
        {
            _paused = paused;
        }

        public static bool IsPaused => _paused;
        public static float Scale => _scale;
    }
}
