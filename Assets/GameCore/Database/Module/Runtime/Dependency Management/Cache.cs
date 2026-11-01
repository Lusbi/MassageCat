#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Pool;

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
namespace GameCore.Database.Dependency
{
    /// <summary>
    /// Manages library cache (locates in: ../Library/JSLDatabase/Dependencies).
    /// </summary>
    static class Cache
    {
        [Serializable]
        private record Record(Type DataType, string DataKey, List<DependencyInfo> Dependencies);

        public static bool Exists => Directory.Exists(path);

        private const string FILENAME_FORMAT = "{0}_{1}.json";
        private static readonly string path;
        private static readonly Dictionary<int, Record> records = new();
        private static readonly HashSet<int> dirtyEntries = new();
        private static readonly Dictionary<int, DependencyInfo> objectHashes = new();
        private static bool isLoading = false;

        static Cache()
        {
            path = Path.Combine(Directory.GetCurrentDirectory(), "Library", "JSLDatabase", "Dependencies");
        }

        public static bool Add(Data data, DependencyInfo info)
        {
            Type dataType = data.GetType();
            int hash = Hash(dataType, data.key);
            if (!records.TryGetValue(hash, out var record))
                records.Add(hash, record = new Record(dataType, data.key, new List<DependencyInfo>()));

            if (record.Dependencies.Contains(info))
                return false;
            InsertDependency(record.Dependencies, info);
            objectHashes.TryAdd(info.ObjectHash, info);
            dirtyEntries.Add(hash);
            return true;
        }

        public static void Clear(Data data)
        {
            int hash = Hash(data.GetType(), data.key);
            if (!records.TryGetValue(hash, out var record))
                return;

            record.Dependencies.Clear();
            dirtyEntries.Add(hash);
        }

        public static void Dump()
        {
            if (dirtyEntries.Count == 0)
                return;

            Directory.CreateDirectory(path);

            string filename;
            string filePath;
            foreach (var entry in dirtyEntries)
            {
                if (!records.TryGetValue(entry, out var record)) continue;
                filename = GetFilename(record);
                filePath = Path.Combine(path, filename);
                if (record.Dependencies.Count > 0)
                {
                    string contents = JsonConvert.SerializeObject(record, Formatting.Indented);
                    File.WriteAllText(filePath, contents);
                }
                else
                {
                    File.Delete(filePath);
                }
            }
            dirtyEntries.Clear();
        }

        public static async Task DumpAsync()
        {
            if (dirtyEntries.Count == 0)
                return;

            Directory.CreateDirectory(path);

            using (ListPool<Task>.Get(out var tasks))
            {
                string filename;
                string filePath;
                foreach (var entry in dirtyEntries)
                {
                    if (!records.TryGetValue(entry, out var record)) continue;
                    filename = GetFilename(record);
                    filePath = Path.Combine(path, filename);
                    if (record.Dependencies.Count > 0)
                    {
                        string contents = JsonConvert.SerializeObject(record, Formatting.Indented);
                        tasks.Add(File.WriteAllTextAsync(filePath, contents));
                    }
                    else
                    {
                        File.Delete(filePath);
                    }
                }
                await Task.WhenAll(tasks);
            }
            dirtyEntries.Clear();
        }

        public static void Flush()
        {
            records.Clear();
            dirtyEntries.Clear();
            objectHashes.Clear();
        }

        public static void Retarget(Type dataType, string oldKey, string newKey)
        {
            if (dataType == null || string.IsNullOrWhiteSpace(oldKey)) return;
            int hash = Hash(dataType, oldKey);
            if (records.TryGetValue(hash, out var record))
            {
                int hash2 = Hash(dataType, newKey);
                if (records.TryGetValue(hash2, out var record2) && record2.Dependencies.Count > 0)
                {
                    Debug.LogError($"Failed to retarget key \"{oldKey}\" to an existing key \"{newKey}\" of {dataType.FullName}");
                    return;
                }

                records[hash2] = new Record(dataType, newKey, new List<DependencyInfo>(record.Dependencies));
                record.Dependencies.Clear();
                dirtyEntries.Add(hash);
                dirtyEntries.Add(hash2);
            }
            else
            {
                Debug.LogError($"Failed to retarget non-existent key \"{oldKey}\" of {dataType.FullName}");
            }
        }

        public static async Task<bool> TryGetDependenciesAsync(Data data, List<DependencyInfo> buffer, CancellationToken token)
        {
            if (buffer == null)
                return false;

            while (records.Count == 0 || isLoading)
            {
                await Task.Delay(10);
                token.ThrowIfCancellationRequested();
            }

            if (TryGetDependencies(data, out var dependencies))
            {
                buffer.AddRange(dependencies);
                return true;
            }

            string filename = GetFilename(data);
            string filePath = Path.Combine(path, filename);
            if (!File.Exists(filePath))
                return false;

            Record record = await LoadRecordFromFileAsync(filePath);
            if (record != null)
            {
                buffer.AddRange(record.Dependencies);
                return true;
            }
            return false;
        }

