#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;

public static class GalleryProjectGenerator
{
    private const string k_mainScenePath = "Assets/Scenes/Main.unity";
    private const string k_fontAssetPath = "Assets/GameCore/UI/Font/NotoSansTC-VariableFont_wght SDF.asset";
    private const string k_localizationCollectionName = "LocalizationCollection";
    private const string k_defaultBuildDirectory = "Builds/Windows";
    private const string k_localTestBuildDirectory = "Builds/WindowsLocalTest";
    private const string k_disableSteamDefine = "DISABLE_STEAM";

    private static readonly string[] s_disableSteamDefines = { k_disableSteamDefine };

    private static readonly KeyValuePair<string, string>[] s_localizedEntries =
    {
        new KeyValuePair<string, string>("Gallery.Title", "對象圖鑑"),
        new KeyValuePair<string, string>("Gallery.All", "全部"),
        new KeyValuePair<string, string>("Gallery.Sort", "排序"),
        new KeyValuePair<string, string>("Gallery.SortOrder", "固定順序"),
        new KeyValuePair<string, string>("Gallery.SortMissing", "缺卡優先"),
        new KeyValuePair<string, string>("Gallery.SortTomato", "番茄未完成優先"),
        new KeyValuePair<string, string>("Gallery.FilterMissing", "只看缺卡"),
        new KeyValuePair<string, string>("Gallery.FilterTomato", "只看番茄未完成"),
        new KeyValuePair<string, string>("Gallery.Empty", "沒有符合條件的已解鎖對象"),
        new KeyValuePair<string, string>("Gallery.Confirm", "確認切換"),
        new KeyValuePair<string, string>("Gallery.CardObtained", "卡片：已取得"),
        new KeyValuePair<string, string>("Gallery.CardMissing", "卡片：未取得"),
        new KeyValuePair<string, string>("Gallery.Online", "● 桌寵已連線"),
        new KeyValuePair<string, string>("Gallery.Offline", "○ 離線瀏覽"),
        new KeyValuePair<string, string>("Gallery.Connecting", "正在連接桌寵…"),
        new KeyValuePair<string, string>("Gallery.Switching", "正在切換對象…"),
        new KeyValuePair<string, string>("Gallery.SwitchSuccess", "已切換對象"),
        new KeyValuePair<string, string>("Gallery.ConnectTimeout", "連線逾時，請確認桌寵程式可正常啟動。"),
        new KeyValuePair<string, string>("Gallery.NormalModeOnly", "只能在一般模式切換對象。"),
        new KeyValuePair<string, string>("Gallery.RoleLocked", "此對象目前尚未解鎖。"),
        new KeyValuePair<string, string>("Gallery.RoleNotFound", "找不到此對象資料。"),
        new KeyValuePair<string, string>("Gallery.NotReady", "桌寵尚未完成初始化，請稍後再試。"),
        new KeyValuePair<string, string>("Gallery.LaunchFailedTitle", "無法開啟圖鑑"),
        new KeyValuePair<string, string>("Gallery.LaunchFailedContent", "找不到圖鑑程式或啟動失敗，請確認 MassageCatGallery.exe 與主程式位於相同資料夾。"),
    };

    [MenuItem("Massage Cat/Gallery/Generate Companion Assets")]
    internal static void GenerateCompanionAssets()
    {
        AddLocalizationEntries();
        RegisterBuildScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("整合式圖鑑的本地化資料與建置場景已更新。");
    }

    [MenuItem("Massage Cat/Gallery/Build Windows Players")]
    internal static void BuildWindowsPlayers()
    {
        GenerateCompanionAssets();
        BuildWindowsPlayersInternal(k_defaultBuildDirectory, null);
    }

    [MenuItem("Massage Cat/Gallery/Build Windows Local Test Players (No Steam)")]
    internal static void BuildWindowsLocalTestPlayers()
    {
        GenerateCompanionAssets();
        BuildWindowsPlayersInternal(k_localTestBuildDirectory, s_disableSteamDefines);
    }

    public static void BatchGenerateCompanionAssets()
    {
        GenerateCompanionAssets();
    }

    public static void BatchBuildWindowsPlayers()
    {
        GenerateCompanionAssets();
        BuildWindowsPlayersInternal(k_defaultBuildDirectory, null);
    }

