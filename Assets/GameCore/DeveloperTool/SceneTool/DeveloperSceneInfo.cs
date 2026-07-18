#if UNITY_EDITOR
using Sirenix.OdinInspector;
using UnityEditor.SceneManagement;
using UnityEngine;

[System.Serializable]
public struct DeveloperSceneInfo
{
    [LabelText("註釋")]
    public string comment;

    [LabelText("場景路徑"), FilePath(Extensions = ".unity" , ParentFolder = "Assets/")]
    public string scenePath;

    [Button("開啟場景")]
    private void OpenScene()
    {
        if (string.IsNullOrEmpty(scenePath))
        {
            Debug.LogError("場景路徑為空");
            return;
        }

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(scenePath);
            Debug.Log($"已開啟場景: {scenePath}");
        }
    }
}
#endif