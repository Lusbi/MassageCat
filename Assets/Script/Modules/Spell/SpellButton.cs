using System;
using GameCore;
using GameCore.Database;
using GameCore.Event;
using GameCore.UI;
using UnityEngine;

public class SpellButton : MonoBehaviour, IInitlization
{
    [SerializeField] private CanvasGroup m_canvasGroup;
    [SerializeField] private UIButton m_button;
    [SerializeField] private GameObject m_enableIcon;
    [SerializeField] private GameObject m_disableIcon;
    [SerializeField] private FlagReference m_flagReference;
    private EventListener m_eventListener;
    private bool m_spellEnabled = false;
    public void Initlization(Action callBack = null)
    {
        m_button.onClicked = OnButtonClick;
        if (m_eventListener == null)
        {
            m_eventListener = new EventListener();
            m_eventListener.AddListener<FlagUpdateEvent>(OnFlagUpdateEvent, true);
        }

        OnFlagUpdateEvent(new FlagUpdateEvent(m_flagReference.GetKey()));
        UpdateUI();
    }

    internal void SetEnable(bool v)
    {
        m_spellEnabled = v;
        UpdateUI();
        EventManager.instance.Notify(new SpellEvent(m_spellEnabled));
    }

    private void OnButtonClick(UIButton button)
    {
        SetEnable(!m_spellEnabled);
    }
    private void OnFlagUpdateEvent(FlagUpdateEvent eventData)
    {
        bool unLocked = StorageManager.instance.StorageData.GetFlagStorageValue(m_flagReference.GetKey()) > 0;
        m_canvasGroup.alpha = unLocked ? 1 : 0f;
        m_canvasGroup.blocksRaycasts = unLocked;
    }

    private void UpdateUI()
    {
        m_enableIcon.SetActive(m_spellEnabled);
        m_disableIcon.SetActive(!m_spellEnabled);
    }

}
