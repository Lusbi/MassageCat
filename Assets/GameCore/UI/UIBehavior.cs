using GameCore.Resources;
using UnityEngine;

namespace GameCore.UI
{
    public class UIBehavior<T> : UIBase where T : UIViewBase
    {
        protected virtual int sortOrderID { get; }

        protected virtual string addressableKey => throw new System.NotImplementedException();

        protected GameObject m_uiTemplate;
        protected T m_view;
        protected System.Action m_callBack;

        public UIBehavior(System.Action callBack = null, params System.Enum[] enums)
        {
            identifier = GetHashCode();
            stateID = UIUtils.GetStateID(enums);
            m_callBack = callBack;

            CoreResoucesService.LoadAssetAsync<GameObject>(addressableKey, ViewLoaded);
        }

        public virtual void ViewLoaded(GameObject uIViewBase)
        {
            if (uIViewBase == null)
            {
                Debug.LogError($"Load UI Faild..{GetType().ToString()}");
                return;
            }

            m_uiTemplate = GameObject.Instantiate(uIViewBase);
            

            ViewLoadedFinished();
        }

        public virtual void ViewLoadedFinished()
        {
            UIManager.instance.Registered(this);

            m_view = m_uiTemplate.GetComponent<T>();
            m_view.Initlization();
            m_view.transform.SetParent(UIManager.instance.transform);
            m_view.transform.localPosition = Vector3.zero;
            m_view.transform.localScale = Vector3.one;

            m_view.canvasGroup.alpha = 0;
            m_view.canvasGroup.blocksRaycasts = false;

            m_view.SetSortOrder(sortOrderID);

            if (m_view.currentStatus != UIStatus.Enable)
            {
                m_view.Active(false);
            }

            m_callBack?.Invoke();
        }

        public override void Destory()
        {
            base.Destory();
            CoreResoucesService.Release(m_uiTemplate);
            m_view = null;
        }

        public override void Active(bool isActive)
        {
            m_view.Active(isActive);
        }

        public virtual void ViewEnable()
        {
            m_view.ViewEnable();
        }

        public virtual void ViewDisable()
        {
            m_view.ViewDisable();
        }

    }
}