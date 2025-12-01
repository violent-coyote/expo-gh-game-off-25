using UnityEngine;
using Expo.Core.Debug;

namespace Expo.Controllers
{
    /// <summary>
    /// Persistent music controller that plays background music across all scenes.
    /// This is separate from the GameManager's expo-specific audio.
    /// Uses singleton pattern with DontDestroyOnLoad to persist between scenes.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class MusicController : MonoBehaviour
    {
        private static MusicController _instance;
        
        [Header("Music Settings")]
        [Tooltip("Background music that plays across all scenes")]
        [SerializeField] private AudioClip backgroundMusic;
        
        [Tooltip("Volume for background music (0-1)")]
        [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.5f;
        
        private AudioSource _audioSource;

        /// <summary>
        /// Gets or creates the singleton instance of the MusicController.
        /// Returns null if called before any instance is created.
        /// </summary>
        public static MusicController Instance => _instance;

        private void Awake()
        {
            // Singleton pattern - prevent duplicates
            if (_instance != null && _instance != this)
            {
                DebugLogger.Log(DebugLogger.Category.GENERAL, 
                    "MusicController already exists, destroying duplicate");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAudioSource();
            DebugLogger.Log(DebugLogger.Category.GENERAL, "MusicController initialized and persisted");
        }

        private void Start()
        {
            PlayMusic();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// Sets up the AudioSource component for background music.
        /// </summary>
        private void InitializeAudioSource()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }

            // Configure for looping background music
            _audioSource.playOnAwake = false;
            _audioSource.loop = true;
            _audioSource.volume = musicVolume;
        }

        /// <summary>
        /// Starts playing the background music if not already playing.
        /// </summary>
        public void PlayMusic()
        {
            if (backgroundMusic == null)
            {
                DebugLogger.LogWarning(DebugLogger.Category.GENERAL, 
                    "No background music clip assigned to MusicController");
                return;
            }

            if (_audioSource == null)
            {
                DebugLogger.LogError(DebugLogger.Category.GENERAL, 
                    "AudioSource not initialized in MusicController");
                return;
            }

            // Only play if not already playing the correct clip
            if (_audioSource.clip != backgroundMusic || !_audioSource.isPlaying)
            {
                _audioSource.clip = backgroundMusic;
                _audioSource.Play();
                DebugLogger.Log(DebugLogger.Category.GENERAL, "Background music started playing");
            }
        }

        /// <summary>
        /// Stops playing the background music.
        /// </summary>
        public void StopMusic()
        {
            if (_audioSource != null && _audioSource.isPlaying)
            {
                _audioSource.Stop();
                DebugLogger.Log(DebugLogger.Category.GENERAL, "Background music stopped");
            }
        }

        /// <summary>
        /// Pauses the background music.
        /// </summary>
        public void PauseMusic()
        {
            if (_audioSource != null && _audioSource.isPlaying)
            {
                _audioSource.Pause();
                DebugLogger.Log(DebugLogger.Category.GENERAL, "Background music paused");
            }
        }

        /// <summary>
        /// Resumes the background music if it was paused.
        /// </summary>
        public void ResumeMusic()
        {
            if (_audioSource != null && !_audioSource.isPlaying)
            {
                _audioSource.UnPause();
                DebugLogger.Log(DebugLogger.Category.GENERAL, "Background music resumed");
            }
        }

        /// <summary>
        /// Sets the volume of the background music.
        /// </summary>
        /// <param name="volume">Volume level (0-1)</param>
        public void SetVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            if (_audioSource != null)
            {
                _audioSource.volume = musicVolume;
            }
        }

        /// <summary>
        /// Changes the background music clip and starts playing it.
        /// </summary>
        /// <param name="newClip">The new audio clip to play</param>
        public void ChangeMusic(AudioClip newClip)
        {
            if (newClip == null)
            {
                DebugLogger.LogWarning(DebugLogger.Category.GENERAL, 
                    "Attempted to change music to null clip");
                return;
            }

            backgroundMusic = newClip;
            PlayMusic();
        }
    }
}
