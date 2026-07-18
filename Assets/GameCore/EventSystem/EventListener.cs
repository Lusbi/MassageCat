using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore.Event
{
    public class EventListener 
    {
        private bool m_active;
        public bool active
        {
            get { return m_active; }
            set
            {
                if (m_active == value)
                {
                    return;
                }

                m_active = value;
                OnActiveChanged();
            }
        }

        protected virtual void OnActiveChanged()
        {

        }

        private Dictionary<Type, IDisposable> m_eventListeners;

        public EventListener(bool active = true)
        {
            m_active = active;
            OnActiveChanged();

            m_eventListeners = new Dictionary<Type, IDisposable>();
        }

        private const string LOG_EVENT_EXIST = "Event type '{0}' is already exist";
        public void AddListener<T>(Action<T> listener, bool callWhenInactive = false) where T : IEventBase
        {
            Type t = typeof(T);
            if (m_eventListeners.ContainsKey(t))
            {
                Debug.LogErrorFormat(LOG_EVENT_EXIST, t.Name);
                return;
            }

            Action<T> action = (T eventData) =>
            {
                if (active || callWhenInactive)
                {
                    listener?.Invoke(eventData);
                }
            };

            m_eventListeners.Add(t, EventManager.instance.AddListener<T>(action));
        }

        public void RemoveListener<T>() where T : IEventBase
        {
            Type t = typeof(T);
            if (m_eventListeners.TryGetValue(t, out var listener))
            {
                listener.Dispose();
                m_eventListeners.Remove(t);
            }
        }

        #region IDestroyable
        public virtual void Destroy()
        {
            RemoveListener();
        }

        protected void RemoveListener()
        {
            foreach (var listener in m_eventListeners.Values)
            {
                listener.Dispose();
            }

            m_eventListeners.Clear();
        }
        #endregion
    }
}
