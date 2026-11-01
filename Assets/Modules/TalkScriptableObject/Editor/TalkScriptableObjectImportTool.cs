using GameCore.Database;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "TalkScriptableObjectImportTool", menuName = "Scriptable Objects/Talk Scriptable Object Import Tool")]
public class TalkScriptableObjectImportTool : ScriptableObject
{
    [SerializeField]
    private GoogleSheetDownloaderReference googleSheetDownloaderReference;

#if UNITY_EDITOR
    [Button("Download & Import")]
    public void DownloadAndImport()
    {
        if (googleSheetDownloaderReference == null)
        {
            Debug.LogError("GoogleSheetDownloaderReference is not assigned.");
            return;
        }

        googleSheetDownloaderReference.Setup(OnDownloadProgress, OnDownloadFinished);
        googleSheetDownloaderReference.DownLoad();
    }

    private void OnDownloadProgress(float progress)
    {
        UnityEditor.EditorUtility.DisplayProgressBar("Talk Scriptable Object Import", "Downloading Google Sheet...", progress);
    }

    private void OnDownloadFinished(string content)
    {
        UnityEditor.EditorUtility.ClearProgressBar();
        TalkScriptableObjectSheetImporter.Import(content);
    }
#endif
}
