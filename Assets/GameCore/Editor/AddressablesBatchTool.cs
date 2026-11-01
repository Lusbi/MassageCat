using GameCore.Resources.Utils;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

using ModificationEvent = UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.ModificationEvent;

[InitializeOnLoad]
public class AddressablesBatchTool
{
    private const string DATABASE_BUNDLE_PREFIX = "database";

    static AddressablesBatchTool()
    {
        EditorApplication.update += Update;
    }

    static void Update()
    {
        if (EditorApplication.isUpdating || EditorApplication.isCompiling) return;

        AddressableAssetSettingsDefaultObject.Settings.OnModification += OnSettingsModified;
        EditorApplication.update -= Update;
    }

    private static void OnSettingsModified(AddressableAssetSettings settings, ModificationEvent e, object obj)
    {
        if (e == ModificationEvent.EntryAdded)
        {
            if (obj is List<AddressableAssetEntry> list)
            {
                foreach (var entry in list)
                {
                    string filename = Path.GetFileNameWithoutExtension(entry.AssetPath);
                    string groupName = AddressableUtils.GetGroupNameFromPath(entry.AssetPath);
                    AddressableAssetGroup group = GetOrCreateGroup(settings, groupName);
                    entry.SetAddress(filename, false);
                    settings.CreateOrMoveEntry(entry.guid, group, false, false);
                }
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static AddressableAssetGroup GetOrCreateGroup(AddressableAssetSettings settings, string groupName)
    {
        AddressableAssetGroup group = settings.FindGroup(groupName);
        if (group == null)
        {
            group = settings.CreateGroup(groupName, false, false, false, null);
            var schema = group.AddSchema<BundledAssetGroupSchema>();
            schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
            group.AddSchema<ContentUpdateGroupSchema>().StaticContent = true;
        }

        return group;
    }

    #region MenuItems
    [MenuItem("EditorTool/Addressables/Remove Empty Groups")]
    private static void RemoveEmptyGroups()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        for (int i = settings.groups.Count - 1; i >= 0; i--)
        {
            AddressableAssetGroup group = settings.groups[i];
            if (group.ReadOnly || group.Default) continue;

            if (group == null || group.entries.Count == 0)
                settings.RemoveGroup(group);
        }
    }
    #endregion
}
