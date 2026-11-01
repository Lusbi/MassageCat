using System;
using System.Collections.Generic;
using GameCore.Log;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GameCore.Database
{
    public static class Database<T> where T : Data
    {
        private const string LOG_INITIALIZE_FIRST = "Please call Database<{0}>.Initialize first.";
        private const string ADDRESSABLE_FORMAT = "{0}Repository";

        private static bool m_initializing;
        private static bool m_initialized;
        private static Action m_onRepositoryLoaded;
        private static DatabaseRepository<T> m_databaseRepository;
        private static T[] m_datas;
        private static string[] m_keys;
        private static AsyncOperationHandle<IList<Repository<T>>> handle;

        public static void ReInitialized()
        {
            m_initialized = false;
            EditorInitialize();
        }

        public static bool IsInitialized()
        {
            return m_initialized;
        }

        public static void Initialize(Action onRepositoryLoaded = null)
        {
            if (m_initializing || m_initialized)
            {
                if (m_initialized)
                {
                    onRepositoryLoaded?.Invoke();
                }

                return;
            }

            m_initializing = true;
            m_onRepositoryLoaded = onRepositoryLoaded;

            string addressableName = string.Format(ADDRESSABLE_FORMAT, typeof(T).Name.Substring(0, typeof(T).Name.Length - 4));
            var handle = Addressables.LoadAssetsAsync<Repository<T>>(addressableName, null);
            handle.Completed += OnHandleCompleted;
        }
        private static void OnHandleCompleted(AsyncOperationHandle<IList<Repository<T>>> h)
        {
            if (h.Status == AsyncOperationStatus.Succeeded)
            {
                handle = h;
                OnRepositoryLoaded(handle.Result);
            }
            else
            {
                Addressables.Release(h);
            }
        }
        private static void OnRepositoryLoaded(IList<Repository<T>> repositories)
        {
            OnDataLoaded(repositories);
        }

        private static void OnDataLoaded(IList<Repository<T>> repositories)
        {
            m_initializing = false;
            m_initialized = true;

            if (repositories != null)
            {
                m_databaseRepository = new DatabaseRepository<T>(repositories);
            }

            m_onRepositoryLoaded?.Invoke();
            foreach (var kvp in m_asyncCallbacks)
            {
                kvp.Value?.Invoke(Load(kvp.Key));
            }
            m_asyncCallbacks.Clear();
        }

        private static void OnDataLoaded(T[] datas)
        {
            m_initializing = false;
            m_initialized = true;

            m_datas = datas;

            if (m_datas != null)
            {
                m_keys = new string[m_datas.Length];
                for (int i = 0; i < m_datas.Length; i++)
                {
                    if (m_datas[i] == null)
                        continue;
                    m_keys[i] = m_datas[i].key;
                }

                m_databaseRepository = new DatabaseRepository<T>(m_datas);
            }

            m_onRepositoryLoaded?.Invoke();
            foreach (var kvp in m_asyncCallbacks)
            {
                kvp.Value?.Invoke(Load(kvp.Key));
            }
            m_asyncCallbacks.Clear();

            m_loadAllAsyncCallbacks?.Invoke(m_datas);
            m_loadAllAsyncCallbacks = null;
        }

        public static bool Exists(string key)
        {
            EditorInitialize();

            if (!m_initialized)
            {
                //JSLDebug.LogErrorFormatTag(DatabaseTag.TAG_DATABASE, LOG_INITIALIZE_FIRST, typeof(T).Name);
                return false;
            }

            return m_databaseRepository.Exists(key);
        }

        public static string[] GetKeys()
        {
            EditorInitialize();

            if (!m_initialized)
            {
                //JSLDebug.LogErrorFormatTag(DatabaseTag.TAG_DATABASE, LOG_INITIALIZE_FIRST, typeof(T).Name);
                return null;
            }

            Dictionary<string, T>.KeyCollection keyCollection = m_databaseRepository.KeyCollection;
            string[] keys = new string[keyCollection.Count];
            keyCollection.CopyTo(keys, 0);
            return keys;
        }

        public static T Load(string key)
        {
            EditorInitialize();

            if (!m_initialized)
            {
                //JSLDebug.LogErrorFormatTag(DatabaseTag.TAG_DATABASE, LOG_INITIALIZE_FIRST, typeof(T).Name);
                return null;
            }

            return m_databaseRepository.GetByKey(key);
        }

        public static bool TryLoad(string key, out T value)
        {
            EditorInitialize();

            if (!m_initialized)
            {
                //JSLDebug.LogErrorFormatTag(DatabaseTag.TAG_DATABASE, LOG_INITIALIZE_FIRST, typeof(T).Name);
                value = null;
                return false;
            }

            return m_databaseRepository.TryGet(key, out value);
        }

        public static Dictionary<string, T>.ValueCollection GetAll()
        {
            EditorInitialize();

            if (!m_initialized)
            {
                eLog.Error($"{DatabaseTag.TAG_DATABASE} {typeof(T).Name} GetAll Faild");
                return null;
            }

            return m_databaseRepository.ValueCollection;
        }

        [Obsolete("改用 GetAll().Values" , false)]
        /// <summary>
        /// 改用 GetAll().Values
        /// </summary>
        /// <returns></returns>
        public static T[] LoadAll()
        {
            //EditorInitialize();

            //if (!m_initialized)
            //{
            //    //JSLDebug.LogErrorFormatTag(DatabaseTag.TAG_DATABASE, LOG_INITIALIZE_FIRST, typeof(T).Name);
            //    return null;
            //}

            //return m_datas;
            return null;
        }

        private static void EditorInitialize()
        {
#if UNITY_EDITOR
            if (!m_initialized)
            {
                if (!Application.isPlaying)
                {
                    OnDataLoaded(Editor.DatabaseEditorUtils.GetDatas<T>());
                }
            }
#endif
        }

        private static Dictionary<string, Action<T>> m_asyncCallbacks = new Dictionary<string, Action<T>>();
        public static void LoadAsync(string key, Action<T> onDataLoaded)
        {
            if (m_initialized)
            {
                onDataLoaded?.Invoke(Load(key));
                return;
            }

            if (m_asyncCallbacks.ContainsKey(key))
            {
                m_asyncCallbacks[key] += onDataLoaded;
            }
            else
            {
                m_asyncCallbacks.Add(key, onDataLoaded);
            }

            Initialize();
        }

        private static Action<T[]> m_loadAllAsyncCallbacks;
        public static void LoadAllAsync(Action<T[]> onDataLoaded)
        {
            if (m_initialized)
            {
                onDataLoaded?.Invoke(LoadAll());
                return;
            }

            m_loadAllAsyncCallbacks += onDataLoaded;
            Initialize();
        }

#if UNITY_EDITOR
        /// <summary>
        /// 新增一筆資料(編輯器用)
        /// </summary>
        public static void EditorAddData(string key, T data)
        {
            if (Exists(key) == false)
            {
                int dataLength = m_datas.Length;
                Array.Resize(ref m_datas, dataLength + 1);
                m_datas[dataLength] = data;

                m_databaseRepository.EditorAddData(key, data);
            }
        }
#endif
    }
}
