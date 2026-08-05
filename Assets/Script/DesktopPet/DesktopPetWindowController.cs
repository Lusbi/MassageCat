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

    [SerializeField] private Vector2Int m_windowSize = new Vector2Int(640, 480);
    [SerializeField] private Vector2Int m_windowPosition = Vector2Int.zero;
    [SerializeField] private int m_targetFrameRate = 30;

    private const float k_dragAreaHeight = 56f;
    private const float k_dragAreaLeftInset = 112f;
    private const float k_settingsButtonSize = 64f;
    private const float k_settingsIconSize = 44f;
    private const float k_settingsButtonMargin = 8f;
    private const float k_settingsPanelWidth = 196f;
    private const float k_settingsPanelHeight = 224f;
    private const float k_rightControlReserve = 152f;
    private const int k_controlSortingOrder = 1000;
    private const int k_galleryWidth = 960;
    private const int k_galleryHeight = 640;
    private const int k_referenceDisplayWidth = 2560;
    private const int k_referenceDisplayHeight = 1440;

    private float m_reapplyTimer;
    private float m_windowScale = 1f;
    private Vector2Int m_scaledWindowSize;
    private Vector2Int m_scaledGallerySize;
    private GameObject m_runtimeControlsRoot;
    private RectTransform m_runtimeControlsViewport;
    private GalleryButton m_galleryButton;
    private SceneVisualToggleButton m_sceneVisualToggleButton;
    private GameObject m_settingsDismissArea;
    private GameObject m_settingsPanel;
    private TextMeshProUGUI m_bgmStateText;
    private TextMeshProUGUI m_sfxStateText;
    private TMP_FontAsset m_runtimeFont;
    private Camera m_transparencyCamera;
    private bool m_settingsPanelOpen;
    private bool m_galleryExpanded;
    private bool m_galleryOnLeft;
    private Vector2Int m_collapsedWindowPosition;
    private int m_lastScreenWidth;
    private int m_lastScreenHeight;

    internal Vector2 GalleryViewportOrigin => m_galleryOnLeft
        ? Vector2.zero
        : new Vector2(m_windowSize.x, 0f);

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
        s_instance.AttachSceneVisualToggleToRuntimeViewport();
    }

    internal static void RegisterGalleryButton(GalleryButton galleryButton)
    {
        if (s_instance == null)
            return;

        s_instance.m_galleryButton = galleryButton;
        s_instance.AttachGalleryButtonToRuntimeViewport();
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
            if (m_galleryExpanded)
                ApplyExpandedViewport();
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
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
        if (m_runtimeControlsRoot != null)
        {
            m_runtimeControlsRoot.transform.SetAsLastSibling();
            AttachGalleryButtonToRuntimeViewport();
            AttachSceneVisualToggleToRuntimeViewport();
            return;
        }

        if (UIManager.instance == null)
            return;

        RectTransform root = CreateRectTransform("DesktopPetRuntimeControls", transform);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        root.SetAsLastSibling();
        m_runtimeControlsRoot = root.gameObject;

        Canvas controlCanvas = m_runtimeControlsRoot.AddComponent<Canvas>();
        controlCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        controlCanvas.overrideSorting = true;
        controlCanvas.sortingOrder = k_controlSortingOrder;
        CanvasScaler controlScaler = m_runtimeControlsRoot.AddComponent<CanvasScaler>();
        controlScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        controlScaler.scaleFactor = m_windowScale;
        m_runtimeControlsRoot.AddComponent<GraphicRaycaster>();

        RectTransform viewport = CreateRectTransform("GameViewport", root);
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.zero;
        viewport.pivot = Vector2.zero;
        viewport.anchoredPosition = Vector2.zero;
        viewport.sizeDelta = m_windowSize;
        m_runtimeControlsViewport = viewport;

        TextMeshProUGUI existingText = UIManager.instance.GetComponentInChildren<TextMeshProUGUI>(true);
        m_runtimeFont = existingText != null ? existingText.font : TMP_Settings.defaultFontAsset;

        CreateDragArea(viewport);
        CreateSettingsDismissArea(viewport);
        CreateSettingsPanel(viewport);
        CreateSettingsButton(viewport);
        AttachGalleryButtonToRuntimeViewport();
        AttachSceneVisualToggleToRuntimeViewport();
        SetSettingsPanelOpen(false);
    }

    private void AttachGalleryButtonToRuntimeViewport()
    {
        if (m_runtimeControlsViewport == null || m_galleryButton == null)
            return;

        m_galleryButton.ApplyDesktopPetLayout(m_runtimeControlsViewport);
    }

    private void AttachSceneVisualToggleToRuntimeViewport()
    {
        if (m_runtimeControlsViewport == null || m_sceneVisualToggleButton == null)
            return;

        m_sceneVisualToggleButton.ApplyDesktopPetLayout(m_runtimeControlsViewport);
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
        panel.anchorMin = new Vector2(1f, 1f);
        panel.anchorMax = new Vector2(1f, 1f);
        panel.pivot = new Vector2(1f, 1f);
        panel.anchoredPosition = new Vector2(-k_settingsButtonMargin, -80f);
        panel.sizeDelta = new Vector2(k_settingsPanelWidth, k_settingsPanelHeight);

        Image background = panel.gameObject.AddComponent<Image>();
        Sprite panelSprite = MassageCatUiArt.SettingsPanel;
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
        titleRect.anchoredPosition = new Vector2(0f, -18f);
        titleRect.sizeDelta = new Vector2(-32f, 28f);
        title.color = new Color32(70, 36, 20, 255);
        title.fontStyle = FontStyles.Bold;

        m_bgmStateText = CreateMenuButton(panel, "BgmToggle", -58f, OnBgmToggleClicked);
        m_sfxStateText = CreateMenuButton(panel, "SfxToggle", -106f, OnSfxToggleClicked);
        CreateMenuButton(panel, "Quit", -154f, OnQuitClicked).SetText("離開遊戲");
        m_settingsPanel = panel.gameObject;
    }

    private void CreateSettingsButton(RectTransform root)
    {
        RectTransform settingsButton = CreateRectTransform("DesktopPetSettingsButton", root);
        settingsButton.anchorMin = new Vector2(1f, 1f);
        settingsButton.anchorMax = new Vector2(1f, 1f);
        settingsButton.pivot = new Vector2(1f, 1f);
        settingsButton.anchoredPosition = new Vector2(-k_settingsButtonMargin, -k_settingsButtonMargin);
        settingsButton.sizeDelta = new Vector2(k_settingsButtonSize, k_settingsButtonSize);

        Image hitArea = settingsButton.gameObject.AddComponent<Image>();
        hitArea.color = new Color(0f, 0f, 0f, 0f);
        hitArea.raycastTarget = true;
        Sprite settingsSprite = MassageCatUiArt.Settings;
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

    private TextMeshProUGUI CreateMenuButton(
        RectTransform parent,
        string objectName,
        float anchoredY,
        Action<UIButton> onClick)
    {
        RectTransform button = CreateRectTransform(objectName, parent);
        button.anchorMin = new Vector2(0f, 1f);
        button.anchorMax = new Vector2(1f, 1f);
        button.pivot = new Vector2(0.5f, 1f);
        button.anchoredPosition = new Vector2(0f, anchoredY);
        button.sizeDelta = new Vector2(-36f, 40f);

        Image image = button.gameObject.AddComponent<Image>();
        image.color = new Color32(82, 65, 104, 242);
        image.raycastTarget = true;
        MassageCatUiArt.ApplyIconOutline(image);

        TextMeshProUGUI label = CreateText("Text", button, string.Empty, 16f);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 2f);
        labelRect.offsetMax = new Vector2(-8f, -2f);
        label.color = new Color32(255, 239, 195, 255);

        UIButton uiButton = button.gameObject.AddComponent<UIButton>();
        uiButton.onClicked = onClick;
        return label;
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
        BeginWindowDrag();
    }

    private void OnSettingsButtonClicked(UIButton button)
    {
        SetSettingsPanelOpen(!m_settingsPanelOpen);
    }

    private void OnSettingsDismissClicked(GameObject go)
    {
        SetSettingsPanelOpen(false);
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
            RefreshSettingsState();
    }

    private void RefreshSettingsState()
    {
        if (m_bgmStateText != null)
            m_bgmStateText.SetText(AudioClipManager.instance.IsBGMEnabled ? "BGM：開" : "BGM：關");
        if (m_sfxStateText != null)
            m_sfxStateText.SetText(AudioClipManager.instance.IsSFXEnabled ? "音效：開" : "音效：關");
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
        Vector2 viewportOrigin = GetGameViewportOrigin();
        float dragAreaRight = viewportOrigin.x + m_windowSize.x - k_rightControlReserve;
        if (referenceScreenPosition.y >= viewportOrigin.y + m_windowSize.y - k_dragAreaHeight
            && referenceScreenPosition.y <= viewportOrigin.y + m_windowSize.y
            && referenceScreenPosition.x >= viewportOrigin.x + k_dragAreaLeftInset
            && referenceScreenPosition.x <= dragAreaRight)
            return true;

        float settingsButtonLeft = viewportOrigin.x + m_windowSize.x - k_settingsButtonMargin - k_settingsButtonSize;
        float settingsButtonBottom = viewportOrigin.y + m_windowSize.y - k_settingsButtonMargin - k_settingsButtonSize;
        return referenceScreenPosition.x >= settingsButtonLeft
               && referenceScreenPosition.y >= settingsButtonBottom;
    }

    private void ExpandForGallery()
    {
        m_galleryExpanded = true;
        m_galleryOnLeft = false;
        m_collapsedWindowPosition = m_windowPosition;

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        System.IntPtr windowHandle = GetUnityWindowHandle();
        if (windowHandle != System.IntPtr.Zero
            && GetWindowRect(windowHandle, out NativeRect windowRect)
            && TryGetMonitorWorkArea(windowHandle, out NativeRect workArea))
        {
            m_collapsedWindowPosition = new Vector2Int(windowRect.left, windowRect.top);
            int leftSpace = windowRect.left - workArea.left;
            int rightSpace = workArea.right - windowRect.right;
            m_galleryOnLeft = leftSpace > rightSpace;

            int combinedWidth = m_scaledWindowSize.x + m_scaledGallerySize.x;
            int desiredX = m_galleryOnLeft ? windowRect.left - m_scaledGallerySize.x : windowRect.left;
            int maxX = System.Math.Max(workArea.left, workArea.right - combinedWidth);
            int maxY = System.Math.Max(workArea.top, workArea.bottom - m_scaledGallerySize.y);
            m_windowPosition = new Vector2Int(
                Mathf.Clamp(desiredX, workArea.left, maxX),
                Mathf.Clamp(windowRect.top, workArea.top, maxY));
        }
#endif

        ApplyExpandedViewport();
        Screen.SetResolution(
            m_scaledWindowSize.x + m_scaledGallerySize.x,
            m_scaledGallerySize.y,
            false);
        m_lastScreenWidth = Screen.width;
        m_lastScreenHeight = Screen.height;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        ApplyNativeWindowBounds(
            m_windowPosition,
            m_scaledWindowSize.x + m_scaledGallerySize.x,
            m_scaledGallerySize.y);
#endif
        StartCoroutine(ReapplyViewportNextFrame());
    }

    private void CollapseGallery()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        System.IntPtr windowHandle = GetUnityWindowHandle();
        if (windowHandle != System.IntPtr.Zero && GetWindowRect(windowHandle, out NativeRect windowRect))
        {
            int gameLeft = m_galleryOnLeft ? m_scaledGallerySize.x : 0;
            m_collapsedWindowPosition = new Vector2Int(windowRect.left + gameLeft, windowRect.top);
        }