        public static bool TryGetDependencies(Data data, out IReadOnlyList<DependencyInfo> dependencies)
        {
            if (!data)
            {
                dependencies = null;
                return false;
            }

            return TryGetDependenciesPrivate(data.GetType(), data.key, out dependencies);
        }

        public static bool TryGetDependencies(Type dataType, string key, out IReadOnlyList<DependencyInfo> dependencies)
        {
            return TryGetDependenciesPrivate(dataType, key, out dependencies);
        }

        public static async Task<bool> LoadAsync(CancellationToken token)
        {
            if (isLoading)
            {
                while (isLoading)
                {
                    await Task.Delay(10);
                    token.ThrowIfCancellationRequested();
                }

                return true;
            }

            if (!Directory.Exists(path))
                return false;

            isLoading = true;
            Flush();
            using (ListPool<Task>.Get(out var tasks))
            {
                foreach (string filePath in Directory.EnumerateFiles(path, "*.json", SearchOption.TopDirectoryOnly))
                    tasks.Add(LoadRecordFromFileAsync(filePath));

                await Task.WhenAll(tasks);
            }

            isLoading = false;
            return true;
        }

        public static bool Remove(Data data, int infoObjectHash)
        {
            int hash = Hash(data.GetType(), data.key);
            if (records.TryGetValue(hash, out var record))
            {
                if (objectHashes.TryGetValue(infoObjectHash, out var info))
                {
                    if (record.Dependencies.Remove(info))
                    {
                        dirtyEntries.Add(hash);
                        return true;
                    }
                }
            }
            return false;
        }

        private static string GetFilename(Data data) => string.Format(FILENAME_FORMAT, data.GetType().Name, data.key);

        private static string GetFilename(Record record) => string.Format(FILENAME_FORMAT, record.DataType.Name, record.DataKey);

        private static int Hash(Type dataType, string key) => HashCode.Combine(dataType, key);

        private static int Hash(Record record) => HashCode.Combine(record.DataType, record.DataKey);

        private static void InsertDependency(List<DependencyInfo> dependencies, DependencyInfo dependencyInfo)
        {
            if (dependencies.Count == 0 || dependencies[^1].CompareTo(dependencyInfo) <= 0)
            {
                dependencies.Add(dependencyInfo);
                return;
            }

            if (dependencies[0].CompareTo(dependencyInfo) >= 0)
            {
                dependencies.Insert(0, dependencyInfo);
                return;
            }

            int index = dependencies.BinarySearch(dependencyInfo);
            if (index < 0)
                index = ~index;
            dependencies.Insert(index, dependencyInfo);
        }

        private static async Task<Record> LoadRecordFromFileAsync(string filePath)
        {
            string contents = await File.ReadAllTextAsync(filePath);
            var record = JsonConvert.DeserializeObject<Record>(contents);
            if (record != null)
            {
                int hash = Hash(record);
                if (records.TryAdd(hash, record))
                {
                    if (record.Dependencies != null)
                    {
                        bool changed = false;
                        DependencyInfo info;
                        for (int i = record.Dependencies.Count - 1; i > -1; --i)
                        {
                            info = record.Dependencies[i];
                            if (!info.ValidateAsset())
                            {
                                record.Dependencies.RemoveAt(i);
                                changed = true;
                                Debug.LogError($"Invalid dependency removed from {record}: {info}");
                            }
                            objectHashes.TryAdd(info.ObjectHash, info);
                        }
                        if (changed)
                        {
                            contents = JsonConvert.SerializeObject(record, Formatting.Indented);
                            await File.WriteAllTextAsync(filePath, contents);
                        }
                    }
                }
                else
                {
                    var relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), filePath);
                    Debug.LogError($"Duplicate record found at <a href=\"{relativePath}\">{relativePath}</a>\nExisted: " + records[hash]);
                }
            }
            else
            {
                Debug.LogError("Failed to deserialize record: " + filePath);
            }
            return record;
        }


        private static bool TryGetDependenciesPrivate(Type dataType, string key, out IReadOnlyList<DependencyInfo> dependencies)
        {
            if (dataType == null || string.IsNullOrWhiteSpace(key))
            {
                dependencies = null;
                return false;
            }

            int hash = Hash(dataType, key);
            if (records.TryGetValue(hash, out var record))
                dependencies = record.Dependencies.AsReadOnly();
            else
                dependencies = null;
            return dependencies != null;
        }
    }
}
#endif