#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.UI;

internal sealed class UIBuildConsistencyValidator : IPreprocessBuildWithReport
{
    private const string k_inBattlePrefabPath = "Assets/2DArt/UI/InBattle.prefab";
    private const string k_gameMainPrefabPath = "Assets/AssetResources/UI/GameMain.prefab";
    private const int k_assetDatabasePlayModeBuilderIndex = 0;

    private static readonly string[] s_battlePanelRequiredReferences =
    {
        "m_enemyParent",
        "m_bgImage",
        "m_foregroundRoot",
        "m_sceneVisualToggleButton",
        "m_gameSettingsMenu",
        "m_enemySpawn",
        "m_ani",
        "m_tomatoBtn",
        "m_galleryButton",
        "m_healFeedbackPanel",
        "m_teachPanel",
        "m_autoNextButton",
        "m_spellButton",
        "m_spellHandler",
        "m_systemMessageView",
        "m_normalHealEffectTemplate",
        "m_strongHealEffectTemplate",
        "m_bgmStateButton",
        "m_audioStateButton"
    };

    private static readonly string[] s_gameMainViewRequiredReferences =
    {
        "m_battlePanel",
        "m_roleScrollRect",
        "m_canvasScaler"
    };

    private static readonly string[] s_settingsIconReferences =
    {
        "m_settingsIcon", "m_bgmStateIcon", "m_sfxStateIcon", "m_quitIcon", "m_saveIcon"
    };

    public int callbackOrder => -10000;

    [InitializeOnLoadMethod]
    private static void ConfigureEditorPlayMode()
    {
        EditorApplication.delayCall -= ApplyAssetDatabasePlayMode;
        EditorApplication.delayCall += ApplyAssetDatabasePlayMode;
    }

    [MenuItem("GameCore/Validation/Validate UI Build Consistency")]
    private static void ValidateFromMenu()
    {
        ValidateOrThrow();
        Debug.Log("UI Prefab 與 Build 根 Prefab 的功能引用一致性驗證通過。");
    }

    [MenuItem("GameCore/Validation/Repair GameMain Functional Overrides")]
    private static void RepairGameMainFunctionalOverrides()
    {
        GameObject prefabContents = PrefabUtility.LoadPrefabContents(k_gameMainPrefabPath);
        int repairedCount = 0;

        try
        {
            BattlePanel buildBattlePanel =
                prefabContents.GetComponentInChildren<BattlePanel>(true);
            if (buildBattlePanel == null)
                throw new BuildFailedException($"{k_gameMainPrefabPath} 缺少 BattlePanel。");

            GameObject instanceRoot =
                PrefabUtility.GetNearestPrefabInstanceRoot(buildBattlePanel.gameObject);
            var removedGameObjects = PrefabUtility.GetRemovedGameObjects(instanceRoot);
            for (int i = removedGameObjects.Count - 1; i >= 0; i--)
            {
                if (AssetDatabase.GetAssetPath(removedGameObjects[i].assetGameObject)
                    != k_inBattlePrefabPath)
                {
                    continue;
                }

                removedGameObjects[i].Revert(InteractionMode.AutomatedAction);
                repairedCount++;
            }

            var removedComponents = PrefabUtility.GetRemovedComponents(instanceRoot);
            for (int i = removedComponents.Count - 1; i >= 0; i--)
            {
                if (AssetDatabase.GetAssetPath(removedComponents[i].assetComponent)
                    != k_inBattlePrefabPath)
                {
                    continue;
                }

                removedComponents[i].Revert(InteractionMode.AutomatedAction);
                repairedCount++;
            }

            var addedComponents = PrefabUtility.GetAddedComponents(instanceRoot);
            for (int i = addedComponents.Count - 1; i >= 0; i--)
            {
                Component instanceComponent = addedComponents[i].instanceComponent;
                if (instanceComponent == null)
                    continue;

                GameObject sourceGameObject =
                    PrefabUtility.GetCorrespondingObjectFromSource(instanceComponent.gameObject);
                if (AssetDatabase.GetAssetPath(sourceGameObject) != k_inBattlePrefabPath)
                    continue;

                Component canonicalComponent =
                    sourceGameObject.GetComponent(instanceComponent.GetType());
                if (canonicalComponent != null)
                {
                    RevertReferencesToAddedComponent(
                        instanceRoot,
                        instanceComponent);
                    addedComponents[i].Revert(InteractionMode.AutomatedAction);
                }
                else
                {
                    // GameMain 只負責組合與版面；功能元件必須回寫到標準 InBattle Prefab。
                    PrefabUtility.ApplyAddedComponent(
                        instanceComponent,
                        k_inBattlePrefabPath,
                        InteractionMode.AutomatedAction);
                }

                repairedCount++;
            }

            PrefabUtility.SaveAsPrefabAsset(prefabContents, k_gameMainPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabContents);
        }

        Debug.Log($"GameMain 功能性 Prefab Override 修復完成：{repairedCount} 項。");
    }

