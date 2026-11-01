using System;
using GameCore;

namespace GameCore.Event
{
    public class EventManager : Singleton<EventManager>, IEventService
    {
        public IDisposable AddListener<T>(Action<T> listener) where T : IEventBase
        {
            return EventContainer<T>.AddListener(listener);
        }

        public void RemoveListener<T>(Action<T> listener) where T : IEventBase
        {
            EventContainer<T>.RemoveListener(listener);
        }

        public void Notify<T>(T eventBase) where T : IEventBase
        {
            EventContainer<T>.Notify(eventBase);

            // if (eventBase is IPoolableEventBase) 當eventBase是struct時會因為Boxing產生GC
            // 參考VitalRouter的作法(https://github.com/hadashiA/VitalRouter)避免此問題
            if (TypeAnalyzerCache<T>.IsPoolable)
            {
                // 回收IPoolableEventBase事件
                EventBasePool<T>.Instance.Return(eventBase);
            }
        }
    }
}
