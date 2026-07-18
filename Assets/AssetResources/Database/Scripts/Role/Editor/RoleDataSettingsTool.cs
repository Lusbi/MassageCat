using UnityEditor;
using UnityEngine;
using System.Reflection;
using GameCore.Database;
using GameCore.Database.Editor;

namespace GameCore.Database.Editor
{
    public static class RoleDataSettingsTool
    {
        [MenuItem("Tools/Role/FastSettingKillFlag")]
        public static void FastSettingKillFlag()
        {
            int totalCount = 0;
            int updateCount = 0;
            
            // 使用專案提供的 EnumerateData<T>() 來取得 Repository 內的所有資料，
            // 若只用 FindAssets 會導致只抓到作為 Sub-asset 的第一筆。
            foreach (RoleData roleData in DatabaseEditorUtils.EnumerateData<RoleData>())
            {
                if (roleData != null)
                {
                    totalCount++;
                    bool isModified = false;
                    
                    // 取得 RoleData 的私有變數 m_satisfyToAddFlagReference
                    var flagField = typeof(RoleData).GetField("m_satisfyToAddFlagReference", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (flagField != null)
                    {
                        // 取出目前的 FlagReference，如果為空則初始化
                        var flagRef = flagField.GetValue(roleData) as FlagReference;
                        
                        if (flagRef == null)
                        {
                            flagRef = new FlagReference();
                            flagField.SetValue(roleData, flagRef);
                            isModified = true;
                        }
                        
                        // 根據企劃規則，Key值須為 "擊殺_{角色資料的Key}"
                        string expectedKey = $"擊殺_{roleData.key}";
                        
                        // 若原先設定和預期不吻合，就進行自動設定
                        if (flagRef.GetKey() != expectedKey)
                        {
                            flagRef.SetKey(expectedKey);
                            isModified = true;
                        }
                    }

                    // 只有在資料有更動時才標記為 Dirty
                    if (isModified)
                    {
                        EditorUtility.SetDirty(roleData);
                        updateCount++;
                    }
                }
            }
            
            // 儲存所有的 Editor 設定
            if (updateCount > 0)
            {
                AssetDatabase.SaveAssets();
            }
            
            Debug.Log($"自動對應擊殺旗標完成！共檢查了 {totalCount} 筆 RoleData ，其中更新了 {updateCount} 筆資料。");
        }
    }
}
