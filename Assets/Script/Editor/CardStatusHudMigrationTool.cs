#if UNITY_EDITOR
using System;
using GameCore.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 將既有卡片提示圖示替換為固定的血條右側狀態圖示，並保留 CardHud 的懸停說明。
/// </summary>
internal static class CardStatusHudMigrationTool
{
    private const string k_inBattlePrefabPath = "Assets/2DArt/UI/InBattle.prefab";
    private const string k_cardStatusPath = "Assets/2DArt/DesktopPet/ChibiPaper/controls_atlas_v1.png";
    private static readonly Vector2 s_cardIconSize = new Vector2(72f, 84f);
    private static readonly Vector2 s_cardIconPosition = new Vector2(6f, 0f);

    [MenuItem("Tools/Massage Cat/Migrate Card Status HUD")]
    public static void Migrate()
    {
        ConfigureCardStatusSprite();
        Sprite cardStatusSprite = ChibiPaperArtMigration.LoadNamedSprite(k_cardStatusPath, "CardStatus");
        if (cardStatusSprite == null)
            throw new InvalidOperationException("找不到卡片狀態圖示 Sprite。\n" + k_cardStatusPath);

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(k_inBattlePrefabPath);
        try
        {
            RoleSpawnHUD roleSpawnHud = prefabRoot.GetComponentInChildren<RoleSpawnHUD>(true);
            CardHud cardHud = prefabRoot.GetComponentInChildren<CardHud>(true);
            RectTransform progressRoot = FindTransform(prefabRoot.transform, "HealProgressRoot") as RectTransform;
            if (roleSpawnHud == null || cardHud == null || progressRoot == null)
                throw new InvalidOperationException("InBattle.prefab 缺少 RoleSpawnHUD、CardHud 或 HealProgressRoot。\n");

            SerializedObject serializedCardHud = new SerializedObject(cardHud);
            Image cardImage = serializedCardHud.FindProperty("m_cardImg").objectReferenceValue as Image;
            UIButton cardButton = serializedCardHud.FindProperty("m_cardBtn").objectReferenceValue as UIButton;
            if (cardImage == null || cardButton == null)
                throw new InvalidOperationException("CardHud 缺少既有的卡片圖示或懸停目標。\n");

            ConfigureCardIcon(cardImage, progressRoot, cardStatusSprite);

            SerializedObject serializedRoleHud = new SerializedObject(roleSpawnHud);
            serializedRoleHud.FindProperty("m_cardHud").objectReferenceValue = cardHud;
            serializedRoleHud.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, k_inBattlePrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Validate();
        Debug.Log("卡片狀態圖示 Prefab 遷移與驗證完成。");
    }

    public static void BatchMigrateCardStatusHud()
    {
        Migrate();
    }

    [MenuItem("Tools/Massage Cat/Validate Card Status HUD")]
    public static void Validate()
    {
        TextureImporter importer = AssetImporter.GetAtPath(k_cardStatusPath) as TextureImporter;
        if (importer == null || importer.textureType != TextureImporterType.Sprite)
            throw new InvalidOperationException("卡片狀態圖示未正確匯入為 Sprite。\n");

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(k_inBattlePrefabPath);
        try
        {
            RoleSpawnHUD roleSpawnHud = prefabRoot.GetComponentInChildren<RoleSpawnHUD>(true);
            CardHud cardHud = prefabRoot.GetComponentInChildren<CardHud>(true);
            RectTransform progressRoot = FindTransform(prefabRoot.transform, "HealProgressRoot") as RectTransform;
            if (roleSpawnHud == null || cardHud == null || progressRoot == null)
                throw new InvalidOperationException("InBattle.prefab 的卡片狀態節點不完整。\n");

            SerializedObject serializedRoleHud = new SerializedObject(roleSpawnHud);
            if (serializedRoleHud.FindProperty("m_cardHud").objectReferenceValue != cardHud)
                throw new InvalidOperationException("RoleSpawnHUD 尚未綁定 CardHud。\n");

            SerializedObject serializedCardHud = new SerializedObject(cardHud);
            Image cardImage = serializedCardHud.FindProperty("m_cardImg").objectReferenceValue as Image;
            UIButton cardButton = serializedCardHud.FindProperty("m_cardBtn").objectReferenceValue as UIButton;
            if (cardImage == null
                || cardButton == null
                || cardImage.rectTransform.parent != progressRoot
                || string.Equals(AssetDatabase.GetAssetPath(cardImage.sprite), k_cardStatusPath, StringComparison.Ordinal) == false
                || cardImage.preserveAspect == false
                || cardImage.raycastTarget == false
                || Vector2.Distance(cardImage.rectTransform.sizeDelta, s_cardIconSize) > 0.01f)
            {
                throw new InvalidOperationException("卡片狀態圖示未位於血條右側或未保留懸停目標。\n");
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        Debug.Log("卡片狀態圖示驗證通過。");
    }

    private static void ConfigureCardStatusSprite()
    {
        AssetDatabase.ImportAsset(
            k_cardStatusPath,
            ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(k_cardStatusPath) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException("無法取得卡片狀態圖示的匯入器。\n");

        // 共用圖集中保留既有切片，不可改為單張 Sprite。
        ChibiPaperArtMigration.LoadNamedSprite(k_cardStatusPath, "CardStatus");
    }

    private static void ConfigureCardIcon(Image cardImage, RectTransform progressRoot, Sprite cardStatusSprite)
    {
        RectTransform iconRect = cardImage.rectTransform;
        iconRect.SetParent(progressRoot, false);
        iconRect.name = "CardStatus";
        iconRect.anchorMin = new Vector2(1f, 0.5f);
        iconRect.anchorMax = new Vector2(1f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = s_cardIconPosition;
        iconRect.sizeDelta = s_cardIconSize;
        iconRect.localScale = Vector3.one;
        iconRect.SetAsLastSibling();

        cardImage.sprite = cardStatusSprite;
        cardImage.color = Color.white;
        cardImage.preserveAspect = true;
        cardImage.raycastTarget = true;
    }

    private static Transform FindTransform(Transform root, string name)
    {
        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindTransform(root.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }
}
#endif
