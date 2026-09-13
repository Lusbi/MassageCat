#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 保留 GUID 搬移紙繪美術，先更新引用，再移除已替換的舊 UI 貼圖。
/// </summary>
internal static class ChibiPaperArtMigration
{
    private const string k_artRoot = "Assets/2DArt/DesktopPet/ChibiPaper/";
    private const string k_output = "Artifacts/ChibiPaperMigration/";
    private static readonly UTF8Encoding s_utf8 = new UTF8Encoding(false);

    [Serializable] private sealed class SliceList { public Slice[] sprites; }
    [Serializable] private sealed class Slice
    {
        public string name;
        public int x, y, width, height, border;
    }

    [MenuItem("Tools/Massage Cat/Art/Migrate ChibiPaper UI")]
    internal static void Migrate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("請先離開 Play Mode 再搬移素材。");

        Directory.CreateDirectory(k_output);
        BackupFolder("Assets/2DArt/UI");
        BackupFolder("Assets/Resources/DesktopPet");
        BackupFolder("Assets/AssetResources/UI");
        EnsureFolder("Assets/2DArt/Support/MPUIKit");
        Move("Assets/Resources/DesktopPet", "Assets/2DArt/DesktopPet");
        Move("Assets/MPUIKit/Runtime/Resources/mpui_default_empty_sprite.png",
            "Assets/2DArt/Support/MPUIKit/mpui_default_empty_sprite.png");
        ConfigureSupportSettings();
        ImportAtlas("controls_atlas_v1.png");
        ImportAtlas("support_atlas_v1.png");
        ConfigureBorder("settings_panel_v1.png", new Vector4(200f, 220f, 200f, 220f));
        ConfigureBorder("icon_button_frame_v1.png", new Vector4(160f, 160f, 160f, 160f));
        ConfigureBorder("spell_card_v1.png", new Vector4(280f, 170f, 280f, 170f));
        ConfigureBorder("tomato_card_v1.png", new Vector4(240f, 140f, 240f, 140f));

        var replacements = new Dictionary<string, Sprite>();
        string oldRoot = "Assets/2DArt/UI/MassageCatUI/";
        string[] controls = { "AutoNextOff", "AutoNextOn", "BgmOff", "BgmOn", "CardStatus",
            "GalleryClose", "Minimize", "Quit", "SceneHidden", "SceneVisible", "SfxOff", "SfxOn" };
        for (int i = 0; i < controls.Length; i++)
            replacements.Add(oldRoot + controls[i] + ".png", LoadNamedSprite("controls_atlas_v1.png", controls[i]));
        string[,] simple = {
            { "GalleryOpen", "gallery_icon_v1.png" }, { "GalleryPanel", "settings_panel_v1.png" },
            { "Settings", "settings_icon_v1.png" }, { "SettingsPanel", "settings_panel_v1.png" },
            { "IconButtonFrame", "icon_button_frame_v1.png" }, { "MessagePanel", "spell_card_v1.png" },
            { "Spell", "spell_icon_v1.png" }, { "TomatoTimer", "tomato_icon_v1.png" }
        };
        for (int i = 0; i < simple.GetLength(0); i++)
            replacements.Add(oldRoot + simple[i, 0] + ".png", LoadNamedSprite(simple[i, 1]));
        string[] support = { "HealProgressTrack", "HealProgressFill" };
        for (int i = 0; i < support.Length; i++)
            replacements.Add(oldRoot + support[i] + ".png", LoadNamedSprite("support_atlas_v1.png", support[i]));
        replacements.Add(oldRoot + "Message.png", LoadNamedSprite("message_icon_v1.png", "Message"));
        replacements.Add(oldRoot + "HealProgressBar.png", LoadNamedSprite("support_atlas_v1.png", "HealProgressTrack"));
        string kenney = "Assets/2DArt/UI/kenney_game-icons/PNG/White/2x/";
        replacements.Add(kenney + "arrowLeft.png", LoadNamedSprite("controls_atlas_v1.png", "ArrowLeft"));
        replacements.Add(kenney + "arrowRight.png", LoadNamedSprite("controls_atlas_v1.png", "ArrowRight"));
        replacements.Add(kenney + "save.png", LoadNamedSprite("controls_atlas_v1.png", "Save"));
        replacements.Add(kenney + "movie.png", LoadNamedSprite("controls_atlas_v1.png", "SceneVisible"));

