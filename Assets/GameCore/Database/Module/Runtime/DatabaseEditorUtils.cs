#if UNITY_EDITOR
using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using MemberTypes = CsvHelper.Configuration.MemberTypes;

namespace GameCore.Database.Editor
{
    public static class DatabaseEditorUtils
    {
        public const string REPOSITORY_PATH = "Assets/AssetResources/Database";
        private const string IMPORT_PROGRESS_BAR_TITLE = "Importing {0}...{1}/{2}";
        private const string IMPORT_PROGRESS_BAR_INFO = "Importing {0} with key {1}...";
        private const string IMPORT_FINISH = "{0} imported to user repo {1}, cost {2} seconds.";
        const string IMPORT_TITLE = "Importing from CSV";
        const string IMPORT_INFO_FORMAT = "Processing...{0}";

        private const string DATA_NAME_FORMAT = "{0}_{1}"; //DataName, Key
        private const string DATA_KEY_FORMAT = "{0}.{1}.{2}"; //DataName, UserName, Index

        private const string REPO_ASSET_NAME_FORMAT = "{0}_{1}.asset"; //RepositoryName, UserName

        private static readonly Dictionary<int, FieldInfo> fieldInfoCache = new();
        private static int serialNumber;
        private static string newKey;

        public static void Import<T>(string text) where T : Data
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                T[] importDatas = DatabaseCSV.FromCSV<T>(text);
                for (int i = 0; i < importDatas.Length; i++)
                {
                    if (EditorUtility.DisplayCancelableProgressBar(string.Format(IMPORT_PROGRESS_BAR_TITLE, typeof(T).Name, i + 1, importDatas.Length),
                        string.Format(IMPORT_PROGRESS_BAR_INFO, typeof(T).Name, importDatas[i].key),
                        (float)(i + 1) / importDatas.Length))
                    {
                        break;
                    }

                    SaveData(importDatas[i]);
                }
            }
            catch (System.Exception exception)
            {
                UnityEngine.Debug.LogError($"{exception} \n {text}");
            }

            EditorUtility.ClearProgressBar();

            stopwatch.Stop();
            UnityEngine.Debug.Log(string.Format( IMPORT_FINISH, typeof(T).Name, DatabaseUser.GetUserName(), stopwatch.ElapsedMilliseconds * 0.001f));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static async Task ImportCsvAsync<T>(string filePath) where T : Data
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            const string FIELD_KEY = nameof(Data.key);
            const BindingFlags BINDING_FLAGS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            Type dataType = typeof(T);
            var stopwatch = Stopwatch.StartNew();
            CsvConfiguration config = new(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                IncludePrivateMembers = true,
                MemberTypes = MemberTypes.Fields,
                MissingFieldFound = null
            };
            using (StreamReader reader = new(filePath))
            using (CsvReader csv = new(reader, config))
            {
                csv.Context.RegisterClassMap(new DataMap<T>(csv.Context));
                if (!await csv.ReadAsync() || !csv.ReadHeader())
                {
                    UnityEngine.Debug.LogError($"No header found in <a href=\"{filePath}\">CSV file<a/>.");
                    return;
                }

                string key;
                T data;
                while (await csv.ReadAsync())
                {
                    key = csv.GetField(FIELD_KEY);
                    if (EditorUtility.DisplayCancelableProgressBar(IMPORT_TITLE, string.Format(IMPORT_INFO_FORMAT, key), 1f))
                        break;

                    data = LoadData<T>(key);
                    if (!data)
                        data = ScriptableObject.CreateInstance<T>();

                    foreach (var fieldName in csv.HeaderRecord)
                    {
                        if (fieldName == FIELD_KEY)
                            continue;
                        int hash = HashCode.Combine(dataType, fieldName);
                        if (!fieldInfoCache.TryGetValue(hash, out var fieldInfo))
                            fieldInfoCache.Add(hash, fieldInfo = dataType.GetField(fieldName, BINDING_FLAGS));
                        string rawValue = csv.GetField(fieldName);
                        DatabaseCSV.ProcessField(data, fieldInfo, rawValue);
                    }
                    SaveData(data);
                }
            }
            stopwatch.Stop();
            UnityEngine.Debug.Log($"Importing elapsed time: {stopwatch.ElapsedMilliseconds} millisecs.");
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 匯入資料至指定使用者
        /// </summary>
        /// <typeparam name="T">資料類別</typeparam>
        /// <param name="text">資料</param>
        /// <param name="userName">使用者名稱</param>
        public static void ImportToUser<T>(string text, string userName) where T : Data
        {
            using (new DatabaseUserScope(userName))
            {
                Import<T>(text);
            }
        }

