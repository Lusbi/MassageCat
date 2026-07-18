using System.Collections;
using GameCore.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class DesktopPetWindowController : MonoBehaviour
{
    private static readonly Color s_transparencyKeyColor = new Color(1f, 0f, 1f, 0f);
    private static DesktopPetWindowController s_instance;

    [SerializeField] private Vector2Int m_windowSize = new Vector2Int(640, 480);
    [SerializeField] private Vector2Int m_windowPosition = Vector2Int.zero;
    [SerializeField] private int m_targetFrameRate = 30;

    private const float k_dragAreaHeight = 56f;
    private const float k_dragAreaLeftInset = 112f;
    private const float k_closeButtonSize = 32f;
    private const float k_closeButtonMargin = 8f;
    private const int k_controlSortingOrder = 1000;

    private float m_reapplyTimer;
    private GameObject m_runtimeControlsRoot;

    public static bool IsPointerInRuntimeControlArea()
    {
        return s_instance != null && s_instance.ContainsRuntimeControlArea(Input.mousePosition);
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        s_instance = this;
        ApplyRuntimeSettings();
        ApplyCameraSettings();
    }

    private void Start()
    {
        Screen.SetResolution(m_windowSize.x, m_windowSize.y, false);

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        StartCoroutine(ApplyWindowWhenReady());
#endif
    }

    private void Update()
    {
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
            return;
        }

        if (UIManager.instance == null)
            return;

        RectTransform root = CreateRectTransform("DesktopPetRuntimeControls", UIManager.instance.transform);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        root.SetAsLastSibling();
        m_runtimeControlsRoot = root.gameObject;

        Canvas controlCanvas = m_runtimeControlsRoot.AddComponent<Canvas>();
        controlCanvas.overrideSorting = true;
        controlCanvas.sortingOrder = k_controlSortingOrder;
        m_runtimeControlsRoot.AddComponent<GraphicRaycaster>();

        CreateDragArea(root);
        CreateCloseButton(root);
    }

    private void CreateDragArea(RectTransform root)
    {
        RectTransform dragArea = CreateRectTransform("DesktopPetDragArea", root);
        dragArea.anchorMin = new Vector2(0f, 1f);
        dragArea.anchorMax = new Vector2(1f, 1f);
        dragArea.pivot = new Vector2(0.5f, 1f);
        dragArea.offsetMin = new Vector2(k_dragAreaLeftInset, -k_dragAreaHeight);
        dragArea.offsetMax = Vector2.zero;

        Image image = dragArea.gameObject.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = true;

        UIEventListener listener = UIEventListener.Get(dragArea.gameObject);
        listener.onDown = OnDragAreaPointerDown;
    }

    private void CreateCloseButton(RectTransform root)
    {
        RectTransform closeButton = CreateRectTransform("DesktopPetCloseButton", root);
        closeButton.anchorMin = new Vector2(1f, 1f);
        closeButton.anchorMax = new Vector2(1f, 1f);
        closeButton.pivot = new Vector2(1f, 1f);
        closeButton.anchoredPosition = new Vector2(-k_closeButtonMargin, -k_closeButtonMargin);
        closeButton.sizeDelta = new Vector2(k_closeButtonSize, k_closeButtonSize);

        Image image = closeButton.gameObject.AddComponent<Image>();
        image.color = new Color(0.05f, 0.05f, 0.05f, 0.72f);
        image.raycastTarget = true;

        TextMeshProUGUI text = new GameObject("Text").AddComponent<TextMeshProUGUI>();
        RectTransform textRect = text.rectTransform;
        textRect.SetParent(closeButton, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 20f;
        text.text = "X";
        text.color = Color.white;
        text.raycastTarget = false;

        UIEventListener listener = UIEventListener.Get(closeButton.gameObject);
        listener.onClick = OnCloseButtonClicked;
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

    private void OnCloseButtonClicked(GameObject go)
    {
        Application.Quit();
    }

    private bool ContainsRuntimeControlArea(Vector3 screenPosition)
    {
        if (m_runtimeControlsRoot == null)
            return false;

        if (screenPosition.y >= Screen.height - k_dragAreaHeight)
            return true;

        float closeButtonLeft = Screen.width - k_closeButtonMargin - k_closeButtonSize;
        float closeButtonBottom = Screen.height - k_closeButtonMargin - k_closeButtonSize;
        return screenPosition.x >= closeButtonLeft && screenPosition.y >= closeButtonBottom;
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

    private void ApplyCameraSettings()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return;

        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = s_transparencyKeyColor;
    }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
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

        int colorKey = ColorToColorRef(s_transparencyKeyColor);
        SetLayeredWindowAttributes(windowHandle, colorKey, 0, k_lwaColorKey);
        DwmExtendFrameIntoClientArea(windowHandle, ref s_fullWindowMargins);

        uint flags = k_swpNoActivate | k_swpFrameChanged | k_swpShowWindow;
        if (resize == false)
            flags |= k_swpNoMove | k_swpNoSize;

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
            m_windowSize.x,
            m_windowSize.y,
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

    private static int ColorToColorRef(Color color)
    {
        int r = Mathf.RoundToInt(color.r * 255f);
        int g = Mathf.RoundToInt(color.g * 255f);
        int b = Mathf.RoundToInt(color.b * 255f);
        return r | (g << 8) | (b << 16);
    }

    private Vector2Int GetCenteredWindowPosition(System.IntPtr windowHandle)
    {
        if (TryGetMonitorWorkArea(windowHandle, out NativeRect workArea) == false)
            return m_windowPosition;

        int workAreaWidth = workArea.right - workArea.left;
        int workAreaHeight = workArea.bottom - workArea.top;
        int x = workArea.left + System.Math.Max(0, (workAreaWidth - m_windowSize.x) / 2);
        int y = workArea.top + System.Math.Max(0, (workAreaHeight - m_windowSize.y) / 2);
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
    private const uint k_lwaColorKey = 0x00000001;
    private const uint k_swpNoSize = 0x0001;
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

    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(System.IntPtr hWnd, ref Margins margins);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern System.IntPtr SendMessage(System.IntPtr hWnd, int msg, System.IntPtr wParam, System.IntPtr lParam);
#endif
}
