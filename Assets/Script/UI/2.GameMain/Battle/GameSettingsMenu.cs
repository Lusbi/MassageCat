using GameCore.Audio;
using GameCore.UI;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// 桌寵與 Prefab 共用的設定選單。
/// </summary>
internal sealed class GameSettingsMenu : MonoBehaviour
{
    private static GameSettingsMenu s_instance;

    [SerializeField] private UIButton m_dismissButton;
    [SerializeField] private UIButton m_settingsButton;
    [SerializeField] private UIButton m_bgmToggleButton;
    [SerializeField] private UIButton m_sfxToggleButton;
    [SerializeField] private UIButton m_quitButton;
    [FormerlySerializedAs("m_autoNextToggleButton")]
    [SerializeField] private UIButton m_saveButton;
    [SerializeField] private GameObject m_settingsPanel;
    [SerializeField] private Image m_settingsPanelBackground;
    [SerializeField] private Image m_settingsIcon;
    [SerializeField] private Image m_bgmStateIcon;
    [SerializeField] private Image m_sfxStateIcon;
    [SerializeField] private Image m_quitIcon;
    [FormerlySerializedAs("m_autoNextStateIcon")]
    [SerializeField] private Image m_saveIcon;

    private bool m_isOpen;
    private bool m_isInitialized;
    private bool m_bgmEnabled;
    private bool m_sfxEnabled;

    private void LateUpdate()
    {
        if (!m_isInitialized || !m_isOpen)
            return;

        AudioClipManager audioManager = AudioClipManager.instance;
        if (audioManager != null
            && (m_bgmEnabled != audioManager.IsBGMEnabled
                || m_sfxEnabled != audioManager.IsSFXEnabled))
        {
            RefreshState();
        }
    }

    private void Awake()
    {
        s_instance = this;
    }

    private void OnDestroy()
    {
        if (s_instance == this)
            s_instance = null;
    }

    internal static void ToggleFromShortcut()
    {
        if (s_instance == null || s_instance.m_isInitialized == false)
            return;

        s_instance.SetSettingsPanelOpen(s_instance.m_isOpen == false);
    }

    internal void Initialize(AutoNextButton autoNextButton)
    {
        if (m_isInitialized)
            return;

        if (HasRequiredReferences() == false)
        {
            Debug.LogError("GameSettingsMenu Prefab 缺少必要 UI 參考。", this);
            return;
        }

        m_dismissButton.onClicked = OnSettingsDismissClicked;
        m_settingsButton.onClicked = OnSettingsButtonClicked;
        m_bgmToggleButton.onClicked = OnBgmToggleClicked;
        m_sfxToggleButton.onClicked = OnSfxToggleClicked;
        m_quitButton.onClicked = OnQuitClicked;
        m_saveButton.onClicked = OnSaveClicked;
        RefreshState();
        SetSettingsPanelOpen(false);
        m_isInitialized = true;
    }

    private void OnSettingsButtonClicked(UIButton button)
    {
        SetSettingsPanelOpen(!m_isOpen);
    }

    private bool HasRequiredReferences()
    {
        return m_dismissButton != null
            && m_settingsButton != null
            && m_bgmToggleButton != null
            && m_sfxToggleButton != null
            && m_quitButton != null
            && m_saveButton != null
            && m_settingsPanel != null
            && m_settingsPanelBackground != null
            && m_settingsIcon != null
            && m_bgmStateIcon != null
            && m_sfxStateIcon != null
            && m_quitIcon != null
            && m_saveIcon != null;
    }

    private void OnSettingsDismissClicked(UIButton button)
    {
        SetSettingsPanelOpen(false);
    }

    private void OnBgmToggleClicked(UIButton button)
    {
        if (AudioClipManager.instance == null)
            return;

        AudioClipManager.instance.ToggleBGM();
        RefreshState();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(m_bgmEnabled ? "[Codex驗證] 音樂已開啟，請確認聲音與開啟圖示一致。" : "[Codex驗證] 音樂已關閉，請確認靜音與關閉圖示一致。", this);
#endif
        AudioClipManager.instance.PlaySFX(AudioClipType.Button_ClickSFX);
    }

    private void OnSfxToggleClicked(UIButton button)
    {
        if (AudioClipManager.instance == null)
            return;

        AudioClipManager.instance.ToggleSFX();
        RefreshState();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(m_sfxEnabled ? "[Codex驗證] 音效已開啟，請確認聲音與開啟圖示一致。" : "[Codex驗證] 音效已關閉，請確認靜音與關閉圖示一致。", this);
#endif
        AudioClipManager.instance.PlaySFX(AudioClipType.Button_ClickSFX);
    }

    private void OnSaveClicked(UIButton button)
    {
        StorageManager.instance.SaveStorageData();
        SetSettingsPanelOpen(false);

        if (AudioClipManager.instance != null)
            AudioClipManager.instance.PlaySFX(AudioClipType.Button_ClickSFX);
    }

    private void OnQuitClicked(UIButton button)
    {
        StorageManager.instance.SaveStorageData();
        Application.Quit();
    }

    private void SetSettingsPanelOpen(bool isOpen)
    {
        m_isOpen = isOpen;
        if (m_dismissButton != null)
            m_dismissButton.gameObject.SetActive(isOpen);
        if (m_settingsPanel != null)
            m_settingsPanel.SetActive(isOpen);
        if (isOpen)
            RefreshState();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(isOpen ? "[Codex驗證] 左側功能列已展開；請確認音樂、音效、存檔、離開直排顯示，再按齒輪可收起。" : "[Codex驗證] 左側功能列已收起。", this);
#endif
    }

    private void RefreshState()
    {
        AudioClipManager audioManager = AudioClipManager.instance;
        if (audioManager == null)
            return;

        m_bgmEnabled = audioManager.IsBGMEnabled;
        m_sfxEnabled = audioManager.IsSFXEnabled;

        // 僅更新開關狀態，不改動 Prefab 的位置、尺寸與層級。
        Sprite bgmSprite = m_bgmEnabled ? MassageCatUiArt.BgmOn : MassageCatUiArt.BgmOff;
        Sprite sfxSprite = m_sfxEnabled ? MassageCatUiArt.SfxOn : MassageCatUiArt.SfxOff;
        if (bgmSprite != null && m_bgmStateIcon.sprite != bgmSprite)
            m_bgmStateIcon.sprite = bgmSprite;
        if (sfxSprite != null && m_sfxStateIcon.sprite != sfxSprite)
            m_sfxStateIcon.sprite = sfxSprite;
    }
}
