using UnityEngine;
using Modules.Audio;

namespace Modules.Audio.Tests
{
    /// <summary>
    /// 音效系統測試腳本
    /// </summary>
    public class AudioTest : MonoBehaviour
    {
        [Header("Test Keys")]
        public string bgmKey = "BGM_Test";
        public string sfxKey = "SFX_Test";

        [Header("Volume Control")]
        [Range(0f, 1f)]
        public float masterVolume = 1f;
        [Range(0f, 1f)]
        public float bgmVolume = 1f;
        [Range(0f, 1f)]
        public float sfxVolume = 1f;

        private void Update()
        {
            // 測試播放 BGM
            if (Input.GetKeyDown(KeyCode.Q))
            {
                Debug.Log($"[AudioTest] Play BGM: {bgmKey}");
                AudioManager.Instance.PlayBGM(bgmKey);
            }

            // 測試停止 BGM
            if (Input.GetKeyDown(KeyCode.W))
            {
                Debug.Log("[AudioTest] Stop BGM");
                AudioManager.Instance.StopBGM();
            }

            // 測試播放 SFX
            if (Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log($"[AudioTest] Play SFX: {sfxKey}");
                AudioManager.Instance.PlaySFX(sfxKey);
            }
        }

        // 測試音量調整 (可透過 Inspector 拉動 Slider 測試)
        public void OnMasterVolumeChanged(float value)
        {
            AudioManager.Instance.SetMasterVolume(value);
        }

        public void OnBGMVolumeChanged(float value)
        {
            AudioManager.Instance.SetBGMVolume(value);
        }

        public void OnSFXVolumeChanged(float value)
        {
            AudioManager.Instance.SetSFXVolume(value);
        }
        
        private void OnValidate()
        {
            if (Application.isPlaying && AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMasterVolume(masterVolume);
                AudioManager.Instance.SetBGMVolume(bgmVolume);
                AudioManager.Instance.SetSFXVolume(sfxVolume);
            }
        }
    }
}
