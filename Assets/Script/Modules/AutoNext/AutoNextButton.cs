using System;
using GameCore;
using GameCore.Database;
using GameCore.Event;
using GameCore.UI;
using UnityEngine;

public class AutoNextButton : MonoBehaviour, IInitlization
{
    [SerializeField] private CanvasGroup m_canvasGroup;
    [SerializeField] private UIButton m_button;
    [SerializeField] private GameObject m_enableIcon;
    [SerializeField] private GameObject m_disableIcon;

    [SerializeField] private FlagReference m_flagReference;
    private EventListener m_eventListener;
    private bool m_isUnlocked;
    public void Initlization(Action callBack = null)
    {
        m_button.onClicked = OnButtonClick;
        if (m_eventListener == null)
        {
            m_eventListener = new EventListener();
            m_eventListener.AddListener<FlagUpdateEvent>(OnFlagUpdateEvent, true);
        }

        OnFlagUpdateEvent(new FlagUpdateEvent(m_flagReference.GetKey()));
        m_isAutoNext = false;
        UpdateUI();
    }

    private void OnButtonClick(UIButton button)
    {
        TryToggle();
    }
    private void OnFlagUpdateEvent(FlagUpdateEvent eventData)
    {
        m_isUnlocked = StorageManager.instance.StorageData.GetFlagStorageValue(m_flagReference.GetKey()) > 0;
        m_canvasGroup.alpha = m_isUnlocked ? 1 : 0f;
        m_canvasGroup.blocksRaycasts = m_isUnlocked;

    }

    private void UpdateUI()
    {
        m_enableIcon.SetActive(m_isAutoNext);
        m_disableIcon.SetActive(!m_isAutoNext);
    }

    private bool m_isAutoNext;
    public bool isAutoNext => m_isAutoNext;
    internal bool IsUnlocked => m_isUnlocked;

    internal bool TryToggle()
    {
        if (m_isUnlocked == false)
            return false;

        m_isAutoNext = !m_isAutoNext;
        UpdateUI();
        return true;
    }

    internal bool UsesUnlockFlag(string flagKey)
    {
        return m_flagReference != null
            && string.Equals(m_flagReference.GetKey(), flagKey, StringComparison.Ordinal);
    }

    public void Stop()
    {
        if (m_isAutoNext)
        {
            m_isAutoNext = false;
            UpdateUI();
        }
    }
}
