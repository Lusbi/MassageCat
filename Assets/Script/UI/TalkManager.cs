using Febucci.UI;
using GameCore;
using TMPro;
using UnityEngine;

public class TalkManager : MonoSingleton<TalkManager>
{
    private TextMeshProUGUI m_talkText;
    private TextAnimatorPlayer m_textAnimatorPlayer;

    private float m_remainingDuration;
    private bool m_isPlaying;

    private void OnEnable()
    {
        Clear();
    }

    private void Update()
    {
        if (!m_isPlaying)
        {
            return;
        }

        m_remainingDuration -= Time.deltaTime;
        if (m_remainingDuration <= 0f)
        {
            Clear();
        }
    }

    public void Play(string content, float duration)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            Clear();
            return;
        }

        m_remainingDuration = Mathf.Max(duration, 0f);
        m_isPlaying = true;

        if (m_talkText != null && !m_talkText.gameObject.activeSelf)
        {
            m_talkText.gameObject.SetActive(true);
        }

        m_textAnimatorPlayer ??= GameObject.FindAnyObjectByType<TextAnimatorPlayer>();
        if (m_textAnimatorPlayer != null)
        {
            m_textAnimatorPlayer.ShowText(content);
        }
        
        if (m_talkText != null)
        {
            m_talkText.text = content;
        }

    }

    public void Clear()
    {
        m_isPlaying = false;
        m_remainingDuration = 0f;

        m_textAnimatorPlayer ??= GameObject.FindAnyObjectByType<TextAnimatorPlayer>();
        if (m_textAnimatorPlayer != null)
        {
            m_textAnimatorPlayer.StopShowingText();
            m_textAnimatorPlayer.ShowText(string.Empty);
        }

        if (m_talkText != null)
        {
            m_talkText.text = string.Empty;
            if (m_talkText.gameObject.activeSelf)
            {
                m_talkText.gameObject.SetActive(false);
            }
        }
    }
}