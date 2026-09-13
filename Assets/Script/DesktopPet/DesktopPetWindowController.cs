using System;
using System.Collections;
using GameCore.Audio;
using GameCore.UI;
using MassageCat.Gallery;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class DesktopPetWindowController : MonoBehaviour
{
    private static readonly Color s_transparentBackgroundColor = new Color(0f, 0f, 0f, 0f);
    private static DesktopPetWindowController s_instance;

    [SerializeField] private Vector2Int m_windowSize = new Vector2Int(480, 320);
    [SerializeField] private Vector2Int m_windowPosition = Vector2Int.zero;
    [SerializeField] private int m_targetFrameRate = 30;

    private const float k_dragAreaHeight = 40f;
    private const float k_dragAreaLeftInset = 56f;
    private const float k_settingsButtonSize = 48f;
    private const float k_settingsIconSize = 32f;
    private const float k_settingsButtonMargin = 8f;
    private const float k_settingsPanelWidth = 192f;
    private const float k_settingsPanelHeight = 164f;
    private const float k_menuButtonSize = 44f;
    private const float k_menuIconSize = 28f;
    private const float k_rightControlReserve = 104f;
    private const int k_controlSortingOrder = 1000;
    private float m_reapplyTimer;
    private float m_windowScale = 1f;
    private Vector2Int m_scaledWindowSize;
    private GameObject m_runtimeControlsRoot;
    private RectTransform m_runtimeControlsViewport;
    private GalleryButton m_galleryButton;
    private SceneVisualToggleButton m_sceneVisualToggleButton;
    private AutoNextButton m_autoNextButton;
    private GameObject m_settingsDismissArea;
    private GameObject m_settingsPanel;
    private GameObject m_settingsButton;
    private Image m_bgmStateIcon;
    private Image m_sfxStateIcon;
    private Image m_autoNextStateIcon;
    private TMP_FontAsset m_runtimeFont;
    private bool m_settingsPanelOpen;
    private bool m_galleryExpanded;
    private bool m_windowMinimized;
    private int m_lastScreenWidth;
    private int m_lastScreenHeight;

    internal Vector2 GalleryViewportOrigin => Vector2.zero;
    internal Vector2 GameViewportSize => m_windowSize;

    internal bool GalleryExpanded => m_galleryExpanded;
    internal float WindowScale => m_windowScale;

    public static bool IsPointerInRuntimeControlArea()
    {
        return s_instance != null && s_instance.ContainsRuntimeControlArea(Input.mousePosition);
    }

    internal static void RegisterSceneVisualToggle(SceneVisualToggleButton sceneVisualToggleButton)
    {
        if (s_instance == null)
            return;

        s_instance.m_sceneVisualToggleButton = sceneVisualToggleButton;
    }

    internal static void RegisterGalleryButton(GalleryButton galleryButton)
    {
        if (s_instance == null)
            return;

        s_instance.m_galleryButton = galleryButton;
    }

    internal static void RegisterAutoNextButton(AutoNextButton autoNextButton)
    {
        if (s_instance == null)
            return;

        s_instance.m_autoNextButton = autoNextButton;
        s_instance.RefreshAutoNextState();
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        s_instance = this;
        InitializeWindowScale();
        ApplyRuntimeSettings();
        ApplyCameraSettings();
    }

    private void Start()
    {
        Screen.SetResolution(m_scaledWindowSize.x, m_scaledWindowSize.y, false);

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        StartCoroutine(ApplyWindowWhenReady());
#endif
    }

    private void Update()
    {
        int screenWidth = Screen.width;
        int screenHeight = Screen.height;
        if (screenWidth != m_lastScreenWidth || screenHeight != m_lastScreenHeight)
        {
            m_lastScreenWidth = screenWidth;
            m_lastScreenHeight = screenHeight;
            ApplyCollapsedViewport();
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        System.IntPtr windowHandle = GetUnityWindowHandle();
        if (m_windowMinimized)
        {
            if (windowHandle == System.IntPtr.Zero || IsIconic(windowHandle))
                return;

            m_windowMinimized = false;
            ApplyWindowStyles(false);
        }

        m_reapplyTimer += Time.unscaledDeltaTime;
        if (m_reapplyTimer >= 2f)
        {
            m_reapplyTimer = 0f;
            ApplyWindowStyles(false);
        }
#endif
    }

    private void OnDestroy()
    {
        if (s_instance == this)
        {
            s_instance = null;
        }
    }

    public void EnsureRuntimeControls()
    {
        // 桌寵控制項已序列化在 GameMain Prefab；此處僅保留視窗行為。
    }

    internal void SetGalleryExpanded(bool expanded)
    {
        if (m_galleryExpanded == expanded)
            return;

        if (m_galleryButton != null)
            m_galleryButton.SetExpanded(expanded);

        SetSettingsPanelOpen(false);
        if (expanded)
            ExpandForGallery();
        else
            CollapseGallery();
    }

    private void CreateDragArea(RectTransform root)
    {
        RectTransform dragArea = CreateRectTransform("DesktopPetDragArea", root);
        dragArea.anchorMin = new Vector2(0f, 1f);
        dragArea.anchorMax = new Vector2(1f, 1f);
        dragArea.pivot = new Vector2(0.5f, 1f);
        dragArea.offsetMin = new Vector2(k_dragAreaLeftInset, -k_dragAreaHeight);
        // 右上角保留設定鍵與場景顯示切換鍵的點擊範圍。
        dragArea.offsetMax = new Vector2(-k_rightControlReserve, 0f);

        Image image = dragArea.gameObject.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = true;

        UIEventListener listener = UIEventListener.Get(dragArea.gameObject);
        listener.onDown = OnDragAreaPointerDown;
    }

    private void CreateSettingsDismissArea(RectTransform root)
    {
        RectTransform dismissArea = CreateRectTransform("DesktopPetSettingsDismissArea", root);
        dismissArea.anchorMin = Vector2.zero;
        dismissArea.anchorMax = Vector2.one;
        dismissArea.offsetMin = Vector2.zero;
        dismissArea.offsetMax = Vector2.zero;

        Image image = dismissArea.gameObject.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = true;

        UIEventListener listener = UIEventListener.Get(dismissArea.gameObject);
        listener.onClick = OnSettingsDismissClicked;
        m_settingsDismissArea = dismissArea.gameObject;
    }

    private void CreateSettingsPanel(RectTransform root)
    {
        RectTransform panel = CreateRectTransform("DesktopPetSettingsPanel", root);
        panel.anchorMin = new Vector2(0f, 1f);
        panel.anchorMax = new Vector2(0f, 1f);
        panel.pivot = new Vector2(0f, 1f);
        panel.anchoredPosition = new Vector2(k_settingsButtonMargin, -80f);
        panel.sizeDelta = new Vector2(k_settingsPanelWidth, k_settingsPanelHeight);

        Image background = panel.gameObject.AddComponent<Image>();
        Sprite panelSprite = DesktopPetChibiPaperArt.SettingsPanel;
        if (panelSprite != null)
        {
            background.sprite = panelSprite;
            background.color = Color.white;
        }
        else
        {
            background.color = new Color(0.42f, 0.22f, 0.08f, 0.96f);
        }
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
        m_autoNextStateIcon = CreateMenuButton(panel, "AutoNext", -31f, -110f, OnAutoNextToggleClicked, MassageCatUiArt.AutoNextOff);
        CreateMenuButton(panel, "Minimize", 31f, -110f, OnMinimizeClicked, MassageCatUiArt.Minimize);
        m_settingsPanel = panel.gameObject;
    }

    private void CreateSettingsButton(RectTransform root)
    {
        RectTransform settingsButton = CreateRectTransform("DesktopPetSettingsButton", root);
        settingsButton.anchorMin = new Vector2(0f, 1f);
        settingsButton.anchorMax = new Vector2(0f, 1f);
        settingsButton.pivot = new Vector2(0f, 1f);
        settingsButton.anchoredPosition = new Vector2(k_settingsButtonMargin, -k_settingsButtonMargin);
        settingsButton.sizeDelta = new Vector2(k_settingsButtonSize, k_settingsButtonSize);

        Image hitArea = settingsButton.gameObject.AddComponent<Image>();
        hitArea.color = new Color(0f, 0f, 0f, 0f);
        hitArea.raycastTarget = true;
        Sprite settingsSprite = MassageCatUiArt.DesktopPetSettingsIcon;
        if (settingsSprite != null)
        {
            RectTransform iconRect = CreateRectTransform("Icon", settingsButton);
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(k_settingsIconSize, k_settingsIconSize);

            Image icon = iconRect.gameObject.AddComponent<Image>();
            icon.sprite = settingsSprite;
            icon.color = Color.white;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            MassageCatUiArt.ApplyIconOutline(icon);
        }
        else
        {
            CreateGearIcon(settingsButton, new Color(0.05f, 0.05f, 0.05f, 0.72f));
        }

        UIButton button = settingsButton.gameObject.AddComponent<UIButton>();
        button.onClicked = OnSettingsButtonClicked;
        m_settingsButton = settingsButton.gameObject;
    }

    private static void CreateGearIcon(RectTransform parent, Color holeColor)
    {
        for (int i = 0; i < 4; i++)
        {
            RectTransform toothPair = CreateRectTransform($"ToothPair_{i}", parent);
            toothPair.anchorMin = new Vector2(0.5f, 0.5f);
            toothPair.anchorMax = new Vector2(0.5f, 0.5f);
            toothPair.sizeDelta = new Vector2(22f, 5f);
            toothPair.localEulerAngles = new Vector3(0f, 0f, i * 45f);

            Image toothImage = toothPair.gameObject.AddComponent<Image>();
            toothImage.color = Color.white;
            toothImage.raycastTarget = false;
        }

        RectTransform hub = CreateRectTransform("Hub", parent);
        hub.anchorMin = new Vector2(0.5f, 0.5f);
        hub.anchorMax = new Vector2(0.5f, 0.5f);
        hub.sizeDelta = new Vector2(14f, 14f);
        hub.localEulerAngles = new Vector3(0f, 0f, 45f);

        Image hubImage = hub.gameObject.AddComponent<Image>();
        hubImage.color = Color.white;
        hubImage.raycastTarget = false;

        RectTransform hole = CreateRectTransform("Hole", parent);
        hole.anchorMin = new Vector2(0.5f, 0.5f);
        hole.anchorMax = new Vector2(0.5f, 0.5f);
        hole.sizeDelta = new Vector2(6f, 6f);

        Image holeImage = hole.gameObject.AddComponent<Image>();
        holeImage.color = holeColor;
        holeImage.raycastTarget = false;
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
        image.sprite = DesktopPetChibiPaperArt.IconButtonFrame;
        image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = image.sprite != null ? Color.white : new Color32(82, 65, 104, 242);
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

        UIButton uiButton = button.gameObject.AddComponent<UIButton>();
        uiButton.onClicked = onClick;
        return icon;
    }

    private TextMeshProUGUI CreateText(
        string objectName,
        Transform parent,
        string content,
        float fontSize)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rectTransform = (RectTransform)textObject.transform;
        rectTransform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        if (m_runtimeFont != null)
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
        RectTransform rectTransform = (RectTransform)gameObject.transform;
        rectTransform.SetParent(parent, false);
        return rectTransform;
    }

    private void OnDragAreaPointerDown(GameObject go, Vector2 position)
    {
        TryBeginWindowDrag();
    }

    private void OnSettingsButtonClicked(UIButton button)
    {
        SetSettingsPanelOpen(!m_settingsPanelOpen);
    }

    private void OnSettingsDismissClicked(GameObject go)
    {
        SetSettingsPanelOpen(false);
    }

    private void OnMinimizeClicked(UIButton button)
    {
        SetSettingsPanelOpen(false);
        if (AudioClipManager.instance != null)
            AudioClipManager.instance.PlaySFX(AudioClipType.Button_ClickSFX);

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        MinimizeWindow();
#endif
    }

    /// <summary>
    /// 以原生 Windows 行為最小化一般遊戲視窗。
    /// </summary>
    internal static void MinimizeStandaloneWindow()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        System.IntPtr windowHandle = GetUnityWindowHandle();
        if (windowHandle != System.IntPtr.Zero)
            ShowWindow(windowHandle, k_swMinimize);
#endif
    }

    private void OnAutoNextToggleClicked(UIButton button)
    {
        if (m_autoNextButton == null || m_autoNextButton.TryToggle() == false)
            return;

        RefreshAutoNextState();
        if (AudioClipManager.instance != null)
            AudioClipManager.instance.PlaySFX(AudioClipType.Button_ClickSFX);
    }

    private void OnBgmToggleClicked(UIButton button)
    {
        AudioClipManager.instance.ToggleBGM();
        RefreshSettingsState();
        AudioClipManager.instance.PlaySFX(AudioClipType.Button_ClickSFX);
    }

    private void OnSfxToggleClicked(UIButton button)
    {
        AudioClipManager.instance.ToggleSFX();
        RefreshSettingsState();
        AudioClipManager.instance.PlaySFX(AudioClipType.Button_ClickSFX);
    }

    private void OnQuitClicked(UIButton button)
    {
        Application.Quit();
    }

    private void SetSettingsPanelOpen(bool isOpen)
    {
        m_settingsPanelOpen = isOpen;
        if (m_settingsDismissArea != null)
            m_settingsDismissArea.SetActive(isOpen);
        if (m_settingsPanel != null)
            m_settingsPanel.SetActive(isOpen);
        if (isOpen)
        {
            // 面板開啟時確保入口鈕仍在最上層，避免透明遮罩覆蓋其圖示。
            if (m_settingsButton != null)
            {
                m_settingsButton.SetActive(true);
                m_settingsButton.transform.SetAsLastSibling();
            }
            RefreshSettingsState();
            RefreshAutoNextState();
        }
    }

    private void RefreshSettingsState()
    {
        if (m_bgmStateIcon != null)
            m_bgmStateIcon.sprite = AudioClipManager.instance.IsBGMEnabled
                ? MassageCatUiArt.BgmOn
                : MassageCatUiArt.BgmOff;
        if (m_sfxStateIcon != null)
            m_sfxStateIcon.sprite = AudioClipManager.instance.IsSFXEnabled
                ? MassageCatUiArt.SfxOn
                : MassageCatUiArt.SfxOff;
    }

    private void RefreshAutoNextState()
    {
        if (m_autoNextStateIcon == null)
            return;

        bool isUnlocked = m_autoNextButton != null && m_autoNextButton.IsUnlocked;
        bool isEnabled = isUnlocked && m_autoNextButton.isAutoNext;
        m_autoNextStateIcon.sprite = isEnabled
            ? MassageCatUiArt.AutoNextOn
            : MassageCatUiArt.AutoNextOff;
        m_autoNextStateIcon.color = isUnlocked ? Color.white : new Color32(255, 255, 255, 100);
    }

    private bool ContainsRuntimeControlArea(Vector3 screenPosition)
    {
        if (m_runtimeControlsRoot == null)
            return false;

        if (m_settingsPanelOpen)
            return true;

        float inverseWindowScale = 1f / m_windowScale;
        Vector2 referenceScreenPosition = new Vector2(
            screenPosition.x * inverseWindowScale,
            screenPosition.y * inverseWindowScale);
        Vector2 viewportOrigin = Vector2.zero;
        float dragAreaRight = viewportOrigin.x + m_windowSize.x - k_rightControlReserve;
        if (referenceScreenPosition.y >= viewportOrigin.y + m_windowSize.y - k_dragAreaHeight
            && referenceScreenPosition.y <= viewportOrigin.y + m_windowSize.y
            && referenceScreenPosition.x >= viewportOrigin.x + k_dragAreaLeftInset
            && referenceScreenPosition.x <= dragAreaRight)
            return true;

        float settingsButtonLeft = viewportOrigin.x + k_settingsButtonMargin;
        float settingsButtonBottom = viewportOrigin.y + m_windowSize.y - k_settingsButtonMargin - k_settingsButtonSize;
        if (referenceScreenPosition.x >= settingsButtonLeft
            && referenceScreenPosition.x <= settingsButtonLeft + k_settingsButtonSize
            && referenceScreenPosition.y >= settingsButtonBottom
            && referenceScreenPosition.y <= settingsButtonBottom + k_settingsButtonSize)
            return true;

        float rightControlsLeft = viewportOrigin.x + m_windowSize.x - k_rightControlReserve;
        float rightControlsBottom = viewportOrigin.y + m_windowSize.y - k_settingsButtonMargin - k_settingsButtonSize;
        return referenceScreenPosition.x >= rightControlsLeft
               && referenceScreenPosition.y >= rightControlsBottom;
    }

    private void ExpandForGallery()
    {
        m_galleryExpanded = true;
        // 覆蓋式圖鑑維持與主畫面相同的視窗尺寸，不再展開成左右雙欄視窗。
        ApplyCollapsedViewport();
    }

    private void CollapseGallery()
    {
        m_galleryExpanded = false;
        ApplyCollapsedViewport();
    }

    private void ApplyCollapsedViewport()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
            mainCamera.rect = new Rect(0f, 0f, 1f, 1f);

        ApplyRuntimeControlsViewport(Vector2.zero);
    }

    private void ApplyRuntimeControlsViewport(Vector2 origin)
    {
        if (m_runtimeControlsViewport != null)
            m_runtimeControlsViewport.anchoredPosition = origin;
    }

    internal static bool TryBeginWindowDrag()
    {
        if (s_instance == null)
            return false;

        s_instance.BeginWindowDrag();
        return true;
    }

    private void BeginWindowDrag()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        System.IntPtr windowHandle = GetUnityWindowHandle();
        if (windowHandle == System.IntPtr.Zero)
            return;

        ReleaseCapture();
        SendMessage(windowHandle, k_wmNcLButtonDown, new System.IntPtr(k_htCaption), System.IntPtr.Zero);
