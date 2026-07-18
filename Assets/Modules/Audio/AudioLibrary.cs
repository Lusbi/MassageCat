using System.Collections.Generic;
using UnityEngine;

namespace Modules.Audio
{
    /// <summary>
    /// Audio data library that manages mapping between audio keys and AudioData
    /// </summary>
    [CreateAssetMenu(fileName = "NewAudioLibrary", menuName = "Modules/Audio/Audio Library")]
    public class AudioLibrary : ScriptableObject
    {
        [System.Serializable]
        public class AudioEntry
        {
            public string key;
            public AudioData data;
        }

        [Tooltip("Audio entry list")]
        public List<AudioEntry> entries = new List<AudioEntry>();

        private Dictionary<string, AudioData> _lookup;

        /// <summary>
        /// Initialize dictionary for faster lookup
        /// </summary>
        public void Initialize()
        {
            _lookup = new Dictionary<string, AudioData>();
            foreach (var entry in entries)
            {
                if (!string.IsNullOrEmpty(entry.key) && entry.data != null)
                {
                    if (!_lookup.ContainsKey(entry.key))
                    {
                        _lookup.Add(entry.key, entry.data);
                    }
                    else
                    {
                        Debug.LogWarning($"[AudioLibrary] Duplicate key: {entry.key}");
                    }
                }
            }
        }

        /// <summary>
        /// Get AudioData by key
        /// </summary>
        public AudioData GetAudioData(string key)
        {
            if (_lookup == null)
            {
                Initialize();
            }

            if (_lookup.TryGetValue(key, out AudioData data))
            {
                return data;
            }

            Debug.LogWarning($"[AudioLibrary] Audio key not found: {key}");
            return null;
        }
    }
}