    public static void BatchBuildWindowsLocalTestPlayers()
    {
        GenerateCompanionAssets();
        BuildWindowsPlayersInternal(k_localTestBuildDirectory, s_disableSteamDefines);
    }

    [MenuItem("Massage Cat/Gallery/Validate Companion Assets")]
    public static void BatchValidateCompanionAssets()
    {
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(k_fontAssetPath) == null)
            throw new FileNotFoundException("找不到圖鑑使用的中文字型。", k_fontAssetPath);
        if (LocalizationEditorSettings.GetStringTableCollection(k_localizationCollectionName) == null)
            throw new InvalidOperationException("找不到圖鑑使用的本地化字串表。");

        bool mainSceneRegistered = false;
        EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
        for (int i = 0; i < buildScenes.Length; i++)
        {
            EditorBuildSettingsScene buildScene = buildScenes[i];
            if (buildScene.enabled == false)
                continue;

            mainSceneRegistered |= string.Equals(buildScene.path, k_mainScenePath, StringComparison.Ordinal);
        }

        if (mainSceneRegistered == false)
            throw new InvalidOperationException("Main 場景尚未加入 Build Settings。");

        Debug.Log("整合式圖鑑資產驗證完成。");
    }

    private static void AddLocalizationEntries()
    {
        StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(k_localizationCollectionName);
        if (collection == null)
        {
            Debug.LogWarning($"找不到 Localization Collection：{k_localizationCollectionName}");
            return;
        }

        foreach (StringTable table in collection.StringTables)
        {
            if (table == null)
                continue;

            for (int i = 0; i < s_localizedEntries.Length; i++)
            {
                KeyValuePair<string, string> localizedEntry = s_localizedEntries[i];
                StringTableEntry entry = table.GetEntry(localizedEntry.Key)
                                         ?? table.AddEntry(localizedEntry.Key, localizedEntry.Value);
                entry.Value = localizedEntry.Value;
            }

            EditorUtility.SetDirty(table);
            EditorUtility.SetDirty(table.SharedData);
        }
    }

    private static void RegisterBuildScenes()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(k_mainScenePath, true),
        };
    }

    private static void BuildWindowsPlayersInternal(string buildDirectory, string[] extraScriptingDefines)
    {
        Directory.CreateDirectory(buildDirectory);
        AddressableAssetSettings.BuildPlayerContent();

        string originalProductName = PlayerSettings.productName;
        bool originalForceSingleInstance = PlayerSettings.forceSingleInstance;
        bool originalResizableWindow = PlayerSettings.resizableWindow;
        FullScreenMode originalFullScreenMode = PlayerSettings.fullScreenMode;
        int originalDefaultScreenWidth = PlayerSettings.defaultScreenWidth;
        int originalDefaultScreenHeight = PlayerSettings.defaultScreenHeight;
        bool originalRunInBackground = PlayerSettings.runInBackground;

        try
        {
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true;
            PlayerSettings.forceSingleInstance = true;

            PlayerSettings.productName = "WalkBrave";
            PlayerSettings.resizableWindow = false;
            PlayerSettings.defaultScreenWidth = 640;
            PlayerSettings.defaultScreenHeight = 480;
            BuildPlayer(k_mainScenePath, Path.Combine(buildDirectory, "MassageCat.exe"), extraScriptingDefines);

        }
        finally
        {
            PlayerSettings.productName = originalProductName;
            PlayerSettings.forceSingleInstance = originalForceSingleInstance;
            PlayerSettings.resizableWindow = originalResizableWindow;
            PlayerSettings.fullScreenMode = originalFullScreenMode;
            PlayerSettings.defaultScreenWidth = originalDefaultScreenWidth;
            PlayerSettings.defaultScreenHeight = originalDefaultScreenHeight;
            PlayerSettings.runInBackground = originalRunInBackground;
            AssetDatabase.SaveAssets();
        }
    }

    private static void BuildPlayer(string scenePath, string executablePath, string[] extraScriptingDefines)
    {
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { scenePath },
            locationPathName = executablePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
            extraScriptingDefines = extraScriptingDefines,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException($"Windows Player 建置失敗：{executablePath}，result={report.summary.result}");
    }
}
#endif
