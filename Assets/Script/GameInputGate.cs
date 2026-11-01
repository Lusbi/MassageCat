using System;
using System.Collections.Generic;
using GameCore.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 集中管理開始遊玩的長按解鎖，以及解鎖後的點擊、拖曳與鍵盤輸入。
/// </summary>
internal sealed class GameInputGate : MonoBehaviour
{
    private enum GateState : byte
    {
        Locked,
        TrackingUnlockHold,
        Unlocked,
        DraggingWindow,
    }

    private const float k_unlockHoldDuration = 1.5f;
    private const float k_dragThresholdLogicalPixels = 8f;
    private const float k_unlockHintFadeDuration = 0.25f;
    private const int k_overlaySortingOrder = 2000;
    private const int k_ringSize = 72;
    private const int k_ringTextureSize = 96;
    private const float k_ringOuterRadius = 44f;
    private const float k_ringInnerRadius = 34f;
    private const int k_virtualKeyApps = 0x5D;

    internal static event Action<string> OnGameplayInput;

    // 圖鑑覆蓋主畫面時暫停遊戲輸入，避免圖鑑操作觸發治療或拼字判定。
    private static bool s_gameplayInputPaused;

    private GateState m_state = GateState.Locked;
    private bool m_requiresPointerInsideGameWindow = true;
    private bool m_pointerDown;
    private bool m_pointerStartedOnUi;
    private bool m_pointerExceededDragThreshold;
    private Vector2 m_pointerStartPosition;
    // 重用射線結果與事件資料，避免每次點擊配置記憶體。
    private readonly List<RaycastResult> m_uiRaycastResults = new List<RaycastResult>(8);
    private PointerEventData m_pointerEventData;
    private float m_unlockHoldStartTime;
    private float m_hintFadeTimer;
    private CanvasGroup m_overlayCanvasGroup;
    private GameObject m_overlayRoot;
    private Image m_progressRing;
    private TextMeshProUGUI m_hintText;
    private Texture2D m_ringTexture;
    private Sprite m_ringSprite;

    internal static void SetGameplayInputPaused(bool isPaused)
    {
        s_gameplayInputPaused = isPaused;
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        CreateUnlockOverlay();
    }

    private void OnEnable()
    {
#if UNITY_STANDALONE_WIN
        WindowsInputHookManager.OnGlobalKeyDown += OnGlobalKeyDown;
        WindowsInputHookManager.OnGlobalPointerDown += OnGlobalPointerDown;
        WindowsInputHookManager.OnGlobalPointerUp += OnGlobalPointerUp;
        WindowsInputHookManager.OnGlobalPointerMove += OnGlobalPointerMove;
#endif
    }

    private void OnDisable()
    {
#if UNITY_STANDALONE_WIN
        WindowsInputHookManager.OnGlobalKeyDown -= OnGlobalKeyDown;
        WindowsInputHookManager.OnGlobalPointerDown -= OnGlobalPointerDown;
        WindowsInputHookManager.OnGlobalPointerUp -= OnGlobalPointerUp;
        WindowsInputHookManager.OnGlobalPointerMove -= OnGlobalPointerMove;
#endif
    }

    private void OnDestroy()
    {
        if (m_ringSprite != null)
            Destroy(m_ringSprite);
        if (m_ringTexture != null)
            Destroy(m_ringTexture);
    }

