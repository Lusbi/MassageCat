#if UNITY_EDITOR
using GameCore;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeveloperScene : DeveloperToolBase
{
    public override string DeveloperName => "快速場景工具";

    private const string DATA_PATH = "Assets/GameCore/DeveloperTool/SceneTool/SceneToolData.asset";

    private DeveloperSceneToolData _sceneToolData;

    [TitleGroup("場景列表")]
    [TableList, ShowInInspector]
    public List<DeveloperSceneInfo> developerSceneInfos
    {
        get
        {
            LoadData();
            return _sceneToolData.developerSceneInfos;
        }
    }

    public override void Initlization()
    {
        base.Initlization();
        LoadData();
    }

    private void LoadData()
    {
        if (_sceneToolData == null)
        {
            _sceneToolData = AssetDatabase.LoadAssetAtPath<DeveloperSceneToolData>(DATA_PATH);

            // 如果數據資產不存在，則創建一個
            if (_sceneToolData == null)
            {
                _sceneToolData = ScriptableObject.CreateInstance<DeveloperSceneToolData>();

                // 確保目錄存在
                string directory = Path.GetDirectoryName(DATA_PATH);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                AssetDatabase.CreateAsset(_sceneToolData, DATA_PATH);
                AssetDatabase.SaveAssets();
            }
        }
    }

    private void SaveData()
    {
        if (_sceneToolData != null)
        {
            EditorUtility.SetDirty(_sceneToolData);
            AssetDatabase.SaveAssets();
        }
    }

    [TitleGroup("常用場景")]
    [Button("開啟主場景", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1)]
    public void OpenMainScene()
    {
        OpenScenePath("Assets/Scenes/Main.unity");
    }

    [TitleGroup("場景操作")]
    [Button("添加當前場景"), GUIColor(0.7f, 1, 0.7f)]
    public void AddCurrentScene()
    {
        LoadData();

        Scene currentScene = SceneManager.GetActiveScene();
        if (!string.IsNullOrEmpty(currentScene.path))
        {
            // 檢查是否已存在相同路徑的場景
            bool exists = _sceneToolData.developerSceneInfos.Exists(info => info.scenePath == currentScene.path);

            if (!exists)
            {
                DeveloperSceneInfo newInfo = new DeveloperSceneInfo
                {
                    comment = currentScene.name,
                    scenePath = currentScene.path
                };

                _sceneToolData.developerSceneInfos.Add(newInfo);
                SaveData();
                Debug.Log($"已添加場景: {currentScene.name}");
            }
            else
            {
                Debug.Log($"場景 {currentScene.name} 已在列表中");
            }
        }
        else
        {
            Debug.LogWarning("當前場景未保存，無法添加");
        }
    }

    [TitleGroup("場景操作")]
    [Button("清空場景列表"), GUIColor(1, 0.7f, 0.7f)]
    public void ClearSceneList()
    {
        if (EditorUtility.DisplayDialog("確認清空", "確定要清空所有場景列表嗎？", "確定", "取消"))
        {
            LoadData();
            _sceneToolData.developerSceneInfos.Clear();
            SaveData();
            Debug.Log("場景列表已清空");
        }
    }

    private void OpenScenePath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("場景路徑為空");
            return;
        }

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(path);
            Debug.Log($"已開啟場景: {path}");
        }
    }
}
#endif