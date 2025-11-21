using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Expo.GameFeel.Effects
{
    /// <summary>
    /// Handles fullscreen flash overlay effects using UI Canvas.
    /// Creates its own canvas and image for rendering flashes.
    /// </summary>
    public class ScreenFlashEffect : IGameFeelEffect
    {
        private Canvas _canvas;
        private Image _flashImage;
        private Sequence _currentFlash;
        private GameFeelConfig _config;

        // Current effect profile
        private Color _currentColor;
        private float _currentFadeInDuration;
        private float _currentFadeOutDuration;

        public bool IsActive => _currentFlash != null && _currentFlash.IsActive();

        public ScreenFlashEffect(GameFeelConfig config)
        {
            _config = config;
        }

        public void Initialize()
        {
            CreateFlashCanvas();
        }

        /// <summary>
        /// Creates a fullscreen canvas with an image for flash effects.
        /// </summary>
        private void CreateFlashCanvas()
        {
            // Create canvas GameObject
            GameObject canvasObject = new GameObject("GameFeelFlashCanvas");
            Object.DontDestroyOnLoad(canvasObject);

            // Setup Canvas
            _canvas = canvasObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 9999; // Render on top of everything

            // Add CanvasScaler for resolution independence
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Add GraphicRaycaster (required for Canvas)
            canvasObject.AddComponent<GraphicRaycaster>();

            // Create flash image
            GameObject imageObject = new GameObject("FlashImage");
            imageObject.transform.SetParent(canvasObject.transform, false);

            _flashImage = imageObject.AddComponent<Image>();
            _flashImage.color = new Color(1, 0, 0, 0); // Start transparent

            // Make it fullscreen
            RectTransform rect = _flashImage.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            // Disable raycast target (we don't want to block clicks)
            _flashImage.raycastTarget = false;
        }

        /// <summary>
        /// Trigger screen flash with the current profile.
        /// </summary>
        /// <param name="intensity">Multiplier for flash intensity (affects alpha)</param>
        public void Trigger(float intensity = 1.0f)
        {
            if (_flashImage == null || _config == null) return;

            // Stop any existing flash
            Stop();

            // Apply intensity to alpha (but clamp to not exceed original alpha)
            Color flashColor = _currentColor;
            flashColor.a = Mathf.Min(_currentColor.a * intensity, 1f);

            // Create fade in -> fade out sequence
            _currentFlash = DOTween.Sequence();
            
            // Fade in
            _currentFlash.Append(
                _flashImage.DOColor(flashColor, _currentFadeInDuration)
                    .SetEase(Ease.OutQuad)
            );
            
            // Fade out
            _currentFlash.Append(
                _flashImage.DOColor(new Color(flashColor.r, flashColor.g, flashColor.b, 0f), _currentFadeOutDuration)
                    .SetEase(Ease.InQuad)
            );

            // Ensure transparency at the end
            _currentFlash.OnComplete(() =>
            {
                if (_flashImage != null)
                {
                    Color c = _flashImage.color;
                    c.a = 0;
                    _flashImage.color = c;
                }
            });
        }

        /// <summary>
        /// Set the flash profile for mistakes.
        /// </summary>
        public void SetMistakeProfile(MistakeEffectProfile profile)
        {
            _currentColor = profile.flashColor;
            _currentFadeInDuration = profile.flashFadeInDuration;
            _currentFadeOutDuration = profile.flashFadeOutDuration;
        }

        /// <summary>
        /// Set the flash profile for ticket events.
        /// </summary>
        public void SetTicketProfile(TicketEffectProfile profile)
        {
            _currentColor = profile.flashColor;
            _currentFadeInDuration = profile.flashFadeInDuration;
            _currentFadeOutDuration = profile.flashFadeOutDuration;
        }

        public void Stop()
        {
            if (_currentFlash != null && _currentFlash.IsActive())
            {
                _currentFlash.Kill();
            }

            // Reset to transparent
            if (_flashImage != null)
            {
                Color c = _flashImage.color;
                c.a = 0;
                _flashImage.color = c;
            }
        }
    }
}
