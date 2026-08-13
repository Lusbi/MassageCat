using System;
using GameCore.Audio;
using GameCore.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 一般遊戲模式使用的執行期設定選單。
/// </summary>
internal sealed class GameSettingsMenu : MonoBehaviour
{
    private const float k_settingsButtonMargin = 12f;
    private const float k_settingsButtonSize = 54f;
    private const float k_settingsIconSize = 42f;
    private const float k_settingsPanelWidth = 224f;
    private const float k_settingsPanelHeight = 164f;
    private const float k_menuButtonSize = 48f;
    private const float k_menuIconSize = 34f;

    private AutoNextButton m_autoNextButton;
    private GameObject m_dismissArea;
    private GameObject m_settingsPanel;
    private Image m_bgmStateIcon;
    private Image m_sfxStateIcon;
    private Image m_autoNextStateIcon;
    private TMP_FontAsset m_runtimeFont;
    private bool m_isOpen;

    internal static void CreateRuntime(Transform parent, AutoNextButton autoNextButton)
    {
        if (parent == null || parent.GetComponentInChildren<GameSettingsMenu>(true) != null)
            return;

        GameObject root = new GameObject(
            "GameSettingsMenu",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(GameSettingsMenu));
        root.transform.SetParent(parent, false);
        root.layer = parent.gameObject.layer;

        RectTransform rootRect = (RectTransform)root.transform;
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(640f, 480f);
        scaler.matchWidthOrHeight = 0.5f;

        root.GetComponent<GameSettingsMenu>().Initialize(autoNextButton);
    }

    private void Initialize(AutoNextButton autoNextButton)
    {
        m_autoNextButton = autoNextButton;
        TextMeshProUGUI existingText = FindFirstObjectByType<TextMeshProUGUI>(FindObjectsInactive.Include);
        m_runtimeFont = existingText != null ? existingText.font : TMP_Settings.defaultFontAsset;

        RectTransform root = (RectTransform)transform;
        CreateDismissArea(root);
        CreateSettingsPanel(root);
        CreateSettingsButton(root);
        SetSettingsPanelOpen(false);
    }

    private void CreateDismissArea(RectTransform root)
    {
        RectTransform dismissArea = CreateRectTransform("SettingsDismissArea", root);
        dismissArea.anchorMin = Vector2.zero;
        dismissArea.anchorMax = Vector2.one;
        dismissArea.offsetMin = Vector2.zero;
        dismissArea.offsetMax = Vector2.zero;

        Image image = dismissArea.gameObject.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = true;

        UIEventListener.Get(dismissArea.gameObject).onClick = OnSettingsDismissClicked;
        m_dismissArea = dismissArea.gameObject;
    }

    private void CreateSettingsPanel(RectTransform root)
    {
        RectTransform panel = CreateRectTransform("SettingsPanel", root);
        panel.anchorMin = new Vector2(1f, 1f);
        panel.anchorMax = new Vector2(1f, 1f);
        panel.pivot = new Vector2(1f, 1f);
        panel.anchoredPosition = new Vector2(-k_settingsButtonMargin, -72f);
        panel.sizeDelta = new Vector2(k_settingsPanelWidth, k_settingsPanelHeight);

        Image background = panel.gameObject.AddComponent<Image>();
        background.sprite = MassageCatUiArt.SettingsPanel;
        background.type = background.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        background.color = background.sprite != null ? Color.white : new Color(0.42f, 0.22f, 0.08f, 0.96f);
        background.raycastTarget = true;

        TextMeshProUGUI title = CreateText("Title", panel, "設定", 18f);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -12f);
        titleRect.sizeDelta = new Vector2(-32f, 24f);
        title.color = new Color32(70, 36, 20, 255);
        title.fontStyle = FontStyles.Bold;

