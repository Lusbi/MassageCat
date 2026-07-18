#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using GameCore.Database;
using GameCore.Database.Editor;
using UnityEditor;
using UnityEngine;

internal static class TalkScriptableObjectEditorUtility
{
    internal const string FolderPath = "Assets/Modules/TalkScriptableObject";

    internal static string EnsureFolder()
    {
        if (AssetDatabase.IsValidFolder("Assets/Modules") == false)
        {
            if (AssetDatabase.IsValidFolder("Assets") == false)
            {
                throw new DirectoryNotFoundException("Assets folder not found.");
            }

            AssetDatabase.CreateFolder("Assets", "Modules");
        }

        if (AssetDatabase.IsValidFolder(FolderPath) == false)
        {
            AssetDatabase.CreateFolder("Assets/Modules", "TalkScriptableObject");
        }

        return FolderPath;
    }

    internal static TalkScriptableObject GetOrCreateTalkAsset(int roleId)
    {
        string folder = EnsureFolder();
        string assetPath = $"{folder}/Role_{roleId:000}_Talk.asset";
        var talkAsset = AssetDatabase.LoadAssetAtPath<TalkScriptableObject>(assetPath);
        if (talkAsset == null)
        {
            talkAsset = ScriptableObject.CreateInstance<TalkScriptableObject>();
            AssetDatabase.CreateAsset(talkAsset, assetPath);
        }

        return talkAsset;
    }

    internal static TalkScriptableObject GetOrCreateTalkAssetByName(string assetName)
    {
        string assetPath = BuildAssetPath(assetName);
        var talkAsset = AssetDatabase.LoadAssetAtPath<TalkScriptableObject>(assetPath);
        if (talkAsset == null)
        {
            talkAsset = ScriptableObject.CreateInstance<TalkScriptableObject>();
            AssetDatabase.CreateAsset(talkAsset, assetPath);
        }

        return talkAsset;
    }

    internal static TalkScriptableObject LoadTalkAssetByName(string assetName)
    {
        string assetPath = BuildAssetPath(assetName);
        return AssetDatabase.LoadAssetAtPath<TalkScriptableObject>(assetPath);
    }

    internal static void ApplyGroups(TalkScriptableObject asset, IReadOnlyList<TalkGroup> groups)
    {
        if (asset == null)
        {
            return;
        }

        asset.Editor_SetGroups(groups);
        EditorUtility.SetDirty(asset);
    }

    internal static int GetRoleId(RoleData role)
    {
        if (role == null)
        {
            return -1;
        }

        return role.roleSortId;
    }

    internal static bool TryAssignToRole(TalkScriptableObject talkAsset, string assetName, IDictionary<int, RoleData> roleLookup = null)
    {
        if (talkAsset == null)
        {
            return false;
        }

        int roleId = ExtractRoleIdFromName(assetName);
        if (roleId < 0)
        {
            return false;
        }

        roleLookup ??= BuildRoleLookup();
        if (roleLookup != null && roleLookup.TryGetValue(roleId, out RoleData roleData) && roleData != null)
        {
            if (roleData.TalkScriptableObject != talkAsset)
            {
                roleData.Editor_SetTalkScriptableObject(talkAsset);
                DatabaseEditorUtils.SaveData(roleData);
            }

            return true;
        }

        return false;
    }

    private static Dictionary<int, RoleData> BuildRoleLookup()
    {
        var roles = DatabaseEditorUtils.GetDatas<RoleData>();
        var lookup = new Dictionary<int, RoleData>();
        foreach (RoleData role in roles)
        {
            int id = GetRoleId(role);
            if (id >= 0 && lookup.ContainsKey(id) == false)
            {
                lookup.Add(id, role);
            }
        }

        return lookup;
    }

    internal static int ExtractRoleIdFromName(string assetName)
    {
        if (string.IsNullOrEmpty(assetName))
        {
            return -1;
        }

        Match match = Regex.Match(assetName, @"\d+");
        if (match.Success && int.TryParse(match.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
        {
            return parsed;
        }

        return -1;
    }

    private static string BuildAssetPath(string assetName)
    {
        string folder = EnsureFolder();
        string fileName = assetName.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) ? assetName : assetName + ".asset";
        return Path.Combine(folder, fileName).Replace("\\", "/");
    }
}
#endif
