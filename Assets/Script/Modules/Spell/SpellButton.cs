using System;
using GameCore;
using GameCore.Database;
using GameCore.Event;
using GameCore.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpellButton : MonoBehaviour, IInitlization
{
    [SerializeField] private CanvasGroup m_canvasGroup;
    [SerializeField] private UIButton m_button;
    [SerializeField] private GameObject m_enableIcon;
    [SerializeField] private GameObject m_disableIcon;
    [SerializeField] private FlagReference m_flagReference;
    private EventListener m_eventListener;
    private bool m_spellEnabled = false;
    private bool m_transitionPaused;

    internal void SetTransitionPaused(bool paused)
    {
        m_transitionPaused = paused;
    }
    private bool m_desktopPetCompactLayout = true;
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
        if (m_transitionPaused)
            return;
        SetEnable(!m_spellEnabled);
    }
    private void OnFlagUpdateEvent(FlagUpdateEvent eventData)
    {
        bool unLocked = StorageManager.instance.StorageData.GetFlagStorageValue(m_flagReference.GetKey()) > 0;
        // 精簡版型也必須遵守拼字解鎖旗標，包含圖示與底板。
        m_canvasGroup.alpha = unLocked ? 1f : 0f;
        m_canvasGroup.interactable = unLocked;
        m_canvasGroup.blocksRaycasts = unLocked;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (eventData.flagKey == m_flagReference.GetKey())
            Debug.Log(unLocked ? "[Codex驗證] 拼字解鎖旗標已達成，按鈕可顯示與操作。" : "[Codex驗證] 拼字尚未解鎖，按鈕與底板已隱藏且不可操作。", this);
#endif
    }

    private void UpdateUI()
    {
        if (m_desktopPetCompactLayout)
        {
            m_enableIcon.SetActive(true);
            m_disableIcon.SetActive(false);
            return;
        }

        m_enableIcon.SetActive(m_spellEnabled);
        m_disableIcon.SetActive(!m_spellEnabled);
    }

}
