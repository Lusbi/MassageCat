using UnityEngine;
using GameCore.Utils;
using PrimeTween;

namespace GameCore.Bgm
{
    [System.Obsolete("BgmManager is deprecated. Please use GameCore.Audio.AudioClipManager instead.")]
    public class BgmManager : MonoSingleton<BgmManager>
    {
        private AudioSource m_audioSource;
        private PrimeTween.Tween m_tweener;
        private System.Action m_onFadeComplete;
        private float m_targetVolume = 0.5f;
        private void Awake()
        {
            m_audioSource = gameObject.AddComponentIfNull<AudioSource>();
            m_audioSource.volume = 0.5f;
            m_targetVolume = m_audioSource.volume;
            m_audioSource.loop = true;
        }

        public void PlayBgm(AudioClip audioClip , float fadeInTime = 0 , System.Action callBack = null)
        {
            if (m_audioSource.clip == audioClip)
            {
                return;
            }
            m_onFadeComplete = callBack;
            m_audioSource.clip = audioClip;
            m_audioSource.Play();
            m_audioSource.volume = 0.5f;
            FadeIn(fadeInTime);
        }

        public void PauseBgm()
        {
            m_audioSource.Pause();
        }

        public void StopBgm(float fadeOutTime = 0)
        {
            m_onFadeComplete = ResetAudio;
            FadeOut(fadeOutTime);
        }

        private void FadeIn(float time)
        {
            if (m_tweener.isAlive)
            {
                m_tweener.Stop();
            }

            if (m_audioSource == null)
                return;

            float targetVolume = m_targetVolume;

            if (time <= 0f)
            {
                m_audioSource.volume = targetVolume;
                OnCallBack();
                return;
            }

            m_audioSource.volume = 0f;
            m_tweener = PrimeTween.Tween.AudioVolume(m_audioSource, 0f, targetVolume, time, Ease.Linear);
            m_tweener.OnComplete(() =>
            {
                m_audioSource.volume = targetVolume;
                OnCallBack();
            });
        }

        private void FadeOut(float time)
        {
            if (m_tweener.isAlive)
            {
                m_tweener.Stop();
            }

            if (m_audioSource == null)
            {
                OnCallBack();
                return;
            }

            float startVolume = m_audioSource.volume;

            if (Mathf.Approximately(startVolume, 0f))
            {
                m_audioSource.volume = 0f;
                OnCallBack();
                return;
            }

            if (time <= 0f)
            {
                m_audioSource.volume = 0f;
                OnCallBack();
                return;
            }

            m_tweener = PrimeTween.Tween.AudioVolume(m_audioSource, startVolume, 0f, time, Ease.Linear);
            m_tweener.OnComplete(() =>
            {
                m_audioSource.volume = 0f;
                OnCallBack();
            });
        }

        private void OnUpdateAudioSFX(float value)
        {
            m_targetVolume = value;
            m_audioSource.volume = value;
        }

        private void OnCallBack()
        {
            m_onFadeComplete?.Invoke();
            m_onFadeComplete = null;
        }

        private void ResetAudio()
        {
            m_audioSource.Stop();
            m_audioSource.clip = null;
        }
    }
}