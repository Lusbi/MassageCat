using System;
using GameCore.Database;
using GameCore.Event;
using GameCore.UI;
using UnityEngine;

namespace MassageCat.Gallery
{
    internal sealed class GalleryButton : MonoBehaviour
    {
        [SerializeField] private CanvasGroup m_canvasGroup;
        [SerializeField] private UIButton m_button;
        [SerializeField] private FlagReference m_flagReference;

        private EventListener m_eventListener;

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

            RefreshUnlockState();
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

        private static void OnButtonClicked(UIButton button)
        {
            if (GalleryBootstrap.IntegratedInstance != null
                && GalleryBootstrap.IntegratedInstance.ToggleIntegrated())
                return;

            Debug.LogWarning("圖鑑介面尚未完成初始化。");
        }
    }
}