    private void Update()
    {
        if (m_state == GateState.TrackingUnlockHold)
            UpdateUnlockHold();

        if (m_state == GateState.Unlocked && m_hintFadeTimer > 0f)
            UpdateUnlockHintFade();

#if !UNITY_STANDALONE_WIN
        UpdateFallbackInput();
#endif

        if (s_gameplayInputPaused == false
            && m_state == GateState.Unlocked
            && Application.isFocused
#if !UNITY_STANDALONE_WIN
            && Input.GetKeyDown(KeyCode.Menu)
#else
            && false
#endif
            )
        {
            GameSettingsMenu.ToggleFromShortcut();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus == false)
            CancelTrackingIfNeeded();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            CancelTrackingIfNeeded();
    }

#if UNITY_STANDALONE_WIN
    private void OnGlobalKeyDown(int virtualKey)
    {
        if (s_gameplayInputPaused || m_state != GateState.Unlocked || Application.isFocused == false)
            return;

        if (virtualKey == k_virtualKeyApps)
        {
            GameSettingsMenu.ToggleFromShortcut();
            return;
        }

        if (virtualKey is >= 0x41 and <= 0x5A)
            OnGameplayInput?.Invoke(char.ToLowerInvariant((char)virtualKey).ToString());
        else
            OnGameplayInput?.Invoke(null);
    }

    private void OnGlobalPointerDown(WindowsInputHookManager.PointerEvent pointerEvent)
    {
        if (pointerEvent.Button != 0 || Application.isFocused == false)
            return;

        BeginPointer(Input.mousePosition);
    }

    private void OnGlobalPointerUp(WindowsInputHookManager.PointerEvent pointerEvent)
    {
        if (pointerEvent.Button != 0)
            return;

        EndPointer(Input.mousePosition);
    }

    private void OnGlobalPointerMove(WindowsInputHookManager.PointerEvent pointerEvent)
    {
        if (m_pointerDown)
            TrackPointerMovement(Input.mousePosition);
    }
#endif

#if !UNITY_STANDALONE_WIN
    private void UpdateFallbackInput()
    {
        if (Input.GetMouseButtonDown(0))
            BeginPointer(Input.mousePosition);
        if (m_pointerDown && Input.GetMouseButton(0))
            TrackPointerMovement(Input.mousePosition);
        if (Input.GetMouseButtonUp(0))
            EndPointer(Input.mousePosition);
    }
#endif

    private void BeginPointer(Vector2 screenPosition)
    {
        if (s_gameplayInputPaused)
            return;

        bool isInsideGameWindow = IsPointerInsideGameWindow(screenPosition);
        if (m_pointerDown || (m_requiresPointerInsideGameWindow && isInsideGameWindow == false))
            return;

        m_pointerDown = true;
        m_pointerStartedOnUi = IsPointerOverInteractiveControl(screenPosition);
        m_pointerExceededDragThreshold = false;
        m_pointerStartPosition = screenPosition;

        if (m_state == GateState.Locked)
        {
            m_state = GateState.TrackingUnlockHold;
            m_unlockHoldStartTime = Time.unscaledTime;
            SetUnlockProgress(0f);
        }
    }

    private void EndPointer(Vector2 screenPosition)
    {
        if (s_gameplayInputPaused)
        {
            m_pointerDown = false;
            m_pointerStartedOnUi = false;
            m_pointerExceededDragThreshold = false;
            return;
        }

        if (m_pointerDown == false)
            return;

        bool isInsideGameWindow = IsPointerInsideGameWindow(screenPosition);
        bool shouldDispatchGameplayInput = m_state == GateState.Unlocked
                                         && m_pointerStartedOnUi == false
                                         && m_pointerExceededDragThreshold == false
                                         && (m_requiresPointerInsideGameWindow == false || isInsideGameWindow);

        if (m_state == GateState.TrackingUnlockHold)
        {
            CancelUnlockHold();
        }
        else if (m_state == GateState.DraggingWindow)
        {
            m_state = GateState.Unlocked;
        }
        else if (shouldDispatchGameplayInput)
        {
            OnGameplayInput?.Invoke(null);
        }

        m_pointerDown = false;
        m_pointerStartedOnUi = false;
        m_pointerExceededDragThreshold = false;
    }

