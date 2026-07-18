using System;
using System.Collections.Generic;
using System.Linq;

namespace GameCore.Event
{
    public class EventManagerConsole
    {
        public static bool initialized;

        public enum SortType
        {
            Name,
            Count,
        }

        public enum OrderType
        {
            Ascending,
            Descending,
        }

        public class EventData
        {
            public Type type;
            public int notifyCount;
            public string typeName;
            public string notifyCountString;

            public EventData(Type type, int notifyCount)
            {
                this.type = type;
                this.notifyCount = notifyCount;
                typeName = this.type.Name;
                notifyCountString = this.notifyCount.ToString();
            }

            public void Notify()
            {
                notifyCount++;
                notifyCountString = notifyCount.ToString();
            }

            public void Clear()
            {
                notifyCount = 0;
                notifyCountString = notifyCount.ToString();
            }
        }

        public static List<EventData> eventDatas { get; private set; }
        public static SortType sorType
        {
            get { return m_sortType; }
            set
            {
                if(m_sortType != value)
                {
                    m_sortType = value;
                    Sort();
                }
            }
        }
        private static SortType m_sortType = SortType.Count;

        public static OrderType ordertype
        {
            get { return m_orderType; }
            set
            {
                if (m_orderType != value)
                {
                    m_orderType = value;
                    Sort();
                }
            }
        }
        private static OrderType m_orderType = OrderType.Descending;

        public static void Initialize()
        {
            if(initialized)
            {
                return;
            }

            initialized = true;

            eventDatas = new List<EventData>();
            var events = typeof(IEventBase).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(IEventBase)) && !t.IsAbstract && !t.IsGenericTypeDefinition);
            foreach (var e in events)
            {
                eventDatas.Add(new EventData(e, 0));
            }

            Sort();
        }

        public static void Destroy()
        {
            initialized = false;
            eventDatas = null;
        }

        public static void Notify<T>() where T : IEventBase
        {
            Type t = typeof(T);
            int index = -1;
            for(int i = 0; i < eventDatas.Count; i++)
            {
                if(eventDatas[i].type == t)
                {
                    index = i;
                    eventDatas[i].Notify();
                    break;
                }
            }

            if(m_sortType == SortType.Count)
            {
                switch (m_orderType)
                {
                    case OrderType.Ascending:
                        for(int i = index; i < eventDatas.Count - 1; i++)
                        {
                            if(eventDatas[i].notifyCount <= eventDatas[i + 1].notifyCount)
                            {
                                break;
                            }

                            EventData temp = eventDatas[i];
                            eventDatas[i] = eventDatas[i + 1];
                            eventDatas[i + 1] = temp;
                        }
                        break;
                    case OrderType.Descending:
                        for (int i = index; i > 0; i--)
                        {
                            if (eventDatas[i].notifyCount <= eventDatas[i - 1].notifyCount)
                            {
                                break;
                            }

                            EventData temp = eventDatas[i];
                            eventDatas[i] = eventDatas[i - 1];
                            eventDatas[i - 1] = temp;
                        }
                        break;
                }
            }
        }

        public static void Clear()
        {
            for (int i = 0; i < eventDatas.Count; i++)
            {
                eventDatas[i].Clear();
            }
        }

        private static void Sort()
        {
            switch (m_sortType)
            {
                case SortType.Name:
                    switch (m_orderType)
                    {
                        case OrderType.Ascending:
                            eventDatas.Sort(NameAscendingSort);
                            break;
                        case OrderType.Descending:
                            eventDatas.Sort(NameDescendingSort);
                            break;
                    }
                    break;
                case SortType.Count:
                    switch (m_orderType)
                    {
                        case OrderType.Ascending:
                            eventDatas.Sort(CountAscendingSort);
                            break;
                        case OrderType.Descending:
                            eventDatas.Sort(CountDescendingSort);
                            break;
                    }
                    break;
            }
        }

        private static int NameAscendingSort(EventData x, EventData y)
        {
            if (x.type == null && y.type == null)
            {
                return 0;
            }
            else if (x.type == null)
            {
                return -1;
            }
            else if (y.type == null)
            {
                return 1;
            }
            else
            {
                return x.type.Name.CompareTo(y.type.Name);
            }
        }

        private static int NameDescendingSort(EventData x, EventData y)
        {
            if (x.type == null && y.type == null)
            {
                return 0;
            }
            else if (x.type == null)
            {
                return 1;
            }
            else if (y.type == null)
            {
                return -1;
            }
            else
            {
                return y.type.Name.CompareTo(x.type.Name);
            }
        }

        private static int CountAscendingSort(EventData x, EventData y)
        {
            if (x.type == null && y.type == null)
            {
                return 0;
            }
            else if (x.type == null)
            {
                return -1;
            }
            else if (y.type == null)
            {
                return 1;
            }
            else
            {
                return x.notifyCount.CompareTo(y.notifyCount);
            }
        }

        private static int CountDescendingSort(EventData x, EventData y)
        {
            if (x.type == null && y.type == null)
            {
                return 0;
            }
            else if (x.type == null)
            {
                return 1;
            }
            else if (y.type == null)
            {
                return -1;
            }
            else
            {
                return y.notifyCount.CompareTo(x.notifyCount);
            }
        }
    }
}
