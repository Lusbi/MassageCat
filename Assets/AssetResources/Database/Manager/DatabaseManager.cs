// WARNING: Generated file. Do not modify!
using System;
using System.Linq;

namespace GameCore.Database
{
    public class DatabaseManager : Singleton<DatabaseManager>
    {
        private bool m_initializing;
        private bool m_initialized;
        private int m_initializingDatabaseCount;
        private Action m_onInitializeFinish;

        public void Initialize(Action onInitializeFinish = null)
        {
            m_onInitializeFinish = onInitializeFinish;

            if (m_initialized)
            {
                m_onInitializeFinish?.Invoke();
                return;
            }

            if (m_initializing)
            {
                return;
            }

            m_initializing = true;
            m_initializingDatabaseCount = 3;

            Database<FlagData>.Initialize(OnDatabaseInitialized);
            Database<ScenemapData>.Initialize(OnDatabaseInitialized);
            Database<RoleData>.Initialize(OnDatabaseInitialized);

            //// Use reflection to find all classes inheriting from GameCore.Data
            //var dataTypes = AppDomain.CurrentDomain.GetAssemblies()
            //    .SelectMany(assembly => assembly.GetTypes())
            //    .Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(Data)))
            //    .ToList();

            //m_initializingDatabaseCount = dataTypes.Count;

            //// Initialize each database using reflection
            //foreach (var dataType in dataTypes)
            //{
            //    // Get the generic Database<T> type
            //    var databaseType = typeof(Database<>).MakeGenericType(dataType);

            //    // Get the Initialize method
            //    var initializeMethod = databaseType.GetMethod("Initialize");

            //    // Call Initialize method with OnDatabaseInitialized as callback
            //    initializeMethod.Invoke(this, new object[] { (Action)OnDatabaseInitialized });
            //}
        }

        private void OnDatabaseInitialized()
        {
            m_initializingDatabaseCount--;
            if (m_initializingDatabaseCount == 0)
            {
                m_initializing = false;
                m_initialized = true;
                m_onInitializeFinish?.Invoke();
            }
        }

        public T Load<T>(string key) where T : Data
        {
            return Database<T>.Load(key);
        }

        public T[] LoadAll<T>() where T : Data
        {
            return Database<T>.LoadAll();
        }

        public void LoadAsync<T>(string key, Action<T> onDataLoaded) where T : Data
        {
            Database<T>.LoadAsync(key, onDataLoaded);
        }

        public void LoadAllAsync<T>(Action<T[]> onDataLoaded) where T : Data
        {
            Database<T>.LoadAllAsync(onDataLoaded);
        }
    }
}