    public void OnPreprocessBuild(BuildReport report)
    {
        ValidateOrThrow();
    }

    internal static void ValidateOrThrow()
    {
        GameObject inBattlePrefab = LoadPrefabOrThrow(k_inBattlePrefabPath);
        GameObject gameMainPrefab = LoadPrefabOrThrow(k_gameMainPrefabPath);

        BattlePanel canonicalBattlePanel =
            inBattlePrefab.GetComponentInChildren<BattlePanel>(true);
        BattlePanel buildBattlePanel =
            gameMainPrefab.GetComponentInChildren<BattlePanel>(true);
        GameMainView gameMainView = gameMainPrefab.GetComponent<GameMainView>();

        if (canonicalBattlePanel == null)
            throw new BuildFailedException($"{k_inBattlePrefabPath} 缺少 BattlePanel。");
        if (buildBattlePanel == null)
            throw new BuildFailedException($"{k_gameMainPrefabPath} 缺少 BattlePanel。");
        if (gameMainView == null)
            throw new BuildFailedException($"{k_gameMainPrefabPath} 缺少 GameMainView。");

        GameObject correspondingSource =
            PrefabUtility.GetCorrespondingObjectFromSource(buildBattlePanel.gameObject);
        if (correspondingSource != canonicalBattlePanel.gameObject)
        {
            throw new BuildFailedException(
                $"{k_gameMainPrefabPath} 的 BattlePanel 並非來自 {k_inBattlePrefabPath}。");
        }

        ValidateRequiredReferences(
            canonicalBattlePanel,
            buildBattlePanel,
            s_battlePanelRequiredReferences);
        ValidateRequiredReferences(gameMainView, gameMainView, s_gameMainViewRequiredReferences);
        ValidateNoMissingScripts(inBattlePrefab, k_inBattlePrefabPath);
        ValidateNoMissingScripts(gameMainPrefab, k_gameMainPrefabPath);
        ValidateNoFunctionalStructureOverrides(buildBattlePanel.gameObject);
        ValidateVisualControls(canonicalBattlePanel);
        ValidateVisualControls(buildBattlePanel);
    }

