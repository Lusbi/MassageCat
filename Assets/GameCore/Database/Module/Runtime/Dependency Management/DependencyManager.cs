#if UNITY_EDITOR
using GameCore.SystemType.Extensions;
using GameCore.Database.Editor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameCore.Database.Dependency
{
    /// <summary>
    /// 管理本地Database資料欄位的依賴關係（關聯）。目前只追蹤used by，沒追蹤using。
    /// </summary>
    [InitializeOnLoad]
    public static class DependencyManager
    {
        public delegate void DependencyWriter(object obj, IReadOnlyList<FieldInfoPath> paths, DependencyInfo info, Action<Type> onFailed);

        public static bool IsResolveOnSaveOrImportEnabled { get; private set; } = true;

        private const int TIMEOUT_MILLISECS = 1000;
        //private const string SESSION_SCAN_KEY = "DependencyManager.ScanOnLaunch";
        private static readonly string[] scopedNamespacePrefixes = { "GameCore."};
        private static readonly FieldTracker componentTracker;
        private static readonly FieldTracker dataTracker;
        //private static readonly FieldTracker fsmStateActionTracker;
        private static readonly Type targetType;
        /// <summary>Indexes of Data collection used by an asset.</summary>
        private static readonly Dictionary<int, IList<Data>> indexes = new();
        private static readonly Dictionary<Type, MethodInfo> injectedResolvers;
        private static readonly DependencyWriter dependencyWriter = new(WriteDependencies);
        private static readonly string[] databaseFolders = { "Assets/AssetResources/Database" };
        private static Task loadingTask;

        static DependencyManager()
        {
            targetType = typeof(DataReference<>);

            componentTracker = new FieldTracker(targetType, typeof(MonoBehaviour), scopedNamespacePrefixes);
            dataTracker = new FieldTracker(targetType, typeof(Data), scopedNamespacePrefixes);
            //fsmStateActionTracker = new FieldTracker(typeof(PMDataReference<,>), typeof(FsmStateAction), scopedNamespacePrefixes);

            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
            EditorApplication.quitting += Cache.Dump;
            EditorSceneManager.sceneSaved += ResolveScene;

            injectedResolvers = TypeCache.GetTypesWithAttribute<DependencyResolverAttribute>().ToDictionary(x => x, x => x.GetMethod("Resolve", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));
        }

        public static void BeginReloadCache() => LoadCacheAsync();

        public static void ClearDependencies(Data data) => Cache.Clear(data);

        public static void ClearDependenciesFrom<T>(T data) where T : Data
        {
            T asset = EditorUtility.IsPersistent(data) ? data : DatabaseEditorUtils.LoadData<T>(data.key);
            if (!asset)
                return;
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long localId))
                ClearDependenciesFrom(guid, localId);
        }

        public static void ClearDependenciesFrom(string guid, long localId)
        {
            int hash = HashCode.Combine(guid, localId);
            if (indexes.TryGetValue(hash, out IList<Data> list))
            {
                foreach (Data data in list)
                    Cache.Remove(data, hash);
                list.Clear();
            }
        }

        public static void FlushCache() => Cache.Flush();

        public static async Task OnAssetPostprocessorDomainReloadAsync()
        {
            loadingTask ??= LoadAsync();
            await loadingTask;
        }

        public static void RemoveDependencyFrom(int infoObjectHash, string key) => RemoveDependencyFromIndexes(infoObjectHash, key);

        /// <summary>
        /// Resolves Data dependencies to given Data asset.
        /// </summary>
        /// <param name="data">The Data asset used by others.</param>
        public static void ResolveDependenciesTo<T>(T data) where T : Data
        {
            Type referenceBaseType = typeof(DataReference<>).MakeGenericType(typeof(T));
            foreach (var (dataType, paths) in dataTracker.GetTracked())
            {
                if (!paths.Any(x => IsTypeOrElement(x.FinalType, referenceBaseType)))
                    continue;

                foreach (Data asset in FindSubassets(dataType, databaseFolders))
                {
                    if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long localId))
                        continue;

                    DependencyInfo dependencyInfo = new(dataType, asset.key, guid, localId);
                    RemoveDependencyFrom(guid, localId, data.key);
                    foreach (var path in paths)
                    {
                        if (!IsTypeOrElement(path.FinalType, referenceBaseType))
                            continue;

                        path.Walk(asset, value =>
                        {
                            bool found = value != null && TryLoadData(value, out Data result) && result.key == data.key;
                            if (found)
                                WriteHandle(dependencyInfo, data);
                            return found;
                        });
                    }
                }
            }

            static bool IsTypeOrElement(Type type, Type targetType)
            {
                if (type.TryGetElementTypeOfArrayOrList(out Type elementType))
                    return targetType.IsAssignableFrom(elementType);
                return targetType.IsAssignableFrom(type);
            }
        }

        /// <summary>Resolves dependencies from data. Make sure the asset of given data exists on disk before calling.</summary>
        public static void ResolveGameCoreData(Data data)
        {
            Type dataType = data.GetType();
            if (!dataTracker.TryGetPaths(dataType, out var paths))
                return;

            Data asset = EditorUtility.IsPersistent(data) ? data : DatabaseEditorUtils.LoadData(dataType, data.key);
            if (!asset)
            {
                Debug.LogError("No asset found with key: " + data.key);
                return;
            }

            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long localId))
            {
                Debug.LogError($"Failed to get GUID and local file ID of {dataType.Name} {asset.key}");
                return;
            }

            ClearDependenciesFrom(guid, localId);
            DependencyInfo dependencyInfo = new(dataType, asset.key, guid, localId);

            WriteDependencies(data, paths, dependencyInfo, dataReferenceType =>
            {
                Debug.LogWarning($"Failed to load Data from {dataReferenceType}, Key: {asset.key}, Type: {dataType.Name}");
            });
        }

        //public static void ResolveFsmTemplate(FsmTemplate fsmTemplate)
        //{
        //    if (fsmTemplate == null)
        //        return;

        //    if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(fsmTemplate, out string guid, out long localId))
        //    {
        //        Debug.LogError("Failed to get GUID and local file ID of FsmTemplate " + fsmTemplate.name);
        //        return;
        //    }

        //    ClearDependenciesFrom(guid, localId);
        //    DependencyInfo dependencyInfo = new(typeof(FsmTemplate), fsmTemplate.name, guid, localId);
        //    ResolveFsm(fsmTemplate.fsm, dependencyInfo);
        //}

        public static void ResolvePrefab(GameObject prefab)
        {
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(prefab, out string guid, out long localId))
            {
                Debug.LogError("Failed to get GUID and local file ID of Prefab " + prefab.name);
                return;
            }

            ClearDependenciesFrom(guid, localId);
            // Invoke injected resolvers which take a gameObject as the first parameter
            foreach (var resolver in injectedResolvers.Where(x => x.Key.GetCustomAttribute<DependencyResolverAttribute>().ParameterType == typeof(GameObject)).Select(x => x.Value))
            {
                resolver?.Invoke(null, new object[] { prefab, guid, localId, dependencyWriter });
            }

            DependencyInfo dependencyInfo = new(typeof(GameObject), prefab.name, guid, localId);
            // PlayMakerFSM components
            // Known issues: state.Actions may not be loaded or the key of dataReference remains "Null" while trying to resolve embedded FSMs on a Prefab
            //var playMakerFSMs = prefab.GetComponentsInChildren<PlayMakerFSM>(true);
            //if (playMakerFSMs != null)
            //{
            //    foreach (var playMakerFSM in playMakerFSMs)
            //    {
            //        if (playMakerFSM.UsesTemplate)
            //            ResolveFsmTemplate(playMakerFSM.FsmTemplate);
            //        else
            //            ResolveFsm(playMakerFSM.Fsm, dependencyInfo);
            //    }
            //}

            ScanTrackedComponents(prefab, dependencyInfo);
        }

        public static void Retarget(Type dataType, string oldKey, string newKey)
        {
            if (!Cache.TryGetDependencies(dataType, oldKey, out var dependencies)) return;
            IsResolveOnSaveOrImportEnabled = false;
            UnityEngine.Object asset;
            Type assetType;
            foreach (var dependency in dependencies)
            {
                asset = AssetDatabase.LoadMainAssetAtPath(dependency.AssetPath);
                if (asset == null) continue;
                assetType = asset.GetType();
                if (assetType.IsSubclassOf(typeof(Repository)))
                {
                    if (dataTracker.TryGetPaths(dependency.GroupType, out var paths))
                    {
                        Data holder = DatabaseEditorUtils.LoadData(dependency.GroupType, dependency.Name);
                        SetKeysOnHolder(oldKey, newKey, dataType, paths, holder);
                    }
                    EditorUtility.SetDirty(asset);
                }
                else if (assetType == typeof(GameObject))
                {
                    foreach (var component in ((GameObject)asset).GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        if (!componentTracker.TryGetPaths(component.GetType(), out var paths)) continue;
                        SetKeysOnHolder(oldKey, newKey, dataType, paths, component);
                    }
                    EditorUtility.SetDirty(asset);
                }
                //else if (assetType == typeof(FsmTemplate))
                //{
                //    Fsm fsm = ((FsmTemplate)asset).fsm;
                //    if (!fsm.Initialized)
                //        fsm.Reinitialize();

                //    foreach (var state in fsm.States)
                //    {
                //        foreach (var action in state.Actions)
                //        {
                //            if (!fsmStateActionTracker.TryGetPaths(action.GetType(), out var paths)) continue;
                //            SetKeysOnAction(oldKey, newKey, dataType, paths, action);
                //        }
                //    }
                //    EditorUtility.SetDirty(asset);
                //}
                AssetDatabase.SaveAssetIfDirty(asset);
            }

            Cache.Retarget(dataType, oldKey, newKey);
            IsResolveOnSaveOrImportEnabled = true;
        }

        /// <summary>Performs project-wise scanning except scene assets.</summary>
        public static void Scan(IEnumerable<Data> assets = null)
        {
            foreach (var asset in FindAssets<GameObject>("t:prefab"))
                ResolvePrefab(asset);

            //foreach (var asset in FindAssets<FsmTemplate>())
            //    ResolveFsmTemplate(asset);

            assets ??= FindSubassets<Data>(databaseFolders);
            foreach (var asset in assets)
                ResolveGameCoreData(asset);
        }

        public static bool TryGetDependencies(Data data, out IReadOnlyList<DependencyInfo> dependencies) => Cache.TryGetDependencies(data, out dependencies);

        public static Task<bool> TryGetDependenciesAsync(Data data, List<DependencyInfo> buffer)
        {
            try
            {
                using CancellationTokenSource source = new(TIMEOUT_MILLISECS);
                return Cache.TryGetDependenciesAsync(data, buffer, source.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.LogError("Timeout while trying to get dependecies.");
                return Task.FromResult(false);
            }
        }

        private static void AddToIndexes(DependencyInfo dependencyInfo, Data data)
        {
            int hash = dependencyInfo.ObjectHash;
            if (!indexes.TryGetValue(hash, out IList<Data> list))
                indexes.Add(hash, list = new List<Data>());
            if (!list.Contains(data))
                list.Add(data);
        }

        private static void BuildIndexes(IEnumerable<Data> assets)
        {
            indexes.Clear();
            foreach (var asset in assets)
            {
                if (!Cache.TryGetDependencies(asset, out var dependencies))
                    continue;

                foreach (var dependency in dependencies)
                    AddToIndexes(dependency, asset);
            }
        }

        private static IEnumerable<T> FindAssets<T>(string[] searchInFolders = null) where T : UnityEngine.Object
        {
            return FindAssets<T>("t:" + typeof(T).FullName, searchInFolders);
        }

        private static IEnumerable<T> FindAssets<T>(string filter, string[] searchInFolders = null) where T : UnityEngine.Object
        {
            foreach (string guid in AssetDatabase.FindAssets(filter, searchInFolders))
                yield return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
        }

        private static IEnumerable<T> FindSubassets<T>(string[] searchInFolders = null) where T : UnityEngine.Object
        {
            return FindSubassets(typeof(T), searchInFolders).Cast<T>();
        }

        private static IEnumerable FindSubassets(Type type, string[] searchInFolders = null)
        {
            string assetPath;
            UnityEngine.Object[] subassets;
            foreach (var guid in AssetDatabase.FindAssets("t:" + type.FullName, searchInFolders))
            {
                assetPath = AssetDatabase.GUIDToAssetPath(guid);
                subassets = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath);
                foreach (var subasset in subassets)
                {
                    if (subasset)
                        yield return subasset;
                }
            }
        }

        private static void SetKeysOnHolder(string oldKey, string newKey, Type dataType, IEnumerable<FieldInfoPath> paths, object holder)
        {
            foreach (var path in paths)
            {
                path.Walk(holder, value =>
                {
                    Type baseType = value?.GetType().BaseType;
                    if (baseType == null || baseType.IsConstructedGenericType && baseType.GenericTypeArguments[0] != dataType)
                        return false;

                    var getKeyMI = baseType.GetMethod(nameof(DataReference<Data>.GetKey));
                    if ((string)getKeyMI.Invoke(value, null) == oldKey)
                    {
                        var setKeyMI = baseType.GetMethod(nameof(DataReference<Data>.SetKey));
                        setKeyMI.Invoke(value, new[] { newKey });
                    }
                    return false;
                });
            }
        }

        private static void SetKeysOnAction(string oldKey, string newKey, Type dataType, IEnumerable<FieldInfoPath> paths, object action)
        {
            foreach (var path in paths)
            {
                path.Walk(action, value =>
                {
                    Type baseType = value?.GetType().BaseType;
                    if (baseType == null || baseType.IsConstructedGenericType && baseType.GenericTypeArguments[1] != dataType)
                        return false;

                    //var backingKeyFI = baseType.GetField(nameof(PMDataReference<DataReference<Data>, Data>.backingKey));
                    //var backingKey = (string)backingKeyFI.GetValue(value);
                    //if (backingKey == oldKey)
                    //    backingKeyFI.SetValue(value, newKey);

                    return false;
                });
            }
        }

        private static bool TryLoadData(object dataReference, out Data data)
        {
            var type = dataReference.GetType();
            var dataType = type.BaseType.GenericTypeArguments[0];
            var getKeyMI = type.BaseType.GetMethod(nameof(DataReference<Data>.GetKey));
            var key = (string)getKeyMI?.Invoke(dataReference, null);
            data = DatabaseEditorUtils.LoadData(dataType, key);
            return data != null;
        }

        private static async Task LoadAsync()
        {
            var assets = FindSubassets<Data>(databaseFolders);
            if (await LoadCacheAsync())
            {
                BuildIndexes(assets);
            }
            else
            {
                Scan(assets);
                await Cache.DumpAsync();
            }
            loadingTask = null;
        }

        private static Task<bool> LoadCacheAsync()
        {
            try
            {
                using CancellationTokenSource source = new(TIMEOUT_MILLISECS);
                return Cache.LoadAsync(source.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.LogError("Timeout while loading the cache.");
                return Task.FromResult(false);
            }
        }

        private static void OnAfterAssemblyReload()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            loadingTask ??= LoadAsync();
        }

        private static void OnBeforeAssemblyReload()
        {
            Cache.Dump();
        }

        private static void RemoveDependencyFrom(string guid, long localId, string key)
        {
            int hash = HashCode.Combine(guid, localId);
            RemoveDependencyFromIndexes(hash, key);
        }

        private static void RemoveDependencyFromIndexes(int hash, string key)
        {
            if (indexes.TryGetValue(hash, out IList<Data> list))
            {
                foreach (Data data in list)
                {
                    if (data.key == key)
                    {
                        Cache.Remove(data, hash);
                        list.Remove(data);
                        break;
                    }
                }
            }
        }


        private static void ResolveScene(Scene scene)
        {
            var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long localId))
            {
                Debug.LogError("Failed to get GUID and local file ID of scene " + scene.name);
                return;
            }

            ClearDependenciesFrom(guid, localId);            
        }

        private static void ScanTrackedComponents(GameObject go, DependencyInfo dependencyInfo)
        {
            Component[] components;
            foreach (var (componentType, paths) in componentTracker.GetTracked())
            {
                components = go.GetComponentsInChildren(componentType, true);
                if (components == null || components.Length == 0)
                    continue;

                foreach (var component in components)
                    WriteDependencies(component, paths, dependencyInfo, dataReferenceType =>
                    {
                        Debug.LogWarning($"Failed to load Data from {dataReferenceType}, Component: {component.GetType().Name}, GameObject: {component.name}");
                    });
            }
        }

        private static bool TryGetPrefabGuidFromObject(GameObject gameObject, out string guid)
        {
            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null && prefabStage.IsPartOfPrefabContents(gameObject))
            {
                guid = AssetDatabase.AssetPathToGUID(prefabStage.assetPath);
            }
            else if (PrefabUtility.IsPartOfPrefabInstance(gameObject))
            {
                string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);
                guid = AssetDatabase.AssetPathToGUID(assetPath);
            }
            else
            {
                guid = null;
            }

            return !string.IsNullOrEmpty(guid);
        }

        private static void WriteDependencies(object obj, IReadOnlyList<FieldInfoPath> paths, DependencyInfo dependencyInfo, Action<Type> onLoadFailed = null)
        {
            foreach (var path in paths)
            {
                path.Walk(obj, value =>
                {
                    onLoadFailed?.Invoke(path.FinalType);
                    return false;
                });
            }
        }

        private static void WriteHandle(DependencyInfo dependencyInfo, Data data)
        {
            if (Cache.Add(data, dependencyInfo))
                AddToIndexes(dependencyInfo, data);
        }
    }
}
#endif