#endif

        m_galleryExpanded = false;
        m_galleryOnLeft = false;
        m_windowPosition = m_collapsedWindowPosition;
        ApplyCollapsedViewport();
        Screen.SetResolution(m_scaledWindowSize.x, m_scaledWindowSize.y, false);
        m_lastScreenWidth = Screen.width;
        m_lastScreenHeight = Screen.height;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        ApplyNativeWindowBounds(m_windowPosition, m_scaledWindowSize.x, m_scaledWindowSize.y);
#endif
        StartCoroutine(ReapplyViewportNextFrame());
    }

    private IEnumerator ReapplyViewportNextFrame()
    {
        yield return null;
        if (m_galleryExpanded)
            ApplyExpandedViewport();
        else
            ApplyCollapsedViewport();
    }

    private void ApplyExpandedViewport()
    {
        Vector2 gameOrigin = GetGameViewportOrigin();
        Vector2 gamePixelOrigin = GetGameViewportPixelOrigin();
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            EnsureTransparencyCamera(mainCamera);
            mainCamera.pixelRect = new Rect(
                gamePixelOrigin.x,
                gamePixelOrigin.y,
                m_scaledWindowSize.x,
                m_scaledWindowSize.y);
        }

        ApplyRuntimeControlsViewport(gameOrigin);
    }

    private void ApplyCollapsedViewport()
    {
        if (m_transparencyCamera != null)
            m_transparencyCamera.enabled = false;

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
            mainCamera.rect = new Rect(0f, 0f, 1f, 1f);

        ApplyRuntimeControlsViewport(Vector2.zero);
    }

    private Vector2 GetGameViewportOrigin()
    {
        if (m_galleryExpanded == false)
            return Vector2.zero;

        float x = m_galleryOnLeft ? k_galleryWidth : 0f;
        float y = Mathf.Max(0f, k_galleryHeight - m_windowSize.y) * 0.5f;
        return new Vector2(x, y);
    }

    private Vector2 GetGameViewportPixelOrigin()
    {
        if (m_galleryExpanded == false)
            return Vector2.zero;

        float x = m_galleryOnLeft ? m_scaledGallerySize.x : 0f;
        float y = Mathf.Max(0f, m_scaledGallerySize.y - m_scaledWindowSize.y) * 0.5f;
        return new Vector2(x, y);
    }

    private void EnsureTransparencyCamera(Camera mainCamera)
    {
        if (m_transparencyCamera == null)
        {
            GameObject cameraObject = new GameObject("DesktopPetTransparencyCamera", typeof(Camera));
            cameraObject.transform.SetParent(transform, false);
            m_transparencyCamera = cameraObject.GetComponent<Camera>();
            m_transparencyCamera.clearFlags = CameraClearFlags.SolidColor;
            m_transparencyCamera.backgroundColor = s_transparentBackgroundColor;
            m_transparencyCamera.cullingMask = 0;
            m_transparencyCamera.allowHDR = false;
            m_transparencyCamera.allowMSAA = false;
            m_transparencyCamera.useOcclusionCulling = false;
        }

        // 主相機只繪製 640×480 遊戲區域；先清除整個擴張視窗，避免未覆蓋區域保留不透明黑色。
        m_transparencyCamera.depth = mainCamera.depth - 1f;
        m_transparencyCamera.targetDisplay = mainCamera.targetDisplay;
        m_transparencyCamera.rect = new Rect(0f, 0f, 1f, 1f);
        m_transparencyCamera.enabled = true;
    }

    private void ApplyRuntimeControlsViewport(Vector2 origin)
    {
        if (m_runtimeControlsViewport != null)
            m_runtimeControlsViewport.anchoredPosition = origin;
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
        int displayWidth = Display.main.systemWidth;
        int displayHeight = Display.main.systemHeight;
        if (displayWidth <= 0 || displayHeight <= 0)
        {
            Resolution currentResolution = Screen.currentResolution;
            displayWidth = currentResolution.width;
            displayHeight = currentResolution.height;
        }

        m_windowScale = CalculateWindowScale(displayWidth, displayHeight);
        m_scaledWindowSize = ScaleSize(m_windowSize, m_windowScale);
        m_scaledGallerySize = ScaleSize(new Vector2Int(k_galleryWidth, k_galleryHeight), m_windowScale);
    }

    private static float CalculateWindowScale(int displayWidth, int displayHeight)
    {
        if (displayWidth <= 0 || displayHeight <= 0)
            return 1f;

        float widthScale = (float)displayWidth / k_referenceDisplayWidth;
        float heightScale = (float)displayHeight / k_referenceDisplayHeight;
        return Mathf.Min(widthScale, heightScale);
    }

    private static Vector2Int ScaleSize(Vector2Int referenceSize, float scale)
    {
        return new Vector2Int(
            Mathf.Max(1, Mathf.RoundToInt(referenceSize.x * scale)),
            Mathf.Max(1, Mathf.RoundToInt(referenceSize.y * scale)));
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
        exStyleValue &= ~k_wsExTransparent;
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

        int targetWidth = m_galleryExpanded
            ? m_scaledWindowSize.x + m_scaledGallerySize.x
            : m_scaledWindowSize.x;
        int targetHeight = m_galleryExpanded
            ? m_scaledGallerySize.y
            : m_scaledWindowSize.y;

        SetWindowPos(
            windowHandle,
            k_hwndTopMost,
            windowPosition.x,
            windowPosition.y,
            targetWidth,
            targetHeight,
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
    private const uint k_lwaAlpha = 0x00000002;
    private const uint k_swpNoMove = 0x0002;
    private const uint k_swpNoActivate = 0x0010;
    private const uint k_swpShowWindow = 0x0040;
    private const uint k_swpFrameChanged = 0x0020;
    private const uint k_monitorDefaultToNearest = 0x00000002;
    private const uint k_monitorInfoSize = 40;
    private const int k_wmNcLButtonDown = 0x00A1;
    private const int k_htCaption = 2;
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

    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(System.IntPtr hWnd, ref Margins margins);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern System.IntPtr SendMessage(System.IntPtr hWnd, int msg, System.IntPtr wParam, System.IntPtr lParam);
#endif
}
