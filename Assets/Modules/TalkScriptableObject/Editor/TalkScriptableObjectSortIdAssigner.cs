#if UNITY_EDITOR
using GameCore.Database;
using GameCore.Database.Editor;
using UnityEditor;
using UnityEngine;

public static class TalkScriptableObjectSortIdAssigner
{
    [MenuItem("Tools/Talk/Assign Role Talk Assets By Sort Id")]
    private static void AssignRoleTalkAssetsBySortId()
    {
        TalkScriptableObjectEditorUtility.EnsureFolder();
        var roles = DatabaseEditorUtils.GetDatas<RoleData>();
        bool updated = false;

        foreach (RoleData role in roles)
        {
            if (role == null)
            {
                continue;
            }

            int sortId = role.roleSortId;
            if (sortId < 0)
            {
                continue;
            }

            string assetName = $"Role_{sortId:000}_Talk";
            TalkScriptableObject talkAsset = TalkScriptableObjectEditorUtility.LoadTalkAssetByName(assetName);
            if (talkAsset == null)
            {
                Debug.LogWarning($"Talk asset not found for sort id {sortId}: {assetName}.asset");
                continue;
            }

            if (role.TalkScriptableObject != talkAsset)
            {
                role.Editor_SetTalkScriptableObject(talkAsset);
                DatabaseEditorUtils.SaveData(role);
                updated = true;
            }
        }

        if (updated)
        {
            AssetDatabase.SaveAssets();
        }

        AssetDatabase.Refresh();
    }
}
#endif
