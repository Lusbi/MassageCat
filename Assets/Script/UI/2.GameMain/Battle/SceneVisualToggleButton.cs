using System;
using GameCore.Audio;
using GameCore.UI;
using UnityEngine;
using UnityEngine.UI;

public sealed class SceneVisualToggleButton : MonoBehaviour
{
    private const float k_buttonSize = 64f;
    private const float k_iconSize = 44f;
    private static readonly Vector2 s_desktopPosition = new Vector2(-80f, -8f);

    [SerializeField] private UIButton m_button;
    [SerializeField] private GameObject m_visibleIcon;
    [SerializeField] private GameObject m_hiddenIcon;

    private Action<bool> m_stateChanged;
    private bool m_isVisible = true;

    internal static SceneVisualToggleButton CreateRuntime(Transform parent)
    {
        GameObject buttonObject = new GameObject(
            "SceneVisualToggle",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(UIButton),
            typeof(SceneVisualToggleButton));
        buttonObject.layer = parent.gameObject.layer;

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.SetParent(parent, false);
        buttonRect.anchorMin = Vector2.one;
        buttonRect.anchorMax = Vector2.one;
        buttonRect.pivot = Vector2.one;
        buttonRect.anchoredPosition = s_desktopPosition;
        buttonRect.sizeDelta = new Vector2(k_buttonSize, k_buttonSize);
        buttonRect.SetAsLastSibling();

        Image backgroundImage = buttonObject.GetComponent<Image>();
        backgroundImage.color = new Color(0f, 0f, 0f, 0f);
        backgroundImage.raycastTarget = true;

        SceneVisualToggleButton toggleButton = buttonObject.GetComponent<SceneVisualToggleButton>();
        toggleButton.m_button = buttonObject.GetComponent<UIButton>();
        toggleButton.m_visibleIcon = CreateVisibilityIcon(
            "VisibleIcon",
            buttonRect,
            MassageCatUiArt.SceneVisible,
            false);
        toggleButton.m_hiddenIcon = CreateVisibilityIcon(
            "HiddenIcon",
            buttonRect,
            MassageCatUiArt.SceneHidden,
            true);
        toggleButton.m_hiddenIcon.SetActive(false);
        return toggleButton;
    }

    internal void ApplyDesktopPetLayout(RectTransform runtimeControlsViewport)
    {
        RectTransform buttonRect = (RectTransform)transform;
        buttonRect.SetParent(runtimeControlsViewport, false);
        buttonRect.anchorMin = Vector2.one;
        buttonRect.anchorMax = Vector2.one;
        buttonRect.pivot = Vector2.one;
        buttonRect.anchoredPosition = s_desktopPosition;
        buttonRect.sizeDelta = new Vector2(k_buttonSize, k_buttonSize);
        buttonRect.SetAsLastSibling();
    }

    private static GameObject CreateVisibilityIcon(
        string objectName,
        Transform parent,
        Sprite sprite,
        bool crossedOut)
    {
        GameObject iconObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        iconObject.layer = parent.gameObject.layer;

        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.SetParent(parent, false);
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(k_iconSize, k_iconSize);

        Image iconImage = iconObject.GetComponent<Image>();
        iconImage.raycastTarget = false;
        if (sprite != null)
        {
            iconImage.sprite = sprite;
            iconImage.color = Color.white;
            iconImage.preserveAspect = true;
            MassageCatUiArt.ApplyIconOutline(iconImage);
            return iconObject;
        }

        iconImage.color = new Color(0f, 0f, 0f, 0f);

        CreateIconLine("UpperLeft", iconRect, new Vector2(-6.5f, 2f), new Vector2(6.5f, 2f), 38f);
        CreateIconLine("UpperCenter", iconRect, new Vector2(0f, 4f), new Vector2(8f, 2f), 0f);
        CreateIconLine("UpperRight", iconRect, new Vector2(6.5f, 2f), new Vector2(6.5f, 2f), -38f);
        CreateIconLine("LowerRight", iconRect, new Vector2(6.5f, -2f), new Vector2(6.5f, 2f), 38f);
        CreateIconLine("LowerCenter", iconRect, new Vector2(0f, -4f), new Vector2(8f, 2f), 0f);
        CreateIconLine("LowerLeft", iconRect, new Vector2(-6.5f, -2f), new Vector2(6.5f, 2f), -38f);
        CreateIconLine("Pupil", iconRect, Vector2.zero, new Vector2(4f, 4f), 0f);

        if (crossedOut)
            CreateIconLine("Slash", iconRect, Vector2.zero, new Vector2(24f, 2.5f), -45f);

        return iconObject;
    }

    private static void CreateIconLine(
        string objectName,
        Transform parent,
        Vector2 anchoredPosition,
        Vector2 size,
        float rotation)
    {
        GameObject lineObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        lineObject.layer = parent.gameObject.layer;

        RectTransform lineRect = lineObject.GetComponent<RectTransform>();
        lineRect.SetParent(parent, false);
        lineRect.anchorMin = new Vector2(0.5f, 0.5f);
        lineRect.anchorMax = new Vector2(0.5f, 0.5f);
        lineRect.anchoredPosition = anchoredPosition;
        lineRect.sizeDelta = size;
        lineRect.localEulerAngles = new Vector3(0f, 0f, rotation);

        Image lineImage = lineObject.GetComponent<Image>();
        lineImage.color = Color.white;
        lineImage.raycastTarget = false;
    }

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