    private void TrackPointerMovement(Vector2 screenPosition)
    {
        if (m_pointerDown == false)
            return;

        if (m_requiresPointerInsideGameWindow && IsPointerInsideGameWindow(screenPosition) == false)
        {
            CancelTrackingIfNeeded();
            return;
        }

        if (GetLogicalPointerDistance(screenPosition) <= k_dragThresholdLogicalPixels)
            return;

        m_pointerExceededDragThreshold = true;
        if (m_state == GateState.TrackingUnlockHold)
        {
            CancelUnlockHold();
            return;
        }

        if (m_state == GateState.Unlocked && m_pointerStartedOnUi == false && IsDesktopPetMode() && DesktopPetWindowController.TryBeginWindowDrag())
            m_state = GateState.DraggingWindow;
    }

    private void UpdateUnlockHold()
    {
        if (m_pointerDown == false || Application.isFocused == false || Input.GetMouseButton(0) == false || IsPointerInsideGameWindow(Input.mousePosition) == false)
        {
            CancelUnlockHold();
            return;
        }

        TrackPointerMovement(Input.mousePosition);
        if (m_state != GateState.TrackingUnlockHold)
            return;

        float progress = Mathf.Clamp01((Time.unscaledTime - m_unlockHoldStartTime) / k_unlockHoldDuration);
        SetUnlockProgress(progress);
        if (progress < 1f)
            return;

        m_state = GateState.Unlocked;
        m_requiresPointerInsideGameWindow = false;
        m_hintFadeTimer = k_unlockHintFadeDuration;
        if (m_overlayCanvasGroup != null)
            m_overlayCanvasGroup.blocksRaycasts = false;
    }

    private void CancelTrackingIfNeeded()
    {
        if (m_state == GateState.TrackingUnlockHold)
            CancelUnlockHold();
        else if (m_state == GateState.DraggingWindow)
            m_state = GateState.Unlocked;

        m_pointerDown = false;
        m_pointerStartedOnUi = false;
        m_pointerExceededDragThreshold = false;
    }

    private void CancelUnlockHold()
    {
        m_state = GateState.Locked;
        m_requiresPointerInsideGameWindow = true;
        SetUnlockProgress(0f);
    }

    private void UpdateUnlockHintFade()
    {
        m_hintFadeTimer -= Time.unscaledDeltaTime;
        float alpha = Mathf.Clamp01(m_hintFadeTimer / k_unlockHintFadeDuration);
        if (m_overlayCanvasGroup != null)
            m_overlayCanvasGroup.alpha = alpha;

        if (m_hintFadeTimer > 0f)
            return;

        if (m_overlayRoot != null)
            m_overlayRoot.SetActive(false);
    }

    private bool IsPointerOverInteractiveControl(Vector2 screenPosition)
    {
        if (DesktopPetWindowController.IsPointerInRuntimeControlArea())
            return true;

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
            return false;

        m_pointerEventData ??= new PointerEventData(eventSystem);
        m_pointerEventData.Reset();
        m_pointerEventData.position = screenPosition;

        m_uiRaycastResults.Clear();
        eventSystem.RaycastAll(m_pointerEventData, m_uiRaycastResults);

        bool isOverInteractiveControl = false;
        for (int i = 0, count = m_uiRaycastResults.Count; i < count; i++)
        {
            GameObject hitObject = m_uiRaycastResults[i].gameObject;
            if (hitObject == null)
                continue;

            if (hitObject.GetComponent<UIButton>() != null
                || hitObject.GetComponent<Button>() != null
                || hitObject.GetComponent<UIEventListener>() != null)
            {
                isOverInteractiveControl = true;
                break;
            }
        }

        m_uiRaycastResults.Clear();
        return isOverInteractiveControl;
    }

    private static bool IsPointerInsideGameWindow(Vector2 screenPosition)
    {
        return screenPosition.x >= 0f && screenPosition.x <= Screen.width && screenPosition.y >= 0f && screenPosition.y <= Screen.height;
    }