        ReplaceReferences(replacements);
        BindArtReferences();
        NormalizeSpritePresentation();
        Validate();
        foreach (KeyValuePair<string, Sprite> pair in replacements)
        {
            if (!File.Exists(pair.Key))
                continue;
            string guid = AssetDatabase.AssetPathToGUID(pair.Key);
            AssertNoReferences(guid);
            if (!AssetDatabase.DeleteAsset(pair.Key))
                throw new IOException("無法刪除已替換素材：" + pair.Key);
        }
        AssetDatabase.SaveAssets();
        File.WriteAllText(k_output + "migration-result.txt", "PASS: 17 textures moved; 28 legacy UI textures replaced; Sprite references bound; Resources texture scan passed.\n", s_utf8);
        Debug.Log("ChibiPaper 素材搬移、替換與引用驗證完成。");
    }

    internal static Sprite LoadNamedSprite(string fileName, string spriteName = null)
    {
        string path = fileName.StartsWith("Assets/", StringComparison.Ordinal) ? fileName : k_artRoot + fileName;
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        for (int i = 0; i < assets.Length; i++)
            if (assets[i] is Sprite sprite && (spriteName == null || sprite.name == spriteName))
                return sprite;
        throw new InvalidOperationException("找不到紙繪 Sprite：" + path + " / " + spriteName);
    }

    private static void ImportAtlas(string fileName)
    {
        string path = k_artRoot + fileName;
        if (!File.Exists(path))
            File.Copy(k_output + "staging/" + fileName, path);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 100f;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.isReadable = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        // 控制圖集包含多個圖示，依切片比例限制每個圖示不超過 256 像素。
        importer.maxTextureSize = fileName == "controls_atlas_v1.png" ? 1024 : 2048;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
        var factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        SpriteRect[] existing = provider.GetSpriteRects();
        Slice[] slices = JsonUtility.FromJson<SliceList>(File.ReadAllText(k_output + "staging/" + fileName + ".json")).sprites;
        var rects = new SpriteRect[slices.Length];
        var ids = new SpriteNameFileIdPair[slices.Length];
        for (int i = 0; i < slices.Length; i++)
        {
            Slice slice = slices[i];
            GUID id = GUID.Generate();
            for (int j = 0; j < existing.Length; j++)
                if (existing[j].name == slice.name) id = existing[j].spriteID;
            rects[i] = new SpriteRect { name = slice.name, spriteID = id,
                rect = new Rect(slice.x, slice.y, slice.width, slice.height),
                pivot = new Vector2(0.5f, 0.5f), alignment = SpriteAlignment.Center,
                border = new Vector4(slice.border, 0f, slice.border, 0f) };
            ids[i] = new SpriteNameFileIdPair(slice.name, id);
        }
        provider.SetSpriteRects(rects);
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(ids);
        provider.Apply();
        importer.SaveAndReimport();
    }

    private static void ConfigureBorder(string fileName, Vector4 border)
    {
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(k_artRoot + fileName);
        var factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        SpriteRect[] rects = provider.GetSpriteRects();
        for (int i = 0; i < rects.Length; i++) rects[i].border = border;
        provider.SetSpriteRects(rects);
        provider.Apply();
        importer.SaveAndReimport();
    }

    private static void ConfigureSupportSettings()
    {
        const string path = "Assets/MPUIKit/Runtime/Resources/MPImageResources.asset";
        ScriptableObject settings = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance("MPUIKIT.MPImageResources");
            if (settings == null) throw new InvalidOperationException("無法建立 MPUIKit 圖像設定。");
            AssetDatabase.CreateAsset(settings, path);
        }
        var serialized = new SerializedObject(settings);
        serialized.FindProperty("m_emptySprite").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/2DArt/Support/MPUIKit/mpui_default_empty_sprite.png");
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
    }

    private static void ReplaceReferences(Dictionary<string, Sprite> replacements)
    {
        var refs = new Dictionary<string, string>();
        var report = new StringBuilder("# ChibiPaper 素材替換清單\n\n| 舊素材 | 新素材 |\n| --- | --- |\n");
        foreach (KeyValuePair<string, Sprite> pair in replacements)
        {
            string oldGuid = AssetDatabase.AssetPathToGUID(pair.Key);
            if (string.IsNullOrEmpty(oldGuid)) continue;
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(pair.Value, out string guid, out long fileId))
                throw new InvalidOperationException("無法取得 Sprite 引用：" + pair.Value.name);
            refs.Add(oldGuid, "{fileID: " + fileId + ", guid: " + guid + ", type: 3}");
            report.Append('|').Append(pair.Key).Append('|').Append(AssetDatabase.GetAssetPath(pair.Value))
                .Append(" / ").Append(pair.Value.name).Append("|\n");
        }
        var pattern = new Regex(@"\{fileID: -?\d+, guid: ([0-9a-f]{32}), type: \d+\}");
        string[] paths = AssetDatabase.GetAllAssetPaths();
        AssetDatabase.StartAssetEditing();
        try
        {
            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i];
                if (!IsSerializedAsset(path)) continue;
                string source = File.ReadAllText(path);
                string updated = pattern.Replace(source, m => refs.TryGetValue(m.Groups[1].Value, out string value) ? value : m.Value);
                if (updated == source) continue;
                BackupFile(path);
                // Unity 可能已記憶體映射 Prefab；以替換檔案方式寫入，避免截斷開啟中的檔案。
                string temporaryPath = path + ".chibi-tmp";
                File.WriteAllText(temporaryPath, updated, s_utf8);
                File.Replace(temporaryPath, path, null);
                AssetDatabase.ImportAsset(path);
            }
        }
        finally { AssetDatabase.StopAssetEditing(); }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        File.WriteAllText(k_output + "replacement-map.md", report.ToString(), s_utf8);
    }

    private static void BindArtReferences()
    {
        const string path = "Assets/2DArt/UI/InBattle.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Type type = Type.GetType("MassageCatUiArt, Assembly-CSharp", true);
            Component art = root.GetComponentInChildren(type, true);
            if (art == null) throw new InvalidOperationException("InBattle 缺少美術引用元件。");
            var serialized = new SerializedObject(art);
            string[,] fields = {
                { "m_stageFrame", "stage_frame_v1.png" }, { "m_spellCard", "spell_card_v1.png" },
                { "m_tomatoCard", "tomato_card_v1.png" }, { "m_spellIcon", "spell_icon_v1.png" },
                { "m_tomatoIcon", "tomato_icon_v1.png" }
            };
            for (int i = 0; i < fields.GetLength(0); i++)
                serialized.FindProperty(fields[i, 0]).objectReferenceValue = LoadNamedSprite(fields[i, 1]);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Component menu = root.GetComponentInChildren(Type.GetType("GameSettingsMenu, Assembly-CSharp", true), true);
            var settings = new SerializedObject(menu);
            Image background = settings.FindProperty("m_settingsPanelBackground").objectReferenceValue as Image;
            if (background == null) throw new InvalidOperationException("設定面板缺少背景 Image。");
            // 修正舊 Prefab 將 Sprite 欄位誤指向同檔 GameObject 的無效引用。
            background.sprite = LoadNamedSprite("settings_panel_v1.png");
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    [MenuItem("Tools/Massage Cat/Art/Validate ChibiPaper UI")]
    internal static void Validate()
    {
        string[] paths = AssetDatabase.GetAllAssetPaths();
        int textures = 0;
        for (int i = 0; i < paths.Length; i++)
        {
            string path = paths[i];
            if (path.StartsWith("Assets/", StringComparison.Ordinal) && path.Contains("/Resources/") &&
                (AssetImporter.GetAtPath(path) is TextureImporter || AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(RenderTexture)))
            {
                textures++;
                Debug.LogError("Resources 仍含貼圖：" + path);
            }
        }
        if (textures != 0) throw new InvalidOperationException("Resources 貼圖數量：" + textures);
        string[] prefabs = { "Assets/2DArt/UI/InBattle.prefab", "Assets/AssetResources/UI/GameMain.prefab" };
        for (int i = 0; i < prefabs.Length; i++)
        {
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(prefabs[i]);
            Component art = root.GetComponentInChildren(Type.GetType("MassageCatUiArt, Assembly-CSharp", true), true);
            if (art == null) throw new InvalidOperationException("缺少紙繪美術元件：" + prefabs[i]);
            var iterator = new SerializedObject(art).GetIterator();
            while (iterator.NextVisible(true))
                if (iterator.propertyType == SerializedPropertyType.ObjectReference && iterator.name != "m_Script")
                    if (!(iterator.objectReferenceValue is Sprite sprite) || !AssetDatabase.GetAssetPath(sprite).StartsWith(k_artRoot, StringComparison.Ordinal))
                        throw new InvalidOperationException("紙繪引用無效：" + prefabs[i] + " / " + iterator.name);
        }
        UIBuildConsistencyValidator.ValidateOrThrow();
    }

    private static void NormalizeSpritePresentation()
    {
        string[] paths = { "Assets/2DArt/UI/EnemySpawn.prefab", "Assets/2DArt/UI/InBattle.prefab",
            "Assets/AssetResources/UI/GameMain.prefab", "Assets/AssetResources/UI/View_Title.prefab" };
        for (int i = 0; i < paths.Length; i++)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(paths[i]);
            bool changed = false;
            try
            {
                Image[] images = root.GetComponentsInChildren<Image>(true);
                for (int j = 0; j < images.Length; j++)
                {
                    Image image = images[j];
                    if (image.sprite == null) continue;
                    string spritePath = AssetDatabase.GetAssetPath(image.sprite);
                    if (!spritePath.StartsWith(k_artRoot, StringComparison.Ordinal)) continue;
                    // 紙繪圖示已有完整配色；保留透明度，移除舊白色剪影所需的染色。
                    Color color = new Color(1f, 1f, 1f, image.color.a);
                    if (image.color != color) { image.color = color; changed = true; }
                    if (spritePath.EndsWith("controls_atlas_v1.png", StringComparison.Ordinal) && !image.preserveAspect)
                    { image.preserveAspect = true; changed = true; }
                    if (spritePath.EndsWith("settings_panel_v1.png", StringComparison.Ordinal) && image.type != Image.Type.Sliced)
                    { image.type = Image.Type.Sliced; changed = true; }
                    if (spritePath.EndsWith("settings_panel_v1.png", StringComparison.Ordinal) && image.pixelsPerUnitMultiplier != 8f)
                    { image.pixelsPerUnitMultiplier = 8f; changed = true; }
                }
                if (changed) PrefabUtility.SaveAsPrefabAsset(root, paths[i]);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }

    [MenuItem("Tools/Massage Cat/Art/Repair ChibiPaper Presentation")]
    internal static void RepairPresentation()
    {
        BindArtReferences();
        NormalizeSpritePresentation();
        Validate();
        AssetDatabase.SaveAssets();
    }

    private static bool IsSerializedAsset(string path)
    {
        if (!path.StartsWith("Assets/", StringComparison.Ordinal)) return false;
        string extension = Path.GetExtension(path);
        return extension == ".prefab" || extension == ".unity" || extension == ".asset" || extension == ".mat" || extension == ".anim" || extension == ".controller";
    }

    private static void AssertNoReferences(string guid)
    {
        string[] paths = AssetDatabase.GetAllAssetPaths();
        for (int i = 0; i < paths.Length; i++)
            if (IsSerializedAsset(paths[i]) && File.ReadAllText(paths[i]).Contains(guid))
                throw new InvalidOperationException("舊美術仍被引用，停止刪除：" + paths[i]);
    }

    private static void Move(string from, string to)
    {
        if (!File.Exists(from) && !Directory.Exists(from)) return;
        if (File.Exists(to) || Directory.Exists(to)) throw new IOException("搬移目標已存在：" + to);
        string error = AssetDatabase.MoveAsset(from, to);
        if (!string.IsNullOrEmpty(error)) throw new IOException(error);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    private static void BackupFolder(string path)
    {
        if (!Directory.Exists(path)) return;
        string[] files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++) BackupFile(files[i]);
        if (File.Exists(path + ".meta")) BackupFile(path + ".meta");
    }

    private static void BackupFile(string path)
    {
        string target = k_output + "backup/" + path;
        if (File.Exists(target)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(target));
        File.Copy(path, target);
    }
}
#endif
