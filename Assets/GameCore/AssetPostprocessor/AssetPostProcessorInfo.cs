using Sirenix.OdinInspector;

namespace GameCore.AssetPostProcessor
{
    [System.Serializable]
    public class AssetPostProcessorInfo
    {
        [LabelText("類型")] public AssetPostProcessorType assetPostProcessorType;
        [Title("簡述功能、管理項目"), HideLabel, MultiLineProperty(3)] public string assetPostprocessorName;
        [LabelText("是否啟用")] public bool isWork = true;
        [LabelText("指定檢查路徑") , FolderPath] public string[] folderPaths;
    }
}