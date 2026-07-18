using GameCore.Database;
using GameCore.Database.Editor;
using UnityEditor;

public class SpriteUpdate
{
    [MenuItem("Tools/角色圖片載入")]
    private static void RoleTransfer()
    {
        var roles = DatabaseEditorUtils.GetDatas<RoleData>();
        foreach (var role in roles)
        {
            // 轉換角色圖片
            role.SetSprite();
            DatabaseEditorUtils.SaveData(role);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/場景圖片載入")]
    private static void SceneMapTransfer()
    {
        var sceneMaps = DatabaseEditorUtils.GetDatas<ScenemapData>();
        foreach (var sceneMap in sceneMaps)
        {
            // 轉換角色圖片
            sceneMap.SetSprite();
            DatabaseEditorUtils.SaveData(sceneMap);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
