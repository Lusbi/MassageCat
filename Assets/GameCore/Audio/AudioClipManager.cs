using System;
using UnityEngine;
using UnityEngine.Audio;
using Modules.Audio;
using GameCore;

namespace GameCore.Audio
{
    /// <summary>
    /// Unified audio clip manager that provides convenient access to the audio system
    /// Bridges game-specific enums (BgmClipType, AudioClipType) to the unified audio system
    /// </summary>
    public class AudioClipManager : MonoSingleton<AudioClipManager>
    {
        private const string PREF_BGM_ENABLED = "Audio.BGM.Enabled";
        private const string PREF_SFX_ENABLED = "Audio.SFX.Enabled";
        
        private AudioManager _audioManager;
        private bool _bgmEnabled = true;
        private bool _sfxEnabled = true;
        
        [Header("Audio Mixer Groups")]
        [Tooltip("Audio mixer group for BGM")]
        public AudioMixerGroup bgmMixerGroup;
        
        [Tooltip("Audio mixer group for SFX")]
        public AudioMixerGroup sfxMixerGroup;
        
        /// <summary>
        /// Get or set whether BGM playback is enabled
        /// </summary>
        public bool IsBGMEnabled
        {
            get => _bgmEnabled;
            set
            {
                if (_bgmEnabled != value)
                {
                    _bgmEnabled = value;
                    PlayerPrefs.SetInt(PREF_BGM_ENABLED, value ? 1 : 0);
                    PlayerPrefs.Save();
                    
                    // Stop BGM if disabled
                    if (!value)
                    {
                        StopBGM(0.5f);
                    }
                }
            }
        }
        
        /// <summary>
        /// Get or set whether SFX playback is enabled
        /// </summary>
        public bool IsSFXEnabled
        {
            get => _sfxEnabled;
            set
            {
                if (_sfxEnabled != value)
                {
                    _sfxEnabled = value;
                    PlayerPrefs.SetInt(PREF_SFX_ENABLED, value ? 1 : 0);
                    PlayerPrefs.Save();
                }
            }
        }

        private void Awake()
        {
            // Load saved settings
            _bgmEnabled = PlayerPrefs.GetInt(PREF_BGM_ENABLED, 1) == 1;
            _sfxEnabled = PlayerPrefs.GetInt(PREF_SFX_ENABLED, 1) == 1;
            
            // Get reference to the AudioManager instance
            _audioManager = AudioManager.Instance;
            
            if (_audioManager == null)
            {
                Debug.LogError("[AudioClipManager] AudioManager instance not found! Please ensure AudioManager is in the scene.");
            }
        }

        /// <summary>
        /// Play background music with fade effect
        /// </summary>
        /// <param name="bgmType">BGM type to play</param>
        /// <param name="fadeTime">Fade in/out duration</param>
        /// <param name="callback">Callback when fade completes</param>
        public void PlayBGM(BgmClipType bgmType, float fadeTime = 2.0f, Action callback = null)
        {
            // Check if BGM is enabled
            if (!_bgmEnabled)
            {
                callback?.Invoke();
                return;
            }
            
            if (_audioManager == null || SettingsManager.instance?.setting?.bgmSettings == null)
            {
                Debug.LogWarning("[AudioClipManager] Cannot play BGM - AudioManager or Settings not available");
                return;
            }

            // Get AudioClip from settings
            AudioClip clip = SettingsManager.instance.setting.bgmSettings.GetAudioClip(bgmType);
            if (clip == null)
            {
                Debug.LogWarning($"[AudioClipManager] BGM clip not found: {bgmType}");
                return;
            }

            // Create AudioData on-the-fly
            AudioData data = CreateBGMAudioData(clip);
            
            // Register to audio library temporarily for this playback
            string key = $"BGM_{bgmType}";
            if (_audioManager.audioLibrary != null)
            {
                // Add or update entry in the library
                RegisterAudioData(key, data);
                _audioManager.PlayBGM(key, fadeTime, callback);
            }
            else
            {
                Debug.LogWarning("[AudioClipManager] Audio library not available");
            }
        }

        /// <summary>
        /// Stop background music with fade out effect
        /// </summary>
        /// <param name="fadeTime">Fade out duration</param>
        /// <param name="callback">Callback when fade completes</param>
        public void StopBGM(float fadeTime = 0.2f, Action callback = null)
        {
            if (_audioManager == null)
            {
                Debug.LogWarning("[AudioClipManager] Cannot stop BGM - AudioManager not available");
                return;
            }

            _audioManager.StopBGM(fadeTime, callback);
        }

