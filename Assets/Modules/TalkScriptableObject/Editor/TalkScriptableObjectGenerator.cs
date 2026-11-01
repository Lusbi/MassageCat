#if UNITY_EDITOR
using GameCore.Database;
using GameCore.Database.Editor;
using UnityEditor;

public static class TalkScriptableObjectGenerator
{
    [MenuItem("Tools/Talk/Generate Role Talk Assets")]
    private static void GenerateRoleTalkAssets()
    {
        TalkScriptableObjectEditorUtility.EnsureFolder();
        var roles = DatabaseEditorUtils.GetDatas<RoleData>();
        bool updated = false;

        foreach (RoleData role in roles)
        {
            int roleId = TalkScriptableObjectEditorUtility.GetRoleId(role);
            if (roleId < 0)
            {
                continue;
            }

            TalkScriptableObject talkAsset = TalkScriptableObjectEditorUtility.GetOrCreateTalkAsset(roleId);
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