    private static void ValidateVisualControls(BattlePanel battlePanel)
    {
        SerializedObject battle = new SerializedObject(battlePanel);
        Component menu = battle.FindProperty("m_gameSettingsMenu").objectReferenceValue as Component;
        Component sceneToggle = battle.FindProperty("m_sceneVisualToggleButton").objectReferenceValue as Component;
        SerializedObject settings = new SerializedObject(menu);
        for (int i = 0; i < s_settingsIconReferences.Length; i++)
            ValidateIcon(settings, s_settingsIconReferences[i]);

        ValidateIcon(settings, "m_settingsPanelBackground");
        SerializedObject scene = new SerializedObject(sceneToggle);
        ValidateIcon(scene, "m_visibleIconImage");
        ValidateIcon(scene, "m_hiddenIconImage");

        GameObject panel = settings.FindProperty("m_settingsPanel").objectReferenceValue as GameObject;
        Component bgm = settings.FindProperty("m_bgmToggleButton").objectReferenceValue as Component;
        Component sfx = settings.FindProperty("m_sfxToggleButton").objectReferenceValue as Component;
        Component dismiss = settings.FindProperty("m_dismissButton").objectReferenceValue as Component;
        if (panel == null || bgm == null || sfx == null || dismiss == null
            || !bgm.transform.IsChildOf(panel.transform) || !sfx.transform.IsChildOf(panel.transform))
        {
            throw new BuildFailedException("設定面板的音樂／音效控制必須位於面板內，避免主畫面重複顯示。");
        }

        RectTransform dismissRect = dismiss.transform as RectTransform;
        if (dismissRect == null || dismissRect.anchorMin != Vector2.zero
            || dismissRect.anchorMax != Vector2.one || dismissRect.offsetMin != Vector2.zero
            || dismissRect.offsetMax != Vector2.zero)
        {
            throw new BuildFailedException("設定面板關閉點擊區必須完整延展至父容器。");
        }

        Canvas menuCanvas = menu.GetComponent<Canvas>();
        Component message = battle.FindProperty("m_systemMessageView").objectReferenceValue as Component;
        Canvas messageCanvas = message != null ? message.GetComponent<Canvas>() : null;
        if (menuCanvas == null || messageCanvas == null || !menuCanvas.overrideSorting
            || !messageCanvas.overrideSorting || menuCanvas.sortingLayerID != messageCanvas.sortingLayerID
            || menuCanvas.sortingOrder >= messageCanvas.sortingOrder)
        {
            throw new BuildFailedException("系統訊息必須顯示於設定選單上方，請檢查 Canvas 排序。");
        }
    }

    private static void ValidateIcon(SerializedObject owner, string propertyName)
    {
        SerializedProperty property = owner.FindProperty(propertyName);
        Image icon = property != null ? property.objectReferenceValue as Image : null;
        // 只驗證功能圖示；透明點擊區與純色遮罩不需要 Sprite。
        if (icon == null || icon.sprite == null || !icon.enabled || icon.color.a <= 0f)
            throw new BuildFailedException($"UI 圖示缺少有效圖片或不可見：{owner.targetObject.name}.{propertyName}");
    }