        m_bgmStateIcon = CreateMenuButton(panel, "BgmToggle", -62f, -48f, OnBgmToggleClicked, MassageCatUiArt.BgmOn);
        m_sfxStateIcon = CreateMenuButton(panel, "SfxToggle", 0f, -48f, OnSfxToggleClicked, MassageCatUiArt.SfxOn);
        CreateMenuButton(panel, "Quit", 62f, -48f, OnQuitClicked, MassageCatUiArt.Quit);
        m_autoNextStateIcon = CreateMenuButton(panel, "AutoNext", -31f, -106f, OnAutoNextToggleClicked, MassageCatUiArt.AutoNextOff);
        CreateMenuButton(panel, "Minimize", 31f, -106f, OnMinimizeClicked, MassageCatUiArt.Minimize);
        m_settingsPanel = panel.gameObject;
    }

    private void CreateSettingsButton(RectTransform root)
    {
        RectTransform settingsButton = CreateRectTransform("SettingsButton", root);
        settingsButton.anchorMin = new Vector2(1f, 1f);
        settingsButton.anchorMax = new Vector2(1f, 1f);
        settingsButton.pivot = new Vector2(1f, 1f);
        settingsButton.anchoredPosition = new Vector2(-k_settingsButtonMargin, -k_settingsButtonMargin);
        settingsButton.sizeDelta = new Vector2(k_settingsButtonSize, k_settingsButtonSize);

        Image hitArea = settingsButton.gameObject.AddComponent<Image>();
        hitArea.color = new Color(0f, 0f, 0f, 0f);
        hitArea.raycastTarget = true;

        RectTransform iconRect = CreateRectTransform("Icon", settingsButton);
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(k_settingsIconSize, k_settingsIconSize);

        Image icon = iconRect.gameObject.AddComponent<Image>();
        icon.sprite = MassageCatUiArt.Settings;
        icon.color = icon.sprite != null ? Color.white : new Color32(90, 57, 105, 255);
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        MassageCatUiArt.ApplyIconOutline(icon);

        settingsButton.gameObject.AddComponent<UIButton>().onClicked = OnSettingsButtonClicked;
    }

    private Image CreateMenuButton(
        RectTransform parent,
        string objectName,
        float anchoredX,
        float anchoredY,
        Action<UIButton> onClick,
        Sprite iconSprite)
    {
        RectTransform button = CreateRectTransform(objectName, parent);
        button.anchorMin = new Vector2(0.5f, 1f);
        button.anchorMax = new Vector2(0.5f, 1f);
        button.pivot = new Vector2(0.5f, 1f);
        button.anchoredPosition = new Vector2(anchoredX, anchoredY);
        button.sizeDelta = new Vector2(k_menuButtonSize, k_menuButtonSize);

        Image image = button.gameObject.AddComponent<Image>();
        // 僅保留功能圖示；透明 Image 仍負責接收按鈕點擊。
        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = true;

        RectTransform iconRect = CreateRectTransform("Icon", button);
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(k_menuIconSize, k_menuIconSize);

        Image icon = iconRect.gameObject.AddComponent<Image>();
        icon.sprite = iconSprite;
        icon.color = Color.white;
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        button.gameObject.AddComponent<UIButton>().onClicked = onClick;
        return icon;
    }

    private TextMeshProUGUI CreateText(string objectName, Transform parent, string content, float fontSize)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = m_runtimeFont;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = fontSize;
        text.SetText(content);
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform CreateRectTransform(string objectName, Transform parent)
    {
        GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return (RectTransform)gameObject.transform;
    }

    private void OnSettingsButtonClicked(UIButton button)
    {
        SetSettingsPanelOpen(!m_isOpen);
    }

    private void OnSettingsDismissClicked(GameObject gameObject)
    {
        SetSettingsPanelOpen(false);
    }

    private void OnBgmToggleClicked(UIButton button)
    {
        if (AudioClipManager.instance == null)
            return;

        AudioClipManager.instance.ToggleBGM();
        RefreshState();
        AudioClipManager.instance.PlaySFX(AudioClipType.Button_ClickSFX);
    }

    private void OnSfxToggleClicked(UIButton button)
    {
        if (AudioClipManager.instance == null)
            return;

        AudioClipManager.instance.ToggleSFX();
        RefreshState();
        AudioClipManager.instance.PlaySFX(AudioClipType.Button_ClickSFX);
    }

    private void OnAutoNextToggleClicked(UIButton button)
    {
        if (m_autoNextButton == null || m_autoNextButton.TryToggle() == false)
            return;

        RefreshState();
        if (AudioClipManager.instance != null)
            AudioClipManager.instance.PlaySFX(AudioClipType.Button_ClickSFX);
    }

    private void OnMinimizeClicked(UIButton button)
    {
        SetSettingsPanelOpen(false);
        if (AudioClipManager.instance != null)
            AudioClipManager.instance.PlaySFX(AudioClipType.Button_ClickSFX);

        DesktopPetWindowController.MinimizeStandaloneWindow();
    }

    private void OnQuitClicked(UIButton button)
    {
        Application.Quit();
    }

    private void SetSettingsPanelOpen(bool isOpen)
    {
        m_isOpen = isOpen;
        if (m_dismissArea != null)
            m_dismissArea.SetActive(isOpen);
        if (m_settingsPanel != null)
            m_settingsPanel.SetActive(isOpen);
        if (isOpen)
            RefreshState();
    }

    private void RefreshState()
    {
        if (AudioClipManager.instance != null)
        {
            if (m_bgmStateIcon != null)
                m_bgmStateIcon.sprite = AudioClipManager.instance.IsBGMEnabled ? MassageCatUiArt.BgmOn : MassageCatUiArt.BgmOff;
            if (m_sfxStateIcon != null)
                m_sfxStateIcon.sprite = AudioClipManager.instance.IsSFXEnabled ? MassageCatUiArt.SfxOn : MassageCatUiArt.SfxOff;
        }

        if (m_autoNextStateIcon == null)
            return;

        bool isUnlocked = m_autoNextButton != null && m_autoNextButton.IsUnlocked;
        bool isEnabled = isUnlocked && m_autoNextButton.isAutoNext;
        m_autoNextStateIcon.sprite = isEnabled ? MassageCatUiArt.AutoNextOn : MassageCatUiArt.AutoNextOff;
        m_autoNextStateIcon.color = isUnlocked ? Color.white : new Color(1f, 1f, 1f, 0.35f);
    }
}
