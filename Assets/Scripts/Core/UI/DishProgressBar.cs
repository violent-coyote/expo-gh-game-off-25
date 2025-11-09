using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Expo.UI
{
    /// <summary>
    /// Reusable progress bar component for displaying dish timers.
    /// Can be used for both cooking progress at stations and die timers on the pass.
    /// Uses Unity's Slider component for Unity 6 compatibility.
    /// </summary>
    public class DishProgressBar : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Slider slider; // Unity Slider component
        [SerializeField] private Image fillImage; // The slider's fill image (for color changes)
        [SerializeField] private TextMeshProUGUI timeText; // Optional: displays remaining time
        
        [Header("Color Settings")]
        [SerializeField] private Gradient colorGradient;
        [SerializeField] private bool useGradient = true;
        [SerializeField] private Color defaultColor = Color.green;
        
        private float _maxValue;
        private bool _isInitialized;

        /// <summary>
        /// Initializes the progress bar with a maximum value (cook time or die time).
        /// </summary>
        public void Initialize(float maxValue)
        {
            _maxValue = maxValue;
            _isInitialized = true;
            
            // Configure slider
            if (slider != null)
            {
                slider.minValue = 0f;
                slider.maxValue = 1f;
                slider.interactable = false; // Not interactive, just for display
            }
            
            // Set initial state
            UpdateProgress(0f);
        }

        /// <summary>
        /// Updates the progress bar with the current elapsed time.
        /// </summary>
        /// <param name="currentValue">Current elapsed time</param>
        public void UpdateProgress(float currentValue)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("DishProgressBar not initialized! Call Initialize() first.");
                return;
            }

            // Calculate progress (0 to 1)
            float progress = Mathf.Clamp01(currentValue / _maxValue);
            
            // Update slider value
            if (slider != null)
            {
                slider.value = progress;
            }
            
            // Update color based on progress
            if (fillImage != null)
            {
                if (useGradient && colorGradient != null)
                {
                    fillImage.color = colorGradient.Evaluate(progress);
                }
                else
                {
                    fillImage.color = defaultColor;
                }
            }
            
            // Update text if available
            if (timeText != null)
            {
                float remaining = Mathf.Max(0, _maxValue - currentValue);
                timeText.text = $"{remaining:F1}s";
            }
        }

        /// <summary>
        /// Sets a specific progress value (0-1).
        /// </summary>
        public void SetProgress(float normalizedProgress)
        {
            float progress = Mathf.Clamp01(normalizedProgress);
            
            if (slider != null)
            {
                slider.value = progress;
            }
            
            if (fillImage != null && useGradient && colorGradient != null)
            {
                fillImage.color = colorGradient.Evaluate(progress);
            }
        }

        /// <summary>
        /// Sets the color of the progress bar.
        /// </summary>
        public void SetColor(Color color)
        {
            if (fillImage != null)
            {
                fillImage.color = color;
            }
        }

        /// <summary>
        /// Hides the time text if it's not needed.
        /// </summary>
        public void HideTimeText()
        {
            if (timeText != null)
            {
                timeText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Shows the time text.
        /// </summary>
        public void ShowTimeText()
        {
            if (timeText != null)
            {
                timeText.gameObject.SetActive(true);
            }
        }
    }
}
