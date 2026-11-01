using UnityEngine;
using UnityEngine.Audio;

namespace Modules.Audio
{
    /// <summary>
    /// Defines the data structure for a single audio clip
    /// </summary>
    [CreateAssetMenu(fileName = "NewAudioData", menuName = "Modules/Audio/Audio Data")]
    public class AudioData : ScriptableObject
    {
        [Tooltip("Audio clip file")]
        public AudioClip clip;

        [Tooltip("Audio mixer group (Output)")]
        public AudioMixerGroup outputGroup;

        [Range(0f, 1f)]
        [Tooltip("Volume")]
        public float volume = 1f;

        [Range(0.1f, 3f)]
        [Tooltip("Pitch")]
        public float pitch = 1f;

        [Tooltip("Loop playback")]
        public bool loop = false;

        [Tooltip("Spatial blend (0 = 2D, 1 = 3D)")]
        [Range(0f, 1f)]
        public float spatialBlend = 0f;
    }
}
