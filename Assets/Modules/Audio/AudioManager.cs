using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Modules.Audio
{
    /// <summary>
    /// Audio system manager, handles BGM and SFX playback
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Settings")]
        [Tooltip("Audio data library")]
        public AudioLibrary audioLibrary;

        [Tooltip("Audio Mixer reference")]
        public AudioMixer audioMixer;

        [Header("Parameters Name")]
        [Tooltip("Master volume parameter name in Mixer")]
        public string masterVolumeParam = "MasterVolume";
        [Tooltip("BGM volume parameter name in Mixer")]
        public string bgmVolumeParam = "BGMVolume";
        [Tooltip("SFX volume parameter name in Mixer")]
        public string sfxVolumeParam = "SFXVolume";

        [Header("Pooling")]
        [Tooltip("Initial SFX AudioSource pool size")]
        public int initialPoolSize = 10;

        private AudioSource _bgmSource;
        private List<AudioSource> _sfxPool;
        private GameObject _sfxRoot;
        private Action _onFadeComplete;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Initialize()
        {
            // Initialize audio library
            if (audioLibrary != null)
            {
                audioLibrary.Initialize();
            }

            // Create BGM source
            _bgmSource = gameObject.AddComponent<AudioSource>();
            _bgmSource.loop = true;
            _bgmSource.playOnAwake = false;

            // Create SFX pool
            _sfxRoot = new GameObject("SFX_Pool");
            _sfxRoot.transform.SetParent(transform);
            _sfxPool = new List<AudioSource>();

            for (int i = 0; i < initialPoolSize; i++)
            {
                CreateNewSFXSource();
            }
        }

        private AudioSource CreateNewSFXSource()
        {
            GameObject go = new GameObject("SFX_Source");
            go.transform.SetParent(_sfxRoot.transform);
            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            _sfxPool.Add(source);
            return source;
        }

        private AudioSource GetAvailableSFXSource()
        {
            foreach (var source in _sfxPool)
            {
                if (!source.isPlaying)
                {
                    return source;
                }
            }
            return CreateNewSFXSource();
        }

        /// <summary>
        /// Play sound effect
        /// </summary>
        /// <param name="key">Audio key</param>
        public void PlaySFX(string key)
        {
            PlaySFX(key, 1.0f);
        }

        /// <summary>
        /// Play sound effect with custom volume
        /// </summary>
        /// <param name="key">Audio key</param>
        /// <param name="volumeScale">Volume scale (0-1)</param>
        public void PlaySFX(string key, float volumeScale)
        {
            if (audioLibrary == null) return;

            AudioData data = audioLibrary.GetAudioData(key);
            if (data != null)
            {
                AudioSource source = GetAvailableSFXSource();
                ApplyAudioData(source, data);
                // Apply volume scale while respecting the data's base volume
                source.PlayOneShot(data.clip, data.volume * volumeScale);
            }
        }

        /// <summary>
        /// Play background music with fade in/out effect
        /// </summary>
        /// <param name="key">Music key</param>
        /// <param name="fadeDuration">Fade in/out duration</param>
        /// <param name="callback">Callback when fade completes</param>
        public void PlayBGM(string key, float fadeDuration = 1.0f, Action callback = null)
        {
            if (audioLibrary == null) return;

            AudioData data = audioLibrary.GetAudioData(key);
            if (data != null)
            {
                if (_bgmSource.clip == data.clip && _bgmSource.isPlaying)
                {
                    // Same music already playing, invoke callback immediately
                    callback?.Invoke();
                    return;
                }

                _onFadeComplete = callback;
                StartCoroutine(FadeSwitchBGM(data, fadeDuration));
            }
        }

        /// <summary>
        /// Stop playing BGM
        /// </summary>
        /// <param name="fadeDuration">Fade out duration</param>
        /// <param name="callback">Callback when fade completes</param>
        public void StopBGM(float fadeDuration = 1.0f, Action callback = null)
        {
            _onFadeComplete = callback;
            StartCoroutine(FadeOutBGM(fadeDuration));
        }

        private void ApplyAudioData(AudioSource source, AudioData data)
        {
            source.outputAudioMixerGroup = data.outputGroup;
            source.volume = data.volume;
            source.pitch = data.pitch;
            source.spatialBlend = data.spatialBlend;
            // Loop setting is invalid for PlayOneShot, mainly used for BGM or special SFX
        }

        private IEnumerator FadeSwitchBGM(AudioData newData, float duration)
        {
            // Fade Out
            if (_bgmSource.isPlaying)
            {
                float startVolume = _bgmSource.volume;
                for (float t = 0; t < duration; t += Time.deltaTime)
                {
                    _bgmSource.volume = Mathf.Lerp(startVolume, 0f, t / duration);
                    yield return null;
                }
                _bgmSource.Stop();
            }

            // Setup New BGM
            _bgmSource.clip = newData.clip;
            _bgmSource.outputAudioMixerGroup = newData.outputGroup;
            _bgmSource.pitch = newData.pitch;
            _bgmSource.loop = newData.loop;
            _bgmSource.spatialBlend = newData.spatialBlend;

            _bgmSource.Play();

            // Fade In
            float targetVolume = newData.volume;
            _bgmSource.volume = 0f;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                _bgmSource.volume = Mathf.Lerp(0f, targetVolume, t / duration);
                yield return null;
            }
            _bgmSource.volume = targetVolume;
            
            // Invoke callback when fade completes
            OnFadeComplete();
        }

        private IEnumerator FadeOutBGM(float duration)
        {
            if (_bgmSource.isPlaying)
            {
                float startVolume = _bgmSource.volume;
                for (float t = 0; t < duration; t += Time.deltaTime)
                {
                    _bgmSource.volume = Mathf.Lerp(startVolume, 0f, t / duration);
                    yield return null;
                }
                _bgmSource.Stop();
            }
            
            // Invoke callback when fade completes
            OnFadeComplete();
        }

        private void OnFadeComplete()
        {
            _onFadeComplete?.Invoke();
            _onFadeComplete = null;
        }

        #region Volume Control

        public void SetMasterVolume(float volume)
        {
            SetMixerVolume(masterVolumeParam, volume);
        }

        public void SetBGMVolume(float volume)
        {
            SetMixerVolume(bgmVolumeParam, volume);
        }

        public void SetSFXVolume(float volume)
        {
            SetMixerVolume(sfxVolumeParam, volume);
        }

        private void SetMixerVolume(string paramName, float volume)
        {
            if (audioMixer != null)
            {
                // Convert 0-1 range to Decibel (-80 to 0)
                float db = volume <= 0.0001f ? -80f : Mathf.Log10(volume) * 20f;
                audioMixer.SetFloat(paramName, db);
            }
        }

        #endregion
    }
}
