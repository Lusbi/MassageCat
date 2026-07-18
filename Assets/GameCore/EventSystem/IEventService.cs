using System;

namespace GameCore.Event
{
    public interface IEventService
    {
        IDisposable AddListener<T>(Action<T> listener) where T : IEventBase;
        void RemoveListener<T>(Action<T> listener) where T : IEventBase;
        void Notify<T>(T eventData) where T : IEventBase;
    }
}
