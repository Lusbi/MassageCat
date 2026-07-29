#if UNITY_EDITOR
using System;
using System.IO;
using GameCore.UI;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;
using UnityEngine.UI;

internal static class GalleryButtonPrefabMigrationTool
{
    private const string k_inBattlePrefabPath = "Assets/2DArt/UI/InBattle.prefab";
    private const string k_galleryIconPath = "Assets/2DArt/UI/kenney_game-icons/PNG/White/2x/menuGrid.png";
    private const string k_localizationCollectionName = "LocalizationCollection";
    private const string k_galleryButtonTypeName = "MassageCat.Gallery.GalleryButton, Assembly-CSharp";
    private const string k_unlockFlagKey = "擊殺_紙箱";
    private const float k_horizontalSpacing = 72f;

    [MenuItem("Massage Cat/Gallery/Migrate In-Game Gallery Button")]
    internal static void Migrate()
    {
        PrepareGalleryIcon();
        AddLocalizationEntries();

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(k_inBattlePrefabPath);
        try
        {
            TomatoButton tomatoButton = prefabRoot.GetComponentInChildren<TomatoButton>(true);
            BattlePanel battlePanel = prefabRoot.GetComponent<BattlePanel>();
            if (tomatoButton == null || battlePanel == null)
                throw new InvalidOperationException("InBattle Prefab 缺少蕃茄鐘按鈕或 BattlePanel。");

            Type galleryButtonType = Type.GetType(k_galleryButtonTypeName, true);
            Transform buttonParent = tomatoButton.transform.parent;
            Transform previousGallery = buttonParent.Find("Gallery");
            if (previousGallery != null)
                UnityEngine.Object.DestroyImmediate(previousGallery.gameObject);

            RectTransform tomatoRect = (RectTransform)tomatoButton.transform;
            GameObject galleryObject = new GameObject("Gallery", typeof(RectTransform), typeof(CanvasGroup));
            RectTransform galleryRect = (RectTransform)galleryObject.transform;
            galleryRect.SetParent(buttonParent, false);
            galleryRect.anchorMin = tomatoRect.anchorMin;
            galleryRect.anchorMax = tomatoRect.anchorMax;
            galleryRect.pivot = tomatoRect.pivot;
            galleryRect.sizeDelta = tomatoRect.sizeDelta;
            galleryRect.anchoredPosition = tomatoRect.anchoredPosition + new Vector2(k_horizontalSpacing, 0f);

            CanvasGroup canvasGroup = galleryObject.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            Image icon = CreateIcon(galleryRect);
            UIButton button = CreateClickTarget(galleryRect);
            Component galleryButton = galleryObject.AddComponent(galleryButtonType);
            BindGalleryButton(galleryButton, canvasGroup, button);

            SerializedObject battlePanelObject = new SerializedObject(battlePanel);
            battlePanelObject.FindProperty("m_galleryButton").objectReferenceValue = galleryButton;
            battlePanelObject.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, k_inBattlePrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        BatchValidateGalleryButton();
    }

    public static void BatchMigrateInGameGalleryButton()
    {
        Migrate();
    }

    [MenuItem("Massage Cat/Gallery/Validate In-Game Gallery Button")]
    public static void BatchValidateGalleryButton()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(k_inBattlePrefabPath);
        try
        {
            TomatoButton tomatoButton = prefabRoot.GetComponentInChildren<TomatoButton>(true);
            Transform gallery = tomatoButton != null ? tomatoButton.transform.parent.Find("Gallery") : null;
            if (tomatoButton == null || gallery == null)
                throw new InvalidOperationException("找不到蕃茄鐘右側的 Gallery 按鈕。");

            RectTransform tomatoRect = (RectTransform)tomatoButton.transform;
            RectTransform galleryRect = (RectTransform)gallery;
            Vector2 expectedPosition = tomatoRect.anchoredPosition + new Vector2(k_horizontalSpacing, 0f);
            if ((galleryRect.anchoredPosition - expectedPosition).sqrMagnitude > 0.01f)
                throw new InvalidOperationException("Gallery 按鈕未位於蕃茄鐘右側。");

            CanvasGroup canvasGroup = gallery.GetComponent<CanvasGroup>();
            UIButton button = gallery.GetComponentInChildren<UIButton>(true);
            Image[] images = gallery.GetComponentsInChildren<Image>(true);
            bool hasNonRaycastIcon = false;
            bool hasRaycastTarget = false;
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                hasNonRaycastIcon |= image.sprite != null && image.raycastTarget == false;
                hasRaycastTarget |= image.raycastTarget;
            }

            if (canvasGroup == null || button == null || hasNonRaycastIcon == false || hasRaycastTarget == false)
                throw new InvalidOperationException("Gallery 按鈕的顯示或點擊元件不完整。");

            Type galleryButtonType = Type.GetType(k_galleryButtonTypeName, true);
            Component galleryButton = gallery.GetComponent(galleryButtonType);
            BattlePanel battlePanel = prefabRoot.GetComponent<BattlePanel>();
            SerializedObject battlePanelObject = new SerializedObject(battlePanel);
            if (battlePanelObject.FindProperty("m_galleryButton").objectReferenceValue != galleryButton)
                throw new InvalidOperationException("BattlePanel 尚未綁定 Gallery 按鈕。");

            TextureImporter importer = AssetImporter.GetAtPath(k_galleryIconPath) as TextureImporter;
            if (importer == null || importer.spriteImportMode != SpriteImportMode.Single)
                throw new InvalidOperationException("圖鑑圖示未設定為單一 Sprite。");

            ValidateLocalizationEntry("Gallery.LaunchFailedTitle");
            ValidateLocalizationEntry("Gallery.LaunchFailedContent");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        string controllerSource = File.ReadAllText("Assets/Script/DesktopPet/DesktopPetWindowController.cs");
        if (controllerSource.Contains("DesktopPetGalleryButton", StringComparison.Ordinal))
            throw new InvalidOperationException("舊的桌寵圖鑑覆蓋按鈕仍存在。");

        Debug.Log("一般遊戲圖鑑按鈕驗證通過。");
    }

