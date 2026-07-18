#if UNITY_EDITOR
using System.Collections.Generic;
using GameCore;
using GameCore.Database;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

public class DataRepositoryTool : DeveloperToolBase
{
    public override string DeveloperName => "Data Repository 工具";

    [Title("Data Repository 列表")]
    [ValueDropdown(nameof(AllDataRepositories))]
    [OnValueChanged(nameof(OnDataRepositorySelected))]
    [LabelText("選擇 Data Repository")]
    public ScriptableObject selectedDataRepository;

    [ShowIf(nameof(selectedDataRepository))]
    [InlineEditor(InlineEditorObjectFieldModes.Hidden)]
    [LabelText("Data Repository 詳細內容")]
    public ScriptableObject selectedDataRepositoryDetails;

    private List<ScriptableObject> AllDataRepositories()
    {
        // 搜索專案中所有繼承自 Data 的 ScriptableObject
        string[] guids = AssetDatabase.FindAssets("t:Repository");
        List<ScriptableObject> repositories = new List<ScriptableObject>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ScriptableObject obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

            if (obj is Repository)
            {
                repositories.Add(obj);
            }
        }

        return repositories;
    }

    private void OnDataRepositorySelected()
    {
        selectedDataRepositoryDetails = selectedDataRepository;
    }
}
#endif
