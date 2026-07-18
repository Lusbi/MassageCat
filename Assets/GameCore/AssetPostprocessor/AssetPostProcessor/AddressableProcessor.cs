#if UNITY_EDITOR
using GameCore.Resources.Utils;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace GameCore.AssetPostProcessor
{
    public class AddressableProcessor : GameCoreAssetPostprocessor
    {
        public override AssetPostProcessorType AssetPostProcessorType => AssetPostProcessorType.Addressable;

        // 在資源導入後進行處理
        void OnPreprocessAsset()
        {
            if (Valid(assetImporter) == false)
            {
                return;
            }

            EditorApplication.delayCall += Execute;
        }

        /// <summary>
        /// 預設為 AssetResources
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="groupName"></param>
        /// <returns></returns>
        private AddressableAssetGroup GetOrCreateGroup(AddressableAssetSettings settings, string groupName)
        {
            AddressableAssetGroup group = settings.FindGroup(groupName);
            if (group == null)
            {
                group = settings.CreateGroup(groupName, false, false, false, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            }
            return group;
        }

        public override void Execute()
        {
            string assetPath = assetImporter.assetPath;
            if (AssetDatabase.IsValidFolder(assetPath))
                return;

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogWarning("AddressableAssetSettings not found.");
                return;
            }

            string groupName = AddressableUtils.GetGroupNameFromPath(assetPath);
            AddressableAssetGroup group = GetOrCreateGroup(settings, groupName);

            string address = AddressableUtils.GetAddressFromPath(assetPath);
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(assetPath), group);
            entry.address = address;

            EditorApplication.delayCall -= Execute;
        }
    }
}
#endif