        /// <summary>
        /// Play sound effect
        /// </summary>
        /// <param name="sfxType">SFX type to play</param>
        /// <param name="volume">Volume (0-1)</param>
        public void PlaySFX(AudioClipType sfxType, float volume = 0.5f)
        {
            // Check if SFX is enabled
            if (!_sfxEnabled)
            {
                return;
            }
            
            if (_audioManager == null || SettingsManager.instance?.setting?.audioSettings == null)
            {
                Debug.LogWarning("[AudioClipManager] Cannot play SFX - AudioManager or Settings not available");
                return;
            }

            // Get AudioClip from settings
            AudioClip clip = SettingsManager.instance.setting.audioSettings.GetAudioClip(sfxType);
            if (clip == null)
            {
                Debug.LogWarning($"[AudioClipManager] SFX clip not found: {sfxType}");
                return;
            }

            // Create AudioData on-the-fly
            AudioData data = CreateSFXAudioData(clip, volume);
            
            // Register to audio library temporarily for this playback
            string key = $"SFX_{sfxType}_{Time.frameCount}"; // Unique key per frame
            if (_audioManager.audioLibrary != null)
            {
                RegisterAudioData(key, data);
                _audioManager.PlaySFX(key, 1.0f); // Volume already set in AudioData
            }
            else
            {
                Debug.LogWarning("[AudioClipManager] Audio library not available");
            }
        }

        /// <summary>
        /// Play sound effect at a specific position (for 3D audio)
        /// </summary>
        /// <param name="sfxType">SFX type to play</param>
        /// <param name="position">World position</param>
        /// <param name="volume">Volume (0-1)</param>
        public void PlaySFX(AudioClipType sfxType, Vector3 position, float volume = 0.5f)
        {
            // For now, just play normal SFX (can be enhanced later for 3D audio)
            PlaySFX(sfxType, volume);
        }

        /// <summary>
        /// Set master volume
        /// </summary>
        /// <param name="volume">Volume (0-1)</param>
        public void SetMasterVolume(float volume)
        {
            if (_audioManager != null)
            {
                _audioManager.SetMasterVolume(volume);
            }
        }

        /// <summary>
        /// Set BGM volume
        /// </summary>
        /// <param name="volume">Volume (0-1)</param>
        public void SetBGMVolume(float volume)
        {
            if (_audioManager != null)
            {
                _audioManager.SetBGMVolume(volume);
            }
        }

        /// <summary>
        /// Set SFX volume
        /// </summary>
        /// <param name="volume">Volume (0-1)</param>
        public void SetSFXVolume(float volume)
        {
            if (_audioManager != null)
            {
                _audioManager.SetSFXVolume(volume);
            }
        }

        /// <summary>
        /// Enable BGM playback
        /// </summary>
        public void EnableBGM()
        {
            IsBGMEnabled = true;
        }

        /// <summary>
        /// Disable BGM playback (stops current BGM)
        /// </summary>
        public void DisableBGM()
        {
            IsBGMEnabled = false;
        }

        /// <summary>
        /// Toggle BGM playback on/off
        /// </summary>
        public void ToggleBGM()
        {
            IsBGMEnabled = !IsBGMEnabled;
        }

        /// <summary>
        /// Enable SFX playback
        /// </summary>
        public void EnableSFX()
        {
            IsSFXEnabled = true;
        }

        /// <summary>
        /// Disable SFX playback
        /// </summary>
        public void DisableSFX()
        {
            IsSFXEnabled = false;
        }

        /// <summary>
        /// Toggle SFX playback on/off
        /// </summary>
        public void ToggleSFX()
        {
            IsSFXEnabled = !IsSFXEnabled;
        }

        /// <summary>
        /// Create AudioData for BGM
        /// </summary>
        private AudioData CreateBGMAudioData(AudioClip clip)
        {
            AudioData data = ScriptableObject.CreateInstance<AudioData>();
            data.clip = clip;
            data.outputGroup = bgmMixerGroup;
            data.volume = 0.5f;
            data.pitch = 1f;
            data.loop = true;
            data.spatialBlend = 0f; // 2D audio
            return data;
        }

        /// <summary>
        /// Create AudioData for SFX
        /// </summary>
        private AudioData CreateSFXAudioData(AudioClip clip, float volume)
        {
            AudioData data = ScriptableObject.CreateInstance<AudioData>();
            data.clip = clip;
            data.outputGroup = sfxMixerGroup;
            data.volume = volume;
            data.pitch = 1f;
            data.loop = false;
            data.spatialBlend = 0f; // 2D audio
            return data;
        }

        /// <summary>
        /// Register AudioData to the audio library
        /// </summary>
        private void RegisterAudioData(string key, AudioData data)
        {
            var library = _audioManager.audioLibrary;
            if (library != null)
            {
                // Check if entry already exists
                var existingEntry = library.entries.Find(e => e.key == key);
                if (existingEntry != null)
                {
                    existingEntry.data = data;
                }
                else
                {
                    // Add new entry
                    library.entries.Add(new AudioLibrary.AudioEntry { key = key, data = data });
                }
                
                // Re-initialize to update lookup dictionary
                library.Initialize();
            }
        }
    }
}
