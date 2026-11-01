using System.Collections.Generic;
using GameCore.Log;
using UnityEngine;

namespace GameCore.Database
{
    public class DatabaseRepository<T> where T : Data
    {
        private const string LOG_NO_KEY = "The key {0} does not exist in Database<{1}>, please check the key is correct or not.";
        private readonly Dictionary<string, T> m_repository;

        public Dictionary<string, T>.KeyCollection KeyCollection => m_repository.Keys;
        public Dictionary<string, T>.ValueCollection ValueCollection => m_repository.Values;

        public DatabaseRepository(IList<Repository<T>> repositories)
        {
            int count = 0;
            foreach (Repository<T> repository in repositories)
            {
                count += repository.GetBaseDataArray().Length;
            }

            m_repository = new Dictionary<string, T>(count);
            foreach (Repository<T> repository in repositories)
            {
                IReadOnlyList<T> list = repository.ReadOnlyList;
                foreach (T data in list)
                {
                    if (data == null)
                    {
                        continue;
                    }

                    if (!m_repository.TryAdd(data.key, data))
                    {
                        eLog.Error($"資料庫衝突：{this} Key：{data.key}");
                    }
                }
            }
        }
        public DatabaseRepository(IReadOnlyCollection<T> collection)
        {
            m_repository = new Dictionary<string, T>(collection.Count);
            foreach (T data in collection)
            {
                if (data == null) continue;
                if (!m_repository.TryAdd(data.key, data))
                    Debug.LogError( $"資料庫衝突：{this} Key：{data.key}");
            }
        }

        public bool Exists(string key) => m_repository.ContainsKey(key);

        public T GetByKey(string key)
        {
            if (!m_repository.TryGetValue(key, out T value))
                Debug.LogError(string.Format( LOG_NO_KEY, key, typeof(T).Name));

            return value;
        }

        public bool TryGet(string key, out T value) => m_repository.TryGetValue(key, out value);

#if UNITY_EDITOR
        /// <summary>
        /// 新增一筆資料(編輯器用)
        /// </summary>
        public void EditorAddData(string key, T data)
        {
            m_repository.Add(key, data);
        }
#endif
    }
}