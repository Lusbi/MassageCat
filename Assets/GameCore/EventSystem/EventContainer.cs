using System;

namespace GameCore.Event
{
    public class EventContainer<T> where T : IEventBase
    {
        protected static Action<T> m_listeners;

        public static IDisposable AddListener(Action<T> listener)
        {
            m_listeners += listener;
            return new AddListenerDispose(listener);
        }

        public static void RemoveListener(Action<T> listener)
        {
            m_listeners -= listener;
        }

        public static void Notify(T eventData)
        {
#if UNITY_EDITOR
            if (EventManagerConsole.initialized)
            {
                EventManagerConsole.Notify<T>();
            }
#endif

            m_listeners?.Invoke(eventData);
        }

        private class AddListenerDispose : IDisposable
        {
            private Action<T> m_listener;

            public AddListenerDispose(Action<T> listener)
            {
                m_listener = listener;
            }

            public void Dispose()
            {
                RemoveListener(m_listener);
            }
        }
    }
}
