using System;
using GameCore;
using GameCore.Audio;
using GameCore.UI;
using UnityEngine;

public class BgmStateButton : MonoBehaviour, IInitlization
{
    [SerializeField] private CanvasGroup m_canvasGroup;
    [SerializeField] private UIButton m_button;
    [SerializeField] private GameObject m_enableIcon;
    [SerializeField] private GameObject m_disableIcon;

    private bool m_state;
    private void LateUpdate()
    {
        // 設定面板與快捷鍵也能切換音樂，只在狀態改變時更新圖示。
        if (AudioClipManager.instance != null && m_state != AudioClipManager.instance.IsBGMEnabled)
            UpdateUI();
    }
    public void Initlization(Action callBack = null)
    {
        m_button.onClicked = OnButtonClick;

        UpdateUI();
    }

    private void OnButtonClick(UIButton button)
    {
        m_state = !AudioClipManager.instance.IsBGMEnabled;
        if (m_state)
            AudioClipManager.instance.EnableBGM();
        else
            AudioClipManager.instance.DisableBGM();

        UpdateUI();

        AudioClipManager.instance.PlaySFX(AudioClipType.Button_ClickSFX);
    }

    private void UpdateUI()
    {
        m_state = AudioClipManager.instance.IsBGMEnabled;
        m_enableIcon.SetActive(m_state);
        m_disableIcon.SetActive(!m_state);
    }


}
