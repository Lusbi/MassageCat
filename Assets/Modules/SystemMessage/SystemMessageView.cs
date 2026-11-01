using System;
using GameCore;
using GameCore.Event;
using GameCore.UI;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Modules.SystemMessage
{

    public struct SystemMessageEvent : IEventBase
    {
        public string Title;
        public string Content;
        public Action OnConfirm;

        public SystemMessageEvent(string title, string content, Action onConfirm)
        {
            Title = title;
            Content = content;
            OnConfirm = onConfirm;
        }
    }

    public class SystemMessageView : MonoBehaviour, IInitlization
    {
        private const float k_fadeDuration = 0.5f;

        [FormerlySerializedAs("viewCanvasGroup")]
        [SerializeField] private CanvasGroup m_viewCanvasGroup;
        [FormerlySerializedAs("Title")]
        [SerializeField] private TextMeshProUGUI m_title;
        [FormerlySerializedAs("Content")]
        [SerializeField] private TextMeshProUGUI m_content;
        [FormerlySerializedAs("BtnClick")]
        [SerializeField] private UIButton m_confirmButton;

        private EventListener m_eventListener;
        private Action m_onConfirm;
        private Tween m_visibilityTween;

        public void Initlization(Action callBack = null)
        {
            StopVisibilityTween();
            m_eventListener?.Destroy();
            m_eventListener = new EventListener();
            m_eventListener.AddListener<SystemMessageEvent>(OnSystemMessageEvent);

            // 此 Prefab 會掛在僅渲染 UI Layer 的相機畫布下，確保所有子節點不會被相機剔除。
            if (transform.parent != null)
                SetLayerRecursively(transform, transform.parent.gameObject.layer);

            if (m_viewCanvasGroup == null)
                m_viewCanvasGroup = GetComponent<CanvasGroup>();

            // 初始化為隱藏狀態，避免第一幀攔截背景輸入。
            if (m_viewCanvasGroup != null)
            {
                m_viewCanvasGroup.alpha = 0f;
                m_viewCanvasGroup.blocksRaycasts = false;
            }

            if (m_confirmButton != null)
            {
                m_confirmButton.onClicked -= OnBtnClick;
                m_confirmButton.onClicked += OnBtnClick;
            }

            callBack?.Invoke();
        }

        private void OnDestroy()
        {
            StopVisibilityTween();
            m_eventListener?.Destroy();
            m_eventListener = null;

            if (m_confirmButton != null)
                m_confirmButton.onClicked -= OnBtnClick;

            UnityMainThreadDispatcher.SetInputListenerState(true);
        }

        public void RegisterInfo(string title, string content, Action callBack)
        {
            if (m_title != null)
                m_title.SetText(title);
            if (m_content != null)
                m_content.SetText(content);

            m_onConfirm = callBack;
        }

        public void Show()
        {
            StopVisibilityTween();
            if (m_viewCanvasGroup != null)
            {
                m_viewCanvasGroup.blocksRaycasts = true;
                m_visibilityTween = Tween.Alpha(m_viewCanvasGroup, 1f, k_fadeDuration);
            }

            // 彈窗顯示期間停用全域鍵盤輸入。
            UnityMainThreadDispatcher.SetInputListenerState(false);
        }

        public void Hide()
        {
            StopVisibilityTween();
            if (m_viewCanvasGroup != null)
            {
                m_visibilityTween = Tween.Alpha(m_viewCanvasGroup, 0f, k_fadeDuration)
                    .OnComplete(this, static view => view.OnHideCompleted(), false);
                return;
            }

            OnHideCompleted();
        }

        private void OnSystemMessageEvent(SystemMessageEvent evt)
        {
            RegisterInfo(evt.Title, evt.Content, evt.OnConfirm);
            Show();
        }

        private void OnHideCompleted()
        {
            if (m_viewCanvasGroup != null)
                m_viewCanvasGroup.blocksRaycasts = false;

            UnityMainThreadDispatcher.SetInputListenerState(true);
        }

        private void StopVisibilityTween()
        {
            if (m_visibilityTween.isAlive)
                m_visibilityTween.Stop();
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            for (int i = 0, count = root.childCount; i < count; i++)
                SetLayerRecursively(root.GetChild(i), layer);
        }

        private void OnBtnClick(UIButton btn)
        {
            m_onConfirm?.Invoke();
            Hide();
        }
    }
}
