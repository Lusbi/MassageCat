#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GameCore.AssetPostProcessor
{
    /// <summary>
    /// 將 TextureSettings 資料轉成 Sprite
    /// </summary>
    public class UIArtPostProcessor : GameCoreAssetPostprocessor
    {
        public override AssetPostProcessorType AssetPostProcessorType => AssetPostProcessorType.UI_Sprite;

        void OnPostprocessTexture(Texture2D texture)
        {
            if (Valid(assetImporter) == false)
            {
                return;
            }

            Execute();
        }

        public override void Execute()
        {
            TextureImporter importer = assetImporter as TextureImporter;

            // 將所有圖片的 Texture Type 設置為 Sprite
            importer.textureType = TextureImporterType.Sprite;

            Debug.Log($"自動調整項目：{importer.name} -> {AssetDatabase.GetAssetPath(importer)}");
        }
    }
}
#endif