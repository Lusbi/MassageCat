using System;
using GameCore.Audio;
using GameCore.UI;
using UnityEngine;
using UnityEngine.UI;

public sealed class SceneVisualToggleButton : MonoBehaviour
{
    [SerializeField] private UIButton m_button;
    [SerializeField] private GameObject m_visibleIcon;
    [SerializeField] private GameObject m_hiddenIcon;
    [SerializeField] private Image m_visibleIconImage;
    [SerializeField] private Image m_hiddenIconImage;

    private Action<bool> m_stateChanged;
    private bool m_isVisible = true;

    public void Initlization(Action<bool> stateChanged)
    {
        m_stateChanged = stateChanged;
        m_isVisible = true;

        if (m_button)
            m_button.onClicked = OnButtonClick;

        UpdateUI();
    }

    private void OnButtonClick(UIButton button)
    {
        m_isVisible = !m_isVisible;
        UpdateUI();
        m_stateChanged?.Invoke(m_isVisible);

        if (AudioClipManager.instance != null)
            AudioClipManager.instance.PlaySFX(AudioClipType.Button_ClickSFX);
    }

    private void UpdateUI()
    {
        if (m_visibleIcon)
            m_visibleIcon.SetActive(m_isVisible);

        if (m_hiddenIcon)
            m_hiddenIcon.SetActive(m_isVisible == false);
    }

}
