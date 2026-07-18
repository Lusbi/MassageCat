using System;
using GameCore;
using GameCore.Audio;
using GameCore.UI;
using UnityEngine;

public class AudioStateButton : MonoBehaviour , IInitlization
{
    [SerializeField] private CanvasGroup m_canvasGroup;
    [SerializeField] private UIButton m_button;
    [SerializeField] private GameObject m_enableIcon;
    [SerializeField] private GameObject m_disableIcon;

    private bool m_state;
    public void Initlization(Action callBack = null)
    {
        m_button.onClicked = OnButtonClick;

        UpdateUI();
    }

    private void OnButtonClick(UIButton button)
    {
        m_state = !m_state;
        if (m_state)
            AudioClipManager.instance.EnableSFX();
        else
            AudioClipManager.instance.DisableSFX();

        UpdateUI();

        AudioClipManager.instance.PlaySFX(AudioClipType.Button_ClickSFX);
    }

    private void UpdateUI()
    {
        m_state = AudioClipManager.instance.IsSFXEnabled;
        m_enableIcon.SetActive(m_state);
        m_disableIcon.SetActive(!m_state);
    }

}