    private static void ApplyAssetDatabasePlayMode()
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null
            || settings.DataBuilders.Count <= k_assetDatabasePlayModeBuilderIndex)
        {
            return;
        }

        if (settings.ActivePlayModeDataBuilderIndex != k_assetDatabasePlayModeBuilderIndex)
        {
            settings.ActivePlayModeDataBuilderIndex = k_assetDatabasePlayModeBuilderIndex;
            Debug.Log("Addressables Editor Play Mode 已切換為 Use Asset Database。");
        }
    }

    private static GameObject LoadPrefabOrThrow(string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
            throw new BuildFailedException($"找不到 UI Prefab：{path}");

        return prefab;
    }

    private static void ValidateRequiredReferences(
        Component canonicalComponent,
        Component buildComponent,
        IReadOnlyList<string> propertyNames)
    {
        SerializedObject canonicalObject = new SerializedObject(canonicalComponent);
        SerializedObject buildObject = new SerializedObject(buildComponent);

        for (int i = 0; i < propertyNames.Count; i++)
        {
            string propertyName = propertyNames[i];
            SerializedProperty canonicalProperty = canonicalObject.FindProperty(propertyName);
            SerializedProperty buildProperty = buildObject.FindProperty(propertyName);

            if (canonicalProperty == null || buildProperty == null)
            {
                throw new BuildFailedException(
                    $"UI 一致性驗證找不到序列化欄位：{propertyName}");
            }

            UnityEngine.Object canonicalReference = canonicalProperty.objectReferenceValue;
            UnityEngine.Object buildReference = buildProperty.objectReferenceValue;
            // 獨立音訊按鈕已由設定選單接手；兩份 Prefab 均未使用時允許留空。
            // 設定選單本體與其功能圖示仍由下方的設定 UI 驗證檢查。
            if (canonicalReference == null && buildReference == null
                && canonicalComponent is BattlePanel
                && (propertyName == "m_bgmStateButton" || propertyName == "m_audioStateButton")
                && canonicalObject.FindProperty("m_gameSettingsMenu").objectReferenceValue != null
                && buildObject.FindProperty("m_gameSettingsMenu").objectReferenceValue != null)
            {
                continue;
            }
            if (canonicalReference == null || buildReference == null)
            {
                throw new BuildFailedException(
                    $"UI 功能引用未綁定：{buildComponent.GetType().Name}.{propertyName}");
            }

            if (canonicalReference.GetType() != buildReference.GetType())
            {
                throw new BuildFailedException(
                    $"UI 功能引用型別不一致：{buildComponent.GetType().Name}.{propertyName}");
            }
        }
    }

    private static void ValidateNoMissingScripts(GameObject prefab, string prefabPath)
    {
        Component[] components = prefab.GetComponentsInChildren<Component>(true);
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] == null)
                throw new BuildFailedException($"UI Prefab 含有 Missing Script：{prefabPath}");
        }
    }

    private static void RevertReferencesToAddedComponent(
        GameObject instanceRoot,
        Component addedComponent)
    {
        Component[] components = instanceRoot.GetComponentsInChildren<Component>(true);
        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];
            if (component == null || component == addedComponent)
                continue;

            SerializedObject serializedObject = new SerializedObject(component);
            SerializedProperty property = serializedObject.GetIterator();
            bool enterChildren = true;
            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (property.propertyType != SerializedPropertyType.ObjectReference
                    || property.objectReferenceValue != addedComponent)
                {
                    continue;
                }

                PrefabUtility.RevertPropertyOverride(
                    property,
                    InteractionMode.AutomatedAction);
            }
        }
    }

    private static void ValidateNoFunctionalStructureOverrides(GameObject instanceObject)
    {
        GameObject instanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(instanceObject);
        if (instanceRoot == null)
            throw new BuildFailedException("GameMain 中找不到 InBattle Prefab instance root。");

        var removedGameObjects = PrefabUtility.GetRemovedGameObjects(instanceRoot);
        var removedComponents = PrefabUtility.GetRemovedComponents(instanceRoot);
        var addedComponents = PrefabUtility.GetAddedComponents(instanceRoot);
        if (removedGameObjects.Count > 0
            || removedComponents.Count > 0
            || addedComponents.Count > 0)
        {
            StringBuilder details = new StringBuilder(256);
            for (int i = 0; i < removedGameObjects.Count; i++)
            {
                GameObject assetGameObject = removedGameObjects[i].assetGameObject;
                details.Append("\nGameObject: ")
                    .Append(assetGameObject != null ? assetGameObject.name : "<null>")
                    .Append(" [")
                    .Append(AssetDatabase.GetAssetPath(assetGameObject))
                    .Append(']');
            }

            for (int i = 0; i < removedComponents.Count; i++)
            {
                Component assetComponent = removedComponents[i].assetComponent;
                details.Append("\nComponent: ")
                    .Append(assetComponent != null ? assetComponent.GetType().Name : "<null>")
                    .Append(" on ")
                    .Append(assetComponent != null ? assetComponent.gameObject.name : "<null>")
                    .Append(" [")
                    .Append(AssetDatabase.GetAssetPath(assetComponent))
                    .Append("] id=")
                    .Append(assetComponent != null
                        ? GlobalObjectId.GetGlobalObjectIdSlow(assetComponent).ToString()
                        : "<null>");
            }

            for (int i = 0; i < addedComponents.Count; i++)
            {
                Component instanceComponent = addedComponents[i].instanceComponent;
                details.Append("\nAdded Component: ")
                    .Append(instanceComponent != null
                        ? instanceComponent.GetType().Name
                        : "<null>")
                    .Append(" on ")
                    .Append(instanceComponent != null
                        ? instanceComponent.gameObject.name
                        : "<null>");
            }

            throw new BuildFailedException(
                "GameMain 不得移除 InBattle 的 GameObject / Component，"
                + "也不得額外增加功能元件；請回到標準 Prefab 調整。"
                + details);
        }
    }
}
#endif
