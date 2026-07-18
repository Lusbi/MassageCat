using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore.UI
{
    [RequireComponent(typeof(GraphicRaycaster))]
    public class UIManager : MonoSingleton<UIManager>, IInitlization
    {
        private Canvas m_canvas;
        private CanvasScaler m_canvasScaler;
        private CanvasRenderer m_canvasRenderer;

        private GraphicRaycaster m_graphicRaycaster;
        private long m_stateID;
        private Dictionary<int, UIBase> m_registeredUIs = new Dictionary<int, UIBase>();
        private UILoadProcess m_uiLoadProcess;

        public void Initlization(System.Action callBack = null)
        {
            m_graphicRaycaster = gameObject.AddComponent<GraphicRaycaster>();
            m_canvasRenderer = gameObject.AddComponent<CanvasRenderer>();
            m_canvasScaler = gameObject.AddComponent<CanvasScaler>();
            m_canvas = gameObject.GetComponent<Canvas>();
            foreach (var kvp in m_registeredUIs)
            {
                kvp.Value.Destory();
            }
            m_registeredUIs.Clear();

            m_canvas.renderMode = RenderMode.ScreenSpaceCamera;
            m_canvas.worldCamera = Camera.main;

            m_canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            m_canvasScaler.referenceResolution = UIUtils.DESIGNER_SIZE;

            m_uiLoadProcess = new UILoadProcess(callBack);
        }

        public void Registered(UIBase uIBase)
        {
            if (m_registeredUIs.ContainsKey(uIBase.identifier))
            {
                return;
            }

            m_registeredUIs.Add(uIBase.identifier, uIBase);
        }
        public void UnRegistered(UIBase uIBase)
        {
            if (m_registeredUIs.ContainsKey(uIBase.identifier) == false)
            {
                return;
            }

            m_registeredUIs.Remove(uIBase.identifier);
        }

        public void Update()
        {
            float deltaTime = Time.deltaTime;
            foreach (UIBase uIBase in m_registeredUIs.Values)
            {
                uIBase.OnUpdate(deltaTime);
            }
        }

        public void ChangeView(Enum e)
        {
            m_stateID = UIUtils.GetStateID(e);

            foreach (UIBase uIBase in m_registeredUIs.Values)
            {
                uIBase.Active((uIBase.stateID & m_stateID) == m_stateID);
            }
        }

        public void RecycleView(Enum e)
        {
            m_stateID = UIUtils.GetStateID(e);

            foreach (UIBase uIBase in m_registeredUIs.Values)
            {
                uIBase.Destory();
            }
        }

        /// <summary>
        /// 控制介面是否可以點擊操作(全域)
        /// </summary>
        /// <param name="state"></param>
        public void SetGrahpicRaycaster(bool state)
        {
            m_graphicRaycaster.enabled = state;
        }
    }
}