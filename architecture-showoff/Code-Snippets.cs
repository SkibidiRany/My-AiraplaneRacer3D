
//     *****     *****
//   ********* *********
//  *********************
//  *********************
//   *******************
//     ***************
//       ***********
//         *******
//           ***
//            *

// Note: these codes exist in different files, but I merged them in one big file for easier navigation.


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SkbidiRany
{
    /// <summary>
    /// Speed types for engine sound management
    /// </summary>
    public enum SpeedType 
    { 
        IdleSpeed, 
        NormalSpeed, 
        BoostSpeed 
    }

    /// <summary>
    /// Abstract base class providing common audio functionality for all sound managers.
    /// Handles basic audio operations including playback, volume control, and smooth transitions.
    /// </summary>
    public abstract class BaseSoundManager : MonoBehaviour
    {
        [Header("Audio Configuration")]
        [SerializeField] protected AudioSource _audioSource;
        [SerializeField] protected AudioClip[] _audioClips;
        [SerializeField] [Range(0f, 1f)] protected float _startVolume = 1f;

        protected int _currentClipIndex = 0;

        /// <summary>
        /// Initializes the sound manager with default settings
        /// </summary>
        protected virtual void Start()
        {
            SetVolume(_startVolume);
            SetCurrentSoundIndex(_currentClipIndex);
        }

        /// <summary>
        /// Sets the audio source volume with clamping between 0 and 1
        /// </summary>
        /// <param name="volume">Target volume (0-1)</param>
        public virtual void SetVolume(float volume)
        {
            if (_audioSource != null)
                _audioSource.volume = Mathf.Clamp01(volume);
        }

        /// <summary>
        /// Sets the current audio clip to play from the clips array
        /// </summary>
        /// <param name="index">Index of the audio clip in the array</param>
        public void SetCurrentSoundIndex(int index)
        {
            if (_audioClips == null || _audioClips.Length == 0)
            {
                Debug.LogWarning("No audio clips assigned to BaseSoundManager");
                return;
            }

            if (index >= 0 && index < _audioClips.Length)
            {
                _currentClipIndex = index;
                if (_audioSource != null)
                    _audioSource.clip = _audioClips[_currentClipIndex];
            }
            else
            {
                Debug.LogWarning($"Invalid sound index {index}. Must be between 0 and {_audioClips.Length - 1}");
            }
        }

        /// <summary>
        /// Plays the currently assigned audio clip if not already playing
        /// </summary>
        public virtual void Play()
        {
            if (_audioSource != null && _audioSource.clip != null && !_audioSource.isPlaying)
            {
                _audioSource.Play();
            }
        }

        /// <summary>
        /// Pauses the current audio playback
        /// </summary>
        public virtual void Pause()
        {
            if (_audioSource != null && _audioSource.isPlaying)
                _audioSource.Pause();
        }

        /// <summary>
        /// Stops the current audio playback
        /// </summary>
        public virtual void Stop()
        {
            if (_audioSource != null)
                _audioSource.Stop();
        }

        /// <summary>
        /// Performs a complete fade in-play-fade out sequence for the audio clip
        /// </summary>
        /// <param name="startVolume">Initial volume level</param>
        /// <param name="peakVolume">Peak volume during playback</param>
        /// <param name="endVolume">Final volume level</param>
        /// <param name="fadeDuration">Duration of each fade transition</param>
        public virtual IEnumerator FadeInAndOut(float startVolume, float peakVolume, float endVolume, float fadeDuration)
        {
            if (_audioSource?.clip == null) yield break;

            _audioSource.volume = startVolume;
            Play();
            
            yield return StartCoroutine(FadeAudio(startVolume, peakVolume, fadeDuration));
            yield return new WaitForSeconds(_audioSource.clip.length - (2 * fadeDuration));
            yield return StartCoroutine(FadeAudio(peakVolume, endVolume, fadeDuration));
            
            Stop();
            _audioSource.volume = endVolume;
        }

        /// <summary>
        /// Smoothly transitions audio volume from start to end over specified duration
        /// </summary>
        /// <param name="startVolume">Starting volume level</param>
        /// <param name="endVolume">Target volume level</param>
        /// <param name="duration">Duration of the fade transition</param>
        public virtual IEnumerator FadeAudio(float startVolume, float endVolume, float duration)
        {
            if (_audioSource == null || duration <= 0f) yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = elapsed / duration;
                _audioSource.volume = Mathf.Lerp(startVolume, endVolume, normalizedTime);
                yield return null;
            }
            _audioSource.volume = endVolume;
        }

        /// <summary>
        /// Smoothly transitions volume to target value over specified duration
        /// </summary>
        /// <param name="targetVolume">Target volume level (0-1)</param>
        /// <param name="duration">Duration of the volume transition</param>
        public void LerpVolume(float targetVolume, float duration)
        {
            if (_audioSource != null && duration > 0f)
                StartCoroutine(LerpVolumeCoroutine(targetVolume, duration));
        }

        /// <summary>
        /// Internal coroutine for smooth volume transitions
        /// </summary>
        private IEnumerator LerpVolumeCoroutine(float targetVolume, float duration)
        {
            float startVolume = _audioSource.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = elapsed / duration;
                _audioSource.volume = Mathf.Lerp(startVolume, targetVolume, normalizedTime);
                yield return null;
            }
            _audioSource.volume = Mathf.Clamp01(targetVolume);
        }
    }

    /// <summary>
    /// Manages background music playback with automatic start and resume functionality.
    /// Extends BaseSoundManager to provide music-specific controls.
    /// </summary>
    public class BackgroundMusicManager : BaseSoundManager
    {
        [Header("Music Settings")]
        [SerializeField] private bool _playOnStart = true;
        [SerializeField] private bool _loop = true;

        /// <summary>
        /// Initializes and starts background music if enabled
        /// </summary>
        protected override void Start()
        {
            base.Start();
            
            if (_audioSource != null)
                _audioSource.loop = _loop;
            
            if (_playOnStart)
                Play();
        }

        /// <summary>
        /// Resumes paused background music
        /// </summary>
        public void ResumeMusic()
        {
            if (_audioSource != null)
                _audioSource.UnPause();
        }

        /// <summary>
        /// Toggles music playback state
        /// </summary>
        public void ToggleMusic()
        {
            if (_audioSource == null) return;

            if (_audioSource.isPlaying)
                Pause();
            else
                ResumeMusic();
        }
    }

    /// <summary>
    /// Handles one-shot sound effects playback for game events.
    /// Manages multiple AudioSources for simultaneous sound effect playback.
    /// </summary>
    public class SoundManagerScript : MonoBehaviour
    {
        [Header("Sound Effects")]
        [SerializeField] private AudioSource _boomSound;
        [SerializeField] private AudioSource _confettiSound;
        [SerializeField] private AudioSource _lossSound;

        /// <summary>
        /// Plays the specified audio source if it's not already playing
        /// </summary>
        /// <param name="audioSource">AudioSource to play</param>
        public void PlaySFX(AudioSource audioSource)
        {
            if (audioSource != null && audioSource.clip != null)
            {
                if (!audioSource.isPlaying)
                    audioSource.Play();
            }
        }

        /// <summary>
        /// Plays the boom sound effect
        /// </summary>
        public void PlayBoomSound()
        {
            PlaySFX(_boomSound);
        }

        /// <summary>
        /// Plays the confetti sound effect
        /// </summary>
        public void PlayConfettiSound()
        {
            PlaySFX(_confettiSound);
        }

        /// <summary>
        /// Plays the loss sound effect
        /// </summary>
        public void PlayLossSound()
        {
            PlaySFX(_lossSound);
        }

        /// <summary>
        /// Stops all currently playing sound effects
        /// </summary>
        public void StopAllSFX()
        {
            StopSFX(_boomSound);
            StopSFX(_confettiSound);
            StopSFX(_lossSound);
        }

        /// <summary>
        /// Stops a specific sound effect
        /// </summary>
        private void StopSFX(AudioSource audioSource)
        {
            if (audioSource != null && audioSource.isPlaying)
                audioSource.Stop();
        }
    }

    /// <summary>
    /// Manages engine sound with dynamic volume adjustment based on vehicle speed.
    /// Implements singleton pattern for global access and uses smooth volume transitions.
    /// </summary>
    public class EngineSoundManager : BaseSoundManager
    {
        [Header("Engine Settings")]
        [SerializeField] private float _lerpingDuration = 1f;
        [SerializeField] private SpeedVolume[] _speedVolumesArray;

        private Dictionary<SpeedType, float> _speedVolumesMap;

        /// <summary>
        /// Singleton instance for global access
        /// </summary>
        public static EngineSoundManager Instance { get; private set; }

        /// <summary>
        /// Serializable class for mapping speed types to volume levels
        /// </summary>
        [System.Serializable]
        public class SpeedVolume
        {
            [Tooltip("The speed type this volume setting applies to")]
            public SpeedType speedType;
            
            [Tooltip("Volume level for this speed (0-1)")]
            [Range(0f, 1f)] 
            public float volume;
        }

        /// <summary>
        /// Initializes singleton instance and speed-volume mappings
        /// </summary>
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeSpeedVolumes();
                SetEngineSound(SpeedType.IdleSpeed);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Starts engine sound playback
        /// </summary>
        protected override void Start()
        {
            base.Start();
            
            if (_audioSource != null)
            {
                _audioSource.loop = true;
                Play();
            }
        }

        /// <summary>
        /// Builds the speed-to-volume mapping dictionary from the serialized array
        /// </summary>
        private void InitializeSpeedVolumes()
        {
            _speedVolumesMap = new Dictionary<SpeedType, float>();
            
            if (_speedVolumesArray != null)
            {
                foreach (var speedVolume in _speedVolumesArray)
                {
                    _speedVolumesMap[speedVolume.speedType] = speedVolume.volume;
                }
            }

            // Ensure all speed types have default values if not configured
            if (!_speedVolumesMap.ContainsKey(SpeedType.IdleSpeed))
                _speedVolumesMap[SpeedType.IdleSpeed] = 0.3f;
            
            if (!_speedVolumesMap.ContainsKey(SpeedType.NormalSpeed))
                _speedVolumesMap[SpeedType.NormalSpeed] = 0.6f;
            
            if (!_speedVolumesMap.ContainsKey(SpeedType.BoostSpeed))
                _speedVolumesMap[SpeedType.BoostSpeed] = 1.0f;
        }

        /// <summary>
        /// Adjusts engine sound volume based on the specified speed type
        /// </summary>
        /// <param name="speedType">The current speed type of the vehicle</param>
        public void SetEngineSound(SpeedType speedType)
        {
            if (_speedVolumesMap.TryGetValue(speedType, out float targetVolume))
            {
                LerpVolume(targetVolume, _lerpingDuration);
            }
            else
            {
                Debug.LogWarning($"SpeedType {speedType} not found in EngineSoundManager. Using default volume.");
                LerpVolume(0.5f, _lerpingDuration);
            }
        }

        /// <summary>
        /// Gets the configured volume level for a specific speed type
        /// </summary>
        /// <param name="speedType">The speed type to query</param>
        /// <returns>Volume level (0-1) for the specified speed type</returns>
        public float GetVolumeForSpeed(SpeedType speedType)
        {
            return _speedVolumesMap.TryGetValue(speedType, out float volume) ? volume : 0.5f;
        }

        /// <summary>
        /// Updates the volume mapping for a specific speed type at runtime
        /// </summary>
        /// <param name="speedType">Speed type to update</param>
        /// <param name="volume">New volume level (0-1)</param>
        public void UpdateSpeedVolume(SpeedType speedType, float volume)
        {
            _speedVolumesMap[speedType] = Mathf.Clamp01(volume);
        }
    }
}