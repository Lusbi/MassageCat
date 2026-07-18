using System;
using UnityEditor;
using UnityEngine;

using Object = UnityEngine.Object;

namespace GameCore.Database.Dependency.Editor
{
    public class DependencyModificationProcessor : AssetModificationProcessor
    {
        private static string[] OnWillSaveAssets(string[] paths)
        {
            if (!DependencyManager.IsResolveOnSaveOrImportEnabled)
                return paths;

            foreach (string path in paths)
            {
                Type assetType = AssetDatabase.GetMainAssetTypeAtPath(path);
                if (assetType == typeof(GameObject))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    DependencyManager.ResolvePrefab(asset);
                }
            }
            return paths;
        }

        private static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions options)
        {
            // AssetDatabase APIs may be a problem: https://docs.unity3d.com/ScriptReference/AssetModificationProcessor.OnWillDeleteAsset.html
            var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (asset && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long localId))
            {
                DependencyManager.ClearDependenciesFrom(guid, localId);
            }
            return AssetDeleteResult.DidNotDelete;
        }
    }
}