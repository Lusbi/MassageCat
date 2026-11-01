using System;
using System.Collections.Generic;

namespace GameCore.Event
{
    /// <summary>
    /// 可回收事件介面(事件為class時可實作此介面，重複使用)
    /// </summary>
    public interface IPoolableEventBase : IEventBase
    {
        void OnReturnToPool();
    }

    /// <summary>
    /// 事件池介面
    /// </summary>
    public interface IEventBasePool<T> where T : IEventBase
    {
        T Get();
        void Return(T eventBase);
    }

    /// <summary>
    /// 判斷與記錄某事件類型是否為IPoolableEventBase(避免Boxing用)
    /// </summary>
    static class TypeAnalyzerCache<T> where T : IEventBase
    {
        public static bool IsPoolable;

        static TypeAnalyzerCache()
        {
            IsPoolable = typeof(IPoolableEventBase).IsAssignableFrom(typeof(T));
        }
    }

    /// <summary>
    /// 事件池
    /// </summary>
    public static class EventBasePool<T> where T : IEventBase
    {
        public static readonly IEventBasePool<T> Instance = new QueueEventBasePool<T>();
    }

    /// <summary>
    /// 事件池(使用Queue實作)
    /// </summary>
    public class QueueEventBasePool<T> : IEventBasePool<T> where T : IEventBase
    {
        internal readonly Queue<T> m_queue = new();

        public T Get()
        {
            if (m_queue.TryDequeue(out var value))
            {
                return value;
            }

            return Activator.CreateInstance<T>();
        }

        public void Return(T eventBase)
        {
            if (eventBase is IPoolableEventBase poolableEventBase)
            {
                poolableEventBase.OnReturnToPool();
                m_queue.Enqueue(eventBase);
            }
        }
    }
}