#endif
    }

    private void ApplyRuntimeSettings()
    {
        Application.runInBackground = true;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = m_targetFrameRate;
    }

    private void InitializeWindowScale()
    {
        // Editor 與發行版固定使用同一組邏輯像素，避免各自的初始解析度改變 UI 尺寸。
        m_windowScale = 1f;
        m_scaledWindowSize = m_windowSize;
    }

    private void ApplyCameraSettings()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return;

        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = s_transparentBackgroundColor;
    }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    private void MinimizeWindow()
    {
        System.IntPtr windowHandle = GetUnityWindowHandle();
        if (windowHandle == System.IntPtr.Zero)
            return;

        // 最小化期間顯示工作列項目，讓使用者能直接點擊恢復。
        System.IntPtr exStyle = GetWindowLongPtr(windowHandle, k_gwlExStyle);
        long exStyleValue = exStyle.ToInt64();
        exStyleValue &= ~k_wsExToolWindow;
        exStyleValue |= k_wsExAppWindow;
        SetWindowLongPtr(windowHandle, k_gwlExStyle, new System.IntPtr(exStyleValue));
        SetWindowPos(
            windowHandle,
            System.IntPtr.Zero,
            0,
            0,
            0,
            0,
            k_swpNoMove | k_swpNoSize | k_swpNoZOrder | k_swpFrameChanged);

        m_windowMinimized = true;
        ShowWindow(windowHandle, k_swMinimize);
    }

    private void ApplyNativeWindowBounds(Vector2Int position, int width, int height)
    {
        System.IntPtr windowHandle = GetUnityWindowHandle();
        if (windowHandle == System.IntPtr.Zero)
            return;

        SetWindowPos(
            windowHandle,
            k_hwndTopMost,
            position.x,
            position.y,
            width,
            height,
            k_swpNoActivate | k_swpFrameChanged | k_swpShowWindow);
    }

    private IEnumerator ApplyWindowWhenReady()
    {
        for (int i = 0; i < 30; i++)
        {
            if (ApplyWindowStyles(true))
                yield break;
            yield return null;
        }
    }

    private bool ApplyWindowStyles(bool resize)
    {
        System.IntPtr windowHandle = GetUnityWindowHandle();
        if (windowHandle == System.IntPtr.Zero)
            return false;

        System.IntPtr style = GetWindowLongPtr(windowHandle, k_gwlStyle);
        long styleValue = style.ToInt64();
        styleValue &= ~(k_wsCaption | k_wsThickFrame | k_wsMinimizeBox | k_wsMaximizeBox | k_wsSysMenu);
        styleValue |= k_wsPopup;
        SetWindowLongPtr(windowHandle, k_gwlStyle, new System.IntPtr(styleValue));

        System.IntPtr exStyle = GetWindowLongPtr(windowHandle, k_gwlExStyle);
        long exStyleValue = exStyle.ToInt64();
        exStyleValue |= k_wsExLayered | k_wsExToolWindow;
        exStyleValue &= ~(k_wsExTransparent | k_wsExAppWindow);
        SetWindowLongPtr(windowHandle, k_gwlExStyle, new System.IntPtr(exStyleValue));

        SetLayeredWindowAttributes(windowHandle, 0, byte.MaxValue, k_lwaAlpha);
        DwmExtendFrameIntoClientArea(windowHandle, ref s_fullWindowMargins);

        uint flags = k_swpNoActivate | k_swpFrameChanged | k_swpShowWindow;
        if (resize == false)
            flags |= k_swpNoMove;

        Vector2Int windowPosition = resize ? GetCenteredWindowPosition(windowHandle) : m_windowPosition;
        if (resize)
        {
            m_windowPosition = windowPosition;
        }

        SetWindowPos(
            windowHandle,
            k_hwndTopMost,
            windowPosition.x,
            windowPosition.y,
            m_scaledWindowSize.x,
            m_scaledWindowSize.y,
            flags);
        return true;
    }

    private static System.IntPtr GetUnityWindowHandle()
    {
        using (System.Diagnostics.Process process = System.Diagnostics.Process.GetCurrentProcess())
        {
            if (process.MainWindowHandle != System.IntPtr.Zero)
                return process.MainWindowHandle;
        }
        return GetActiveWindow();
    }

    private Vector2Int GetCenteredWindowPosition(System.IntPtr windowHandle)
    {
        if (TryGetMonitorWorkArea(windowHandle, out NativeRect workArea) == false)
            return m_windowPosition;

        int workAreaWidth = workArea.right - workArea.left;
        int workAreaHeight = workArea.bottom - workArea.top;
        int x = workArea.left + System.Math.Max(0, (workAreaWidth - m_scaledWindowSize.x) / 2);
        int y = workArea.top + System.Math.Max(0, (workAreaHeight - m_scaledWindowSize.y) / 2);
        return new Vector2Int(x, y);
    }

    private static bool TryGetMonitorWorkArea(System.IntPtr windowHandle, out NativeRect workArea)
    {
        System.IntPtr monitorHandle = MonitorFromWindow(windowHandle, k_monitorDefaultToNearest);
        if (monitorHandle == System.IntPtr.Zero)
        {
            workArea = default;
            return false;
        }

        MonitorInfo monitorInfo = new MonitorInfo
        {
            cbSize = k_monitorInfoSize,
        };

        if (GetMonitorInfo(monitorHandle, ref monitorInfo) == false)
        {
            workArea = default;
            return false;
        }

        workArea = monitorInfo.rcWork;
        return true;
    }

    private struct Margins
    {
        public int leftWidth;
        public int rightWidth;
        public int topHeight;
        public int bottomHeight;
    }

    private struct NativeRect
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    private struct MonitorInfo
    {
        public uint cbSize;
        public NativeRect rcMonitor;
        public NativeRect rcWork;
        public uint dwFlags;
    }

    private const int k_gwlStyle = -16;
    private const int k_gwlExStyle = -20;
    private const long k_wsCaption = 0x00C00000L;
    private const long k_wsThickFrame = 0x00040000L;
    private const long k_wsMinimizeBox = 0x00020000L;
    private const long k_wsMaximizeBox = 0x00010000L;
    private const long k_wsSysMenu = 0x00080000L;
    private const long k_wsPopup = unchecked((long)0x80000000);
    private const long k_wsExLayered = 0x00080000L;
    private const long k_wsExTransparent = 0x00000020L;
    private const long k_wsExToolWindow = 0x00000080L;
    private const long k_wsExAppWindow = 0x00040000L;
    private const uint k_lwaAlpha = 0x00000002;
    private const uint k_swpNoMove = 0x0002;
    private const uint k_swpNoSize = 0x0001;
    private const uint k_swpNoZOrder = 0x0004;
    private const uint k_swpNoActivate = 0x0010;
    private const uint k_swpShowWindow = 0x0040;
    private const uint k_swpFrameChanged = 0x0020;
    private const uint k_monitorDefaultToNearest = 0x00000002;
    private const uint k_monitorInfoSize = 40;
    private const int k_wmNcLButtonDown = 0x00A1;
    private const int k_htCaption = 2;
    private const int k_swMinimize = 6;
    private static readonly System.IntPtr k_hwndTopMost = new System.IntPtr(-1);
    private static Margins s_fullWindowMargins = new Margins
    {
        leftWidth = -1,
        rightWidth = -1,
        topHeight = -1,
        bottomHeight = -1,
    };

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern System.IntPtr GetActiveWindow();

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern System.IntPtr GetWindowLongPtr(System.IntPtr hWnd, int nIndex);

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern System.IntPtr SetWindowLongPtr(System.IntPtr hWnd, int nIndex, System.IntPtr dwNewLong);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool SetLayeredWindowAttributes(System.IntPtr hwnd, int crKey, byte bAlpha, uint dwFlags);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool SetWindowPos(
        System.IntPtr hWnd,
        System.IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern System.IntPtr MonitorFromWindow(System.IntPtr hwnd, uint dwFlags);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(System.IntPtr hMonitor, ref MonitorInfo lpmi);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool GetWindowRect(System.IntPtr hWnd, out NativeRect lpRect);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool IsIconic(System.IntPtr hWnd);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool ShowWindow(System.IntPtr hWnd, int command);

    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(System.IntPtr hWnd, ref Margins margins);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern System.IntPtr SendMessage(System.IntPtr hWnd, int msg, System.IntPtr wParam, System.IntPtr lParam);
#endif
}
