using System;
using GameCore;
using GameCore.Event;
using GameCore.UI;
using PrimeTween;
using TMPro;
using UnityEngine;

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
        [SerializeField] private CanvasGroup viewCanvasGroup;
        [SerializeField] private TextMeshProUGUI Title;
        [SerializeField] private TextMeshProUGUI Content;
        [SerializeField] private UIButton BtnClick;

        // 負責接收訊息事件
        private EventListener eventListener;

        private Action m_onConfirm;

        public void Initlization(Action callBack = null)
        {
            eventListener = new EventListener();
            eventListener.AddListener<SystemMessageEvent>((evt)=>
            {
                RegisterInfo(evt.Title, evt.Content, evt.OnConfirm);
                Show();
            });

            if (viewCanvasGroup == null) viewCanvasGroup = GetComponent<CanvasGroup>();

            // 預設關閉
            if (viewCanvasGroup != null)
            {
                viewCanvasGroup.alpha = 0f;
                viewCanvasGroup.blocksRaycasts = false;
            }

            if (BtnClick != null)
            {
                BtnClick.onClicked += OnBtnClick;
            }
        }

        private void OnDestroy()
        {
            if (eventListener != null)
            {
                eventListener.Destroy();
                eventListener = null;
            }

            if (BtnClick != null)
            {
                BtnClick.onClicked -= OnBtnClick;
            }
        }

        /// <summary>
        /// 初始化時註冊接收資訊
        /// </summary>
        /// <param name="title"></param>
        /// <param name="content"></param>
        /// <param name="callBack"></param>
        public void RegisterInfo(string title, string content, Action callBack)
        {
            if (Title != null) Title.text = title;
            if (Content != null) Content.text = content;
            m_onConfirm = callBack;
        }

        public void Show()
        {
            if (viewCanvasGroup != null)
            {
                // 0.5秒，從0>1
                Tween.Alpha(viewCanvasGroup, 1f, 0.5f);
                viewCanvasGroup.blocksRaycasts = true;
            }

            // 1.3 開啟時，暫停監聽打字發送
            UnityMainThreadDispatcher.SetInputListenerState(false);
        }

        public void Hide()
        {
            if (viewCanvasGroup != null)
            {
                // 0.5秒，從1>0
                Tween.Alpha(viewCanvasGroup, 0f, 0.5f).OnComplete(() =>
                {
                    viewCanvasGroup.blocksRaycasts = false;
                });
            }

            // 關閉時，恢復監聽打字發送
            UnityMainThreadDispatcher.SetInputListenerState(true);
        }

        private void OnBtnClick(UIButton btn)
        {
            m_onConfirm?.Invoke();
            Hide();
        }
    }
}
