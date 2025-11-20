using UnityEngine;
using DG.Tweening;

namespace Expo.GameFeel.Effects
{
    /// <summary>
    /// Handles camera shake effects using DOTween.
    /// Position-only shake for 2D games.
    /// </summary>
    public class CameraShakeEffect : IGameFeelEffect
    {
        private Camera _camera;
        private Vector3 _originalPosition;
        private Tweener _currentShake;
        private GameFeelConfig _config;
        
        // Current effect profile being used
        private float _currentDuration;
        private float _currentStrength;
        private int _currentVibrato;
        private float _currentRandomness;

        public bool IsActive => _currentShake != null && _currentShake.IsActive();

        public CameraShakeEffect(Camera camera, GameFeelConfig config)
        {
            _camera = camera;
            _config = config;
        }

        public void Initialize()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null)
                {
                    Debug.LogError("CameraShakeEffect: No camera found!");
                    return;
                }
            }

            _originalPosition = _camera.transform.localPosition;
        }

        /// <summary>
        /// Trigger camera shake with the current profile.
        /// </summary>
        /// <param name="intensity">Multiplier for shake intensity (for combo effects)</param>
        public void Trigger(float intensity = 1.0f)
        {
            if (_camera == null || _config == null) return;

            // Stop any existing shake
            Stop();

            // Apply intensity multiplier to strength
            float finalStrength = _currentStrength * intensity;
            float finalDuration = _currentDuration;

            // Perform shake on position only (2D game)
            _currentShake = _camera.transform.DOShakePosition(
                finalDuration,
                finalStrength,
                _currentVibrato,
                _currentRandomness,
                false, // Don't fade out (we handle cleanup)
                true   // Random direction for each shake
            );

            // Reset position when complete
            _currentShake.OnComplete(() =>
            {
                if (_camera != null)
                {
                    _camera.transform.localPosition = _originalPosition;
                }
            });
        }

        /// <summary>
        /// Set the shake profile for mistakes.
        /// </summary>
        public void SetMistakeProfile(MistakeEffectProfile profile)
        {
            _currentDuration = profile.shakeDuration;
            _currentStrength = profile.shakeStrength;
            _currentVibrato = profile.shakeVibrato;
            _currentRandomness = profile.shakeRandomness;
        }

        /// <summary>
        /// Set the shake profile for ticket events.
        /// </summary>
        public void SetTicketProfile(TicketEffectProfile profile)
        {
            _currentDuration = profile.shakeDuration;
            _currentStrength = profile.shakeStrength;
            _currentVibrato = profile.shakeVibrato;
            _currentRandomness = profile.shakeRandomness;
        }

        public void Stop()
        {
            if (_currentShake != null && _currentShake.IsActive())
            {
                _currentShake.Kill();
            }

            // Reset camera position
            if (_camera != null)
            {
                _camera.transform.localPosition = _originalPosition;
            }
        }
    }
}