    private static void PrepareGalleryIcon()
    {
        TextureImporter importer = AssetImporter.GetAtPath(k_galleryIconPath) as TextureImporter;
        if (importer == null)
            throw new FileNotFoundException("找不到圖鑑圖示。", k_galleryIconPath);

        if (importer.spriteImportMode == SpriteImportMode.Single)
            return;

        importer.spriteImportMode = SpriteImportMode.Single;
        importer.SaveAndReimport();
    }

    private static Image CreateIcon(RectTransform parent)
    {
        Sprite iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(k_galleryIconPath);
        if (iconSprite == null)
            throw new InvalidOperationException("無法載入圖鑑圖示 Sprite。");

        GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform iconRect = (RectTransform)iconObject.transform;
        iconRect.SetParent(parent, false);
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(44f, 44f);

        Image icon = iconObject.GetComponent<Image>();
        icon.sprite = iconSprite;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        return icon;
    }

    private static UIButton CreateClickTarget(RectTransform parent)
    {
        GameObject clickObject = new GameObject("Click", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform clickRect = (RectTransform)clickObject.transform;
        clickRect.SetParent(parent, false);
        clickRect.anchorMin = Vector2.zero;
        clickRect.anchorMax = Vector2.one;
        clickRect.offsetMin = Vector2.zero;
        clickRect.offsetMax = Vector2.zero;

        Image clickImage = clickObject.GetComponent<Image>();
        clickImage.color = Color.clear;
        clickImage.raycastTarget = true;
        return clickObject.AddComponent<UIButton>();
    }

    private static void BindGalleryButton(Component galleryButton, CanvasGroup canvasGroup, UIButton button)
    {
        SerializedObject serializedButton = new SerializedObject(galleryButton);
        serializedButton.FindProperty("m_canvasGroup").objectReferenceValue = canvasGroup;
        serializedButton.FindProperty("m_button").objectReferenceValue = button;
        SerializedProperty flagReference = serializedButton.FindProperty("m_flagReference");
        flagReference.FindPropertyRelative("m_key").stringValue = k_unlockFlagKey;
        serializedButton.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddLocalizationEntries()
    {
        StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(k_localizationCollectionName);
        if (collection == null)
            throw new InvalidOperationException("找不到 LocalizationCollection。");

        foreach (StringTable table in collection.StringTables)
        {
            SetLocalizationEntry(table, "Gallery.LaunchFailedTitle", "無法開啟圖鑑");
            SetLocalizationEntry(table, "Gallery.LaunchFailedContent", "找不到圖鑑程式或啟動失敗，請確認 MassageCatGallery.exe 與主程式位於相同資料夾。");
            EditorUtility.SetDirty(table);
            EditorUtility.SetDirty(table.SharedData);
        }
    }

    private static void SetLocalizationEntry(StringTable table, string key, string value)
    {
        StringTableEntry entry = table.GetEntry(key) ?? table.AddEntry(key, value);
        entry.Value = value;
    }

    private static void ValidateLocalizationEntry(string key)
    {
        StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(k_localizationCollectionName);
        if (collection == null)
            throw new InvalidOperationException("找不到 LocalizationCollection。");

        foreach (StringTable table in collection.StringTables)
        {
            StringTableEntry entry = table.GetEntry(key);
            if (entry == null || string.IsNullOrEmpty(entry.Value))
                throw new InvalidOperationException($"缺少本地化字串：{key}");
        }
    }
}
#endif
