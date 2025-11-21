using System;
using UnityEngine;

namespace Expo.GameFeel
{
    /// <summary>
    /// Configuration for game feel effects.
    /// Create an instance via Assets > Create > Expo > Game Feel Config
    /// </summary>
    [CreateAssetMenu(fileName = "GameFeelConfig", menuName = "Expo/Game Feel Config", order = 1)]
    public class GameFeelConfig : ScriptableObject
    {
        [Header("Master Controls")]
        [Tooltip("Master switch to enable/disable all game feel effects")]
        public bool enableGameFeel = true;

        [Header("Effect Profiles")]
        public MistakeEffectProfile mistakeProfile;
        public TicketEffectProfile ticketSpawnedProfile;
        public TicketEffectProfile ticketCompletedProfile;

        [Header("Combo System")]
        [Tooltip("Time window (seconds) for events to count as a combo")]
        public float comboTimeWindow = 2.0f;
        
        [Tooltip("Intensity multiplier per combo event (stacks additively)")]
        public float comboIntensityMultiplier = 0.5f;
        
        [Tooltip("Maximum combo multiplier (e.g., 3.0 = 3x intensity max)")]
        public float maxComboMultiplier = 3.0f;
    }

    /// <summary>
    /// Effect profile for mistakes (red flash + camera shake)
    /// </summary>
    [Serializable]
    public class MistakeEffectProfile
    {
        [Header("Enable/Disable")]
        public bool enableCameraShake = true;
        public bool enableScreenFlash = true;

        [Header("Camera Shake")]
        [Tooltip("Duration of shake in seconds")]
        public float shakeDuration = 0.3f;
        
        [Tooltip("Shake strength (distance in world units)")]
        public float shakeStrength = 0.5f;
        
        [Tooltip("How many vibrations per second")]
        public int shakeVibrato = 10;
        
        [Tooltip("Randomness of shake (0-180 degrees)")]
        public float shakeRandomness = 90f;

        [Header("Screen Flash")]
        [Tooltip("Color of the screen flash")]
        public Color flashColor = new Color(1f, 0f, 0f, 0.3f); // Red with 30% alpha
        
        [Tooltip("Duration of flash fade in")]
        public float flashFadeInDuration = 0.05f;
        
        [Tooltip("Duration of flash fade out")]
        public float flashFadeOutDuration = 0.25f;
    }

    /// <summary>
    /// Effect profile for ticket events (typically less intense)
    /// </summary>
    [Serializable]
    public class TicketEffectProfile
    {
        [Header("Enable/Disable")]
        public bool enableCameraShake = true;
        public bool enableScreenFlash = false;

        [Header("Camera Shake")]
        public float shakeDuration = 0.2f;
        public float shakeStrength = 0.2f;
        public int shakeVibrato = 8;
        public float shakeRandomness = 90f;

        [Header("Screen Flash")]
        public Color flashColor = new Color(1f, 1f, 1f, 0.2f); // White flash
        public float flashFadeInDuration = 0.05f;
        public float flashFadeOutDuration = 0.2f;
    }
}