    private float GetLogicalPointerDistance(Vector2 screenPosition)
    {
        Canvas canvas = UIManager.instance != null ? UIManager.instance.GetComponent<Canvas>() : null;
        float scaleFactor = canvas != null ? canvas.scaleFactor : 1f;
        return Vector2.Distance(screenPosition, m_pointerStartPosition) / Mathf.Max(0.0001f, scaleFactor);
    }

    private static bool IsDesktopPetMode()
    {
        return GameProcess.Instance != null && GameProcess.Instance.DesktopPetMode;
    }

    private void CreateUnlockOverlay()
    {
        m_overlayRoot = new GameObject("GameInputUnlockOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        m_overlayRoot.transform.SetParent(transform, false);

        Canvas canvas = m_overlayRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = k_overlaySortingOrder;

        CanvasScaler canvasScaler = m_overlayRoot.GetComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(480f, 320f);
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.matchWidthOrHeight = 0.5f;

        m_overlayCanvasGroup = m_overlayRoot.GetComponent<CanvasGroup>();
        RectTransform rootRect = m_overlayRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image dimmer = m_overlayRoot.AddComponent<Image>();
        dimmer.color = new Color(0f, 0f, 0f, 0.42f);
        dimmer.raycastTarget = true;

        RectTransform ringRoot = CreateRectTransform("UnlockProgressRing", rootRect);
        ringRoot.anchorMin = new Vector2(0.5f, 0.5f);
        ringRoot.anchorMax = new Vector2(0.5f, 0.5f);
        ringRoot.anchoredPosition = new Vector2(0f, 22f);
        ringRoot.sizeDelta = new Vector2(k_ringSize, k_ringSize);

        m_progressRing = ringRoot.gameObject.AddComponent<Image>();
        m_progressRing.sprite = CreateRingSprite();
        m_progressRing.type = Image.Type.Filled;
        m_progressRing.fillMethod = Image.FillMethod.Radial360;
        m_progressRing.fillOrigin = (int)Image.Origin360.Top;
        m_progressRing.fillClockwise = true;
        m_progressRing.color = new Color(1f, 0.9f, 0.38f, 1f);
        m_progressRing.raycastTarget = false;
        SetUnlockProgress(0f);

        m_hintText = CreateText("UnlockHint", rootRect, "長按 1.5 秒開始遊玩", 20f);
        RectTransform textRect = m_hintText.rectTransform;
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = new Vector2(0f, -36f);
        textRect.sizeDelta = new Vector2(360f, 36f);
    }

    private Sprite CreateRingSprite()
    {
        m_ringTexture = new Texture2D(k_ringTextureSize, k_ringTextureSize, TextureFormat.RGBA32, false)
        {
            name = "GameInputUnlockRing",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };

        Color32[] pixels = new Color32[k_ringTextureSize * k_ringTextureSize];
        float center = (k_ringTextureSize - 1) * 0.5f;
        for (int y = 0; y < k_ringTextureSize; y++)
        {
            for (int x = 0; x < k_ringTextureSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (distance >= k_ringInnerRadius && distance <= k_ringOuterRadius)
                    pixels[y * k_ringTextureSize + x] = new Color32(255, 255, 255, 255);
            }
        }
        m_ringTexture.SetPixels32(pixels);
        m_ringTexture.Apply(false, true);
        m_ringSprite = Sprite.Create(m_ringTexture, new Rect(0f, 0f, k_ringTextureSize, k_ringTextureSize), new Vector2(0.5f, 0.5f), k_ringTextureSize);
        return m_ringSprite;
    }

    private static RectTransform CreateRectTransform(string objectName, Transform parent)
    {
        GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        return rectTransform;
    }

    private static TextMeshProUGUI CreateText(string objectName, Transform parent, string content, float fontSize)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        text.SetText(content);
        return text;
    }

    private void SetUnlockProgress(float value)
    {
        if (m_progressRing != null)
            m_progressRing.fillAmount = value;
    }
}
