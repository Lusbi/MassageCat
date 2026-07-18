#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using GameCore.Database;
using GameCore.Database.Editor;
using UnityEditor;
using UnityEngine;

public static class TalkScriptableObjectSheetImporter
{
    private const float DefaultDuration = 5.0f;

    internal static void Import(string rawContent)
    {
        if (string.IsNullOrWhiteSpace(rawContent))
        {
            Debug.LogWarning("Talk sheet content is empty.");
            return;
        }

        TalkScriptableObjectEditorUtility.EnsureFolder();

        // 結構：Dictionary<RoleName, Dictionary<TalkType, List<TalkEntry>>>
        var assetEntries = new Dictionary<string, Dictionary<TalkType, List<TalkEntry>>>();

        string[] lines = rawContent.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
        foreach (string rawLine in lines)
        {
            if (string.IsNullOrWhiteSpace(rawLine))
                continue;

            string[] columns = rawLine.Split('\t');
            if (columns.Length <= 1)
                columns = rawLine.Split(',');

            if (columns.Length == 0)
                continue;

            string key = columns[0].Trim();
            if (string.IsNullOrEmpty(key))
                continue;

            string[] parts = key.Split('.');
            if (parts.Length < 4 || parts[0] != "對話")
                continue;

            string roleName = parts[1];
            string talkTypeStr = parts[2];
            TalkType talkType = ParseTalkType(talkTypeStr);

            if (!assetEntries.ContainsKey(roleName))
                assetEntries[roleName] = new Dictionary<TalkType, List<TalkEntry>>();

            if (!assetEntries[roleName].ContainsKey(talkType))
                assetEntries[roleName][talkType] = new List<TalkEntry>();

            assetEntries[roleName][talkType].Add(new TalkEntry(key, DefaultDuration));
        }

        var roles = DatabaseEditorUtils.GetDatas<RoleData>();

        foreach (var kvp in assetEntries)
        {
            string importedRoleName = kvp.Key;

            RoleData targetRole = null;
            foreach (var role in roles)
            {
                if ((!string.IsNullOrEmpty(role.name) && role.name.Contains(importedRoleName)) ||
                    (!string.IsNullOrEmpty(role.RoleName) && role.RoleName.Contains(importedRoleName)))
                {
                    targetRole = role;
                    break;
                }
            }

            if (targetRole == null)
            {
                Debug.LogWarning($"[TalkScriptableObjectSheetImporter] Cannot find Role matching name: {importedRoleName}");
                continue;
            }

            int roleId = targetRole.roleSortId;
            string assetName = $"Role_{roleId:000}_Talk";

            TalkScriptableObject asset = TalkScriptableObjectEditorUtility.GetOrCreateTalkAssetByName(assetName);
            
            if (targetRole.TalkScriptableObject != asset)
            {
                targetRole.Editor_SetTalkScriptableObject(asset);
                DatabaseEditorUtils.SaveData(targetRole);
            }

            // 【合併邏輯】：保留現有的 Condition，只更新 Entries
            var existingGroupsMap = new Dictionary<TalkType, TalkCondition>();
            if (asset.Groups != null)
            {
                foreach (var grp in asset.Groups)
                {
                    existingGroupsMap[grp.TalkType] = grp.Condition;
                }
            }

            var mergedGroups = new List<TalkGroup>();
            foreach (var groupKvp in kvp.Value)
            {
                TalkType tType = groupKvp.Key;
                List<TalkEntry> newEntries = groupKvp.Value;

                TalkCondition preservedCondition = default(TalkCondition);
                if (existingGroupsMap.ContainsKey(tType))
                {
                    preservedCondition = existingGroupsMap[tType];
                }

                mergedGroups.Add(new TalkGroup(tType, preservedCondition, newEntries));
            }

            TalkScriptableObjectEditorUtility.ApplyGroups(asset, mergedGroups);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Talk Scriptable Objects grouped and imported successfully.");
    }

    private static TalkType ParseTalkType(string typeStr)
    {
        if (Enum.TryParse<TalkType>(typeStr, true, out var result))
            return result;

        switch (typeStr)
        {
            case "初次遇到玩家": return TalkType.FirstEncounter;
            case "按摩時": return TalkType.DuringMassage;
            case "舒緩度<50%": return TalkType.FatigueLessThan50;
            case "特殊事件": return TalkType.SpecialEvent;
            case "舒緩度<10%": return TalkType.FatigueLessThan10;
            case "被其它魔物推薦": return TalkType.Recommended;
            case "戰鬥超過30秒": return TalkType.BattleOver30Seconds;
            case "服務完成": return TalkType.ServiceComplete;
            case "蕃茄鐘專屬對話": return TalkType.TomatoModeExclusive;
            case "要求按摩快一點": return TalkType.RequestFasterMassage;
            default:
                if (!string.IsNullOrEmpty(typeStr))
                    Debug.LogWarning($"[TalkScriptableObjectSheetImporter] Unknown TalkType: {typeStr}, fallback to DuringMassage.");
                return TalkType.DuringMassage;
        }
    }
}
#endif