        public static string Export<T>() where T : Data
        {
            return Export<T>(string.Empty);
        }

        public static string Export<T>(string filter) where T : Data
        {
            return DatabaseCSV.ToCSV(GetDatas<T>(filter));
        }

        public static T[] GetDatas<T>() where T : Data
        {
            return GetDatas<T>(string.Empty);
        }

        public static T[] GetDatas<T>(string filter) where T : Data
        {
            using var _ = ListPool<T>.Get(out var list);

            if (TryGetRepositories(out Repository<T>[] repositories))
            {
                for (int i = 0; i < repositories.Length; i++)
                {
                    if (repositories[i] == null)
                        continue;
                    var temp = repositories[i].FilterDataKey(filter);
                    if (temp != null)
                    {
                        list.AddRange(temp);
                    }
                }
            }

            return list.ToArray();
        }

        public static IEnumerable<T> EnumerateData<T>() where T : Data
        {
            if (TryGetRepositories(out Repository<T>[] repositories))
            {
                foreach (var repository in repositories)
                {
                    foreach (var data in repository.ReadOnlyList)
                        yield return data;
                }
            }
        }

        public static bool HasData<T>(T data) where T : Data
        {
            if (data == null)
                return false;

            return LoadData<T>(data.key) != null;
        }

        public static bool HasKey<T>(string key) where T : Data
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            return LoadData<T>(key) != null;
        }

        /// <summary>
        /// 儲存資料
        /// </summary>
        /// <typeparam name="T">資料類別</typeparam>
        /// <param name="data">將被儲存的資料內容</param>
        /// <param name="oldKey">重新命名Key時須提供原本的Key</param>
        /// <param name="savesAsCopy">當沒有既有的資料時，是否以副本的方式儲存新資料?</param>
        /// <returns>被儲存的Asset</returns>
        public static T SaveData<T>(T data, string oldKey = null, bool savesAsCopy = true) where T : Data
        {
            if (!data)
                return null;

            string targetKey = !string.IsNullOrEmpty(oldKey) ? oldKey : data.key;
            if (!TryGetRepository(targetKey, out Repository<T> repository))
            {
                repository = GetUserRepository<T>();
            }

            if (!string.IsNullOrEmpty(newKey) && data.key == newKey)
            {
                string repoName = ConvertDataName<T>(nameof(Repository));
                RepositorySettingsHelper.SetSerialNumber(repoName, serialNumber);
                newKey = null;
            }

            data.name = string.Format(DATA_NAME_FORMAT, typeof(T).Name, data.key);
            return repository.Save(data, oldKey, savesAsCopy);
        }

        public static void DeleteData<T>(T data) where T : Data
        {
            if (data == null)
            {
                return;
            }

            if (!TryGetRepository(data.key, out Repository<T> repository))
            {
                return;
            }

            repository.Delete(data);
        }

        public static T LoadData<T>(string key) where T : Data
        {
            TryGetRepository(key, out _, out T data);
            return data;
        }

        public static Data LoadData(Type dataType, string key)
        {
            if (TryGetRepositories(dataType, out Repository[] repos))
            {
                foreach (var repo in repos)
                {
                    var data = repo.GetBaseData(key);
                    if (data)
                        return data;
                }
            }
            return null;
        }

        private static bool TryGetRepository<T>(string key, out Repository<T> result) where T : Data
        {
            if (TryGetRepositories(out Repository<T>[] repositories))
                result = Array.Find(repositories, x => x.GetData(key));
            else
                result = null;
            return result != null;
        }

        private static bool TryGetRepository<T>(string key, out Repository<T> result, out T data) where T : Data
        {
            if (TryGetRepositories(out Repository<T>[] repositories))
            {
                foreach (var repo in repositories)
                {
                    data = repo.GetData(key);
                    if (data)
                    {
                        result = repo;
                        return true;
                    }
                }
            }
            result = null;
            data = null;
            return false;
        }

        private static bool TryGetRepositories<T>(out Repository<T>[] result) where T : Data
        {
            UpdateRepositoryCaches<T>(false);
            result = m_repositoryCaches.TryGetValue(typeof(T), out var result2) ? (Repository<T>[])result2 : null;
            return result != null;
        }

        private static bool TryGetRepositories(Type dataType, out Repository[] result)
        {
            UpdateRepositoryCaches(dataType);
            return m_repositoryCaches.TryGetValue(dataType, out result) && result != null;
        }

        private static readonly Dictionary<Type, Repository[]> m_repositoryCaches = new();
        public static void UpdateRepositoryCaches<T>(bool forceUpdate) where T : Data
        {
            if (!forceUpdate && m_repositoryCaches.ContainsKey(typeof(T)))
                return;

            Type type = typeof(T);
            string[] guids = AssetDatabase.FindAssets(string.Format("t:JSLCore.Database.{0}", ConvertDataName<T>(nameof(Repository))));
            if (guids == null || guids.Length == 0)
            {
                m_repositoryCaches[type] = null;
                return;
            }

            var repositories = new Repository<T>[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                repositories[i] = AssetDatabase.LoadAssetAtPath<Repository<T>>(assetPath);
            }

            m_repositoryCaches[type] = repositories;
        }

        private static void UpdateRepositoryCaches(Type dataType, bool force = false)
        {
            if (!force && m_repositoryCaches.ContainsKey(dataType))
                return;

            string[] guids = AssetDatabase.FindAssets(string.Format("t:JSLCore.Database.{0}", ConvertDataName(dataType, nameof(Repository))));
            if (guids == null || guids.Length == 0)
            {
                m_repositoryCaches[dataType] = null;
                return;
            }

            Type repoType = typeof(Repository<>).MakeGenericType(dataType);
            var repositories = (Repository[])Array.CreateInstance(repoType, guids.Length);
            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                repositories[i] = (Repository)AssetDatabase.LoadAssetAtPath(assetPath, repoType);
            }

            m_repositoryCaches[dataType] = repositories;
        }

        public static string GetNewKey<T>() where T : Data
        {
            string repoName = ConvertDataName<T>(nameof(Repository));
            serialNumber = RepositorySettingsHelper.GetSerialNumber(repoName);
            int interval = RepositorySettingsHelper.GetSerialNumberInterval(repoName);

            string codeName = ConvertDataName<T>(string.Empty);
            string userName = DatabaseUser.GetUserName();
            string key = string.Format(DATA_KEY_FORMAT, codeName, userName, serialNumber);
            int origin = serialNumber;
            while (LoadData<T>(key))
            {
                serialNumber += interval;
                key = string.Format(DATA_KEY_FORMAT, codeName, userName, serialNumber);
            }

            if (serialNumber == origin)
                serialNumber += interval;

            return newKey = key;
        }

        private static Repository<T> GetUserRepository<T>() where T : Data
        {
            string assetPath = GetUserRepositoryPath<T>();
            Repository<T> repository = AssetDatabase.LoadAssetAtPath<Repository<T>>(assetPath);
            if (repository == null)
            {
                repository = CreateRepository<T>();
            }

            return repository;
        }

        private static Repository<T> CreateRepository<T>() where T : Data
        {
            string userName = DatabaseUser.GetUserName();
            Directory.CreateDirectory(Path.Combine(REPOSITORY_PATH, userName));

            string repoClassName = string.Format("GameCore.Database.{0}", ConvertDataName<T>(nameof(Repository)));
            var repository = (Repository<T>)ScriptableObject.CreateInstance(repoClassName);
            AssetDatabase.CreateAsset(repository, GetUserRepositoryPath<T>(userName));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            UpdateRepositoryCaches<T>(true);

            return repository;
        }

        private static string GetUserRepositoryPath<T>(string userName = null) where T : Data
        {
            if (string.IsNullOrEmpty(userName))
                userName = DatabaseUser.GetUserName();
            return Path.Combine(REPOSITORY_PATH, userName, GetUserRepositoryName<T>());
        }

        public static string GetUserRepositoryName<T>() where T : Data
        {
            return string.Format(REPO_ASSET_NAME_FORMAT, ConvertDataName<T>(nameof(Repository)), DatabaseUser.GetUserName());
        }

        public static string GetRepoAssetName<T>(T data) where T : Data
        {
            return TryGetRepository(data.key, out Repository<T> repository) ? repository.name + ".asset" : string.Empty;
        }

        public static string GetRepositoryUserName<T>(T data) where T : Data
        {
            if (TryGetRepository(data.key, out Repository<T> repository))
                return repository.GetUserName();
            return string.Empty;
        }

        public static string ConvertDataName<T>(string suffix) where T : Data => typeof(T).Name.Replace(nameof(Data), suffix);

        public static string ConvertDataName(Type dataType, string suffix) => dataType.Name.Replace(nameof(Data), suffix);
    }
}
#endif