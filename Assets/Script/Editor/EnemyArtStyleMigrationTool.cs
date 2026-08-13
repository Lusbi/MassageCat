#if UNITY_EDITOR
using System;
using System.IO;
using GameCore.Database;
using UnityEditor;
using UnityEngine;

public static class EnemyArtStyleMigrationTool
{
    private const string k_enemyArtRoot = "Assets/2DArt/Mob";
    private const string k_roleRepositoryPath =
        "Assets/AssetResources/Database/Lusbi/RoleRepository.asset";
    private const int k_expectedEnemyCount = 58;

    [MenuItem("Tools/Massage Cat/敵人圖像/套用一致化匯入設定並驗證")]
    public static void ConfigureAndValidate()
    {
        string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D Mob_", new[] { k_enemyArtRoot });
        if (textureGuids.Length != k_expectedEnemyCount)
        {
            throw new InvalidOperationException(
                $"敵人圖像數量錯誤：預期 {k_expectedEnemyCount}，實際 {textureGuids.Length}。");
        }

        int normalCount = 0;
        int bossCount = 0;
        for (int i = 0; i < textureGuids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(textureGuids[i]);
            ConfigureTextureImporter(assetPath);

            string fileName = Path.GetFileNameWithoutExtension(assetPath);
            bool isBoss = fileName.EndsWith("11", StringComparison.Ordinal) ||
                          fileName == "Mob_1011" ||
                          fileName == "Mob_1012" ||
                          fileName == "Mob_1013";
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            int expectedSize = isBoss ? 320 : 180;

            if (texture == null || texture.width != expectedSize || texture.height != expectedSize)
            {
                throw new InvalidOperationException(
                    $"敵人圖像尺寸錯誤：{assetPath}，預期 {expectedSize}x{expectedSize}。");
            }

            if (sprite == null)
            {
                throw new InvalidOperationException($"敵人 Sprite 匯入失敗：{assetPath}");
            }

            if (isBoss)
            {
                bossCount++;
            }
            else
            {
                normalCount++;
            }
        }

        ValidateRepositoryReferences();
        Debug.Log(
            $"ENEMY_ART_IMPORT_OK count={textureGuids.Length} normal={normalCount} boss={bossCount}");
    }

    private static void ConfigureTextureImporter(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            throw new InvalidOperationException($"找不到敵人圖像匯入器：{assetPath}");
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.isReadable = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = 512;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.compressionQuality = 100;
        importer.spritePixelsPerUnit = 100.0f;
        importer.SaveAndReimport();
    }

    private static void ValidateRepositoryReferences()
    {
        RoleRepository repository =
            AssetDatabase.LoadAssetAtPath<RoleRepository>(k_roleRepositoryPath);
        if (repository == null)
        {
            throw new InvalidOperationException($"找不到角色資料庫：{k_roleRepositoryPath}");
        }

        if (repository.ReadOnlyList.Count != k_expectedEnemyCount)
        {
            throw new InvalidOperationException(
                $"角色資料筆數錯誤：預期 {k_expectedEnemyCount}，實際 {repository.ReadOnlyList.Count}。");
        }

        for (int i = 0; i < repository.ReadOnlyList.Count; i++)
        {
            RoleData roleData = repository.ReadOnlyList[i];
            if (roleData == null || roleData.EnemyIcon == null)
            {
                throw new InvalidOperationException($"第 {i + 1} 筆角色缺少敵人圖像引用。");
            }

            string iconPath = AssetDatabase.GetAssetPath(roleData.EnemyIcon);
            if (!iconPath.StartsWith(k_enemyArtRoot + "/", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"角色圖像不在一致化目錄內：{roleData.name} -> {iconPath}");
            }
        }

        Debug.Log($"ENEMY_ART_REPOSITORY_OK references={repository.ReadOnlyList.Count}");
    }
}
#endif
