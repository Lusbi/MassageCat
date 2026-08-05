using System;
using GameCore.Database;
using GameCore.Event;
using GameCore.UI;
using UnityEngine;
using UnityEngine.UI;

namespace MassageCat.Gallery
{
    internal sealed class GalleryButton : MonoBehaviour
    {
        private const float k_desktopButtonSize = 64f;
        private const float k_desktopIconSize = 44f;
        private const float k_desktopOffsetX = 72f;

        [SerializeField] private CanvasGroup m_canvasGroup;
        [SerializeField] private UIButton m_button;
        [SerializeField] private FlagReference m_flagReference;

        private EventListener m_eventListener;
        private Image m_iconImage;
        private bool m_isExpanded;

        internal void Initlization()
        {
            if (m_button != null)
            {
                m_button.onClicked -= OnButtonClicked;
                m_button.onClicked += OnButtonClicked;
            }

            if (m_eventListener == null)
            {
                m_eventListener = new EventListener();
                m_eventListener.AddListener<FlagUpdateEvent>(OnFlagUpdateEvent, true);
            }

            m_iconImage ??= GetComponentInChildren<Image>(true);
            RefreshIcon();
            RefreshUnlockState();
        }

        internal void SetExpanded(bool isExpanded)
        {
            m_isExpanded = isExpanded;
            RefreshIcon();
        }

        internal void ApplyDesktopPetLayout(RectTransform runtimeControlsViewport)
        {
            RectTransform buttonRect = (RectTransform)transform;
            buttonRect.SetParent(runtimeControlsViewport, false);
            buttonRect.anchorMin = new Vector2(0f, 1f);
            buttonRect.anchorMax = new Vector2(0f, 1f);
            buttonRect.pivot = new Vector2(0f, 1f);
            buttonRect.anchoredPosition = new Vector2(k_desktopOffsetX, 0f);
            buttonRect.sizeDelta = new Vector2(k_desktopButtonSize, k_desktopButtonSize);
            buttonRect.SetAsLastSibling();

            if (m_iconImage != null)
            {
                RectTransform iconRect = m_iconImage.rectTransform;
                iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.anchoredPosition = Vector2.zero;
                iconRect.sizeDelta = new Vector2(k_desktopIconSize, k_desktopIconSize);
            }
        }

        private void OnDestroy()
        {
            if (m_button != null)
                m_button.onClicked -= OnButtonClicked;

            m_eventListener?.Destroy();
            m_eventListener = null;
        }

        private void OnFlagUpdateEvent(FlagUpdateEvent eventData)
        {
            string unlockFlagKey = m_flagReference.GetKey();
            if (string.Equals(eventData.flagKey, unlockFlagKey, StringComparison.Ordinal) == false)
                return;

            RefreshUnlockState();
        }

        private void RefreshUnlockState()
        {
            if (m_canvasGroup == null || StorageManager.instance == null)
                return;

            string unlockFlagKey = m_flagReference.GetKey();
            bool isUnlocked = StorageManager.instance.StorageData.GetFlagStorageValue(unlockFlagKey) > 0;
            m_canvasGroup.alpha = isUnlocked ? 1f : 0f;
            m_canvasGroup.interactable = isUnlocked;
            m_canvasGroup.blocksRaycasts = isUnlocked;
        }

        private void RefreshIcon()
        {
            if (m_iconImage == null)
                return;

            Sprite sprite = m_isExpanded ? MassageCatUiArt.GalleryClose : MassageCatUiArt.GalleryOpen;
            if (sprite == null)
                return;

            m_iconImage.sprite = sprite;
            m_iconImage.color = Color.white;
            m_iconImage.preserveAspect = true;
            MassageCatUiArt.ApplyIconOutline(m_iconImage);
        }

        private static void OnButtonClicked(UIButton button)
        {
            if (GalleryBootstrap.IntegratedInstance != null
                && GalleryBootstrap.IntegratedInstance.ToggleIntegrated())
                return;

            Debug.LogWarning("圖鑑介面尚未完成初始化。");
        }
    }
}
