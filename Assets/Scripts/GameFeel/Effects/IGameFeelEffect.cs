namespace Expo.GameFeel
{
    /// <summary>
    /// Interface for all game feel effects.
    /// Effects can be triggered by the GameFeelManager and should handle their own cleanup.
    /// </summary>
    public interface IGameFeelEffect
    {
        /// <summary>
        /// Initialize the effect with necessary dependencies.
        /// Called once during GameFeelManager setup.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Trigger the effect with an optional intensity multiplier for combos.
        /// </summary>
        /// <param name="intensity">Multiplier for effect intensity (1.0 = normal, 2.0 = double, etc.)</param>
        void Trigger(float intensity = 1.0f);

        /// <summary>
        /// Stop any ongoing effects and clean up.
        /// </summary>
        void Stop();

        /// <summary>
        /// Check if the effect is currently active.
        /// </summary>
        bool IsActive { get; }
    }
}
