using Febucci.UI;
using GameCore;
using TMPro;
using UnityEngine;

public class TalkManager : MonoSingleton<TalkManager>
{
    [Tooltip("由 BattlePanel 初始化時綁定 EnemySpawn/RoleTalkText。")]
    [SerializeField] private TextMeshProUGUI m_talkText;
    [Tooltip("由 BattlePanel 初始化時從 EnemySpawn/RoleTalkText 快取。")]
    [SerializeField] private TextAnimatorPlayer m_textAnimatorPlayer;

    private float m_remainingDuration;
    private bool m_isPlaying;

    private void OnEnable()
    {
        Clear();
    }

    private void Update()
    {
        if (!m_isPlaying)
            return;

        // 對話可讀時間採真實秒數，不受暫停或遊戲時間倍率影響。
        m_remainingDuration -= Time.unscaledDeltaTime;
        if (m_remainingDuration <= 0f)
            Clear();
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
            m_talkText.gameObject.SetActive(true);

        if (m_textAnimatorPlayer != null && m_textAnimatorPlayer.isActiveAndEnabled)
            m_textAnimatorPlayer.ShowText(content);
        else if (m_talkText != null)
            m_talkText.SetText(content);
    }

    internal void BindRoleTalkText(TextMeshProUGUI roleTalkText)
    {
        if (m_talkText != roleTalkText)
            Clear();

        m_talkText = roleTalkText;
        m_textAnimatorPlayer = null;

        if (m_talkText != null)
            m_talkText.TryGetComponent(out m_textAnimatorPlayer);
    }

    public void Clear()
    {
        m_isPlaying = false;
        m_remainingDuration = 0f;

        // 初次載入時對話物件尚未啟用，避免在 TMP 初始化前呼叫 Febucci。
        if (m_talkText == null || !m_talkText.isActiveAndEnabled)
            return;

        if (m_textAnimatorPlayer != null && m_textAnimatorPlayer.isActiveAndEnabled)
        {
            m_textAnimatorPlayer.StopShowingText();
            m_textAnimatorPlayer.ShowText(string.Empty);
        }
        else
        {
            m_talkText.SetText(string.Empty);
        }

        m_talkText.gameObject.SetActive(false);
    }
}
