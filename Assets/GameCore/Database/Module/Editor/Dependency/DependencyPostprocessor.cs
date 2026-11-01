using System;
using UnityEditor;
using UnityEngine;

namespace GameCore.Database.Dependency.Editor
{
    public class DependencyPostprocessor : AssetPostprocessor
    {
        private static async void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            if (didDomainReload)
            {
                await DependencyManager.OnAssetPostprocessorDomainReloadAsync();
            }

            if (!DependencyManager.IsResolveOnSaveOrImportEnabled) return;

            Type assetType;
            foreach (string assetPath in importedAssets)
            {
                assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
                if (assetType == null)
                    continue;

                if (assetType.IsSubclassOf(typeof(Repository)))
                {
                    foreach (var subasset in AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath))
                    {
                        if (subasset is Data data)
                            DependencyManager.ResolveGameCoreData(data);
                    }
                }
                else if (assetType == typeof(GameObject))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    if (asset != null)
                        DependencyManager.ResolvePrefab(asset);
                }
            }
        }
    }
}
