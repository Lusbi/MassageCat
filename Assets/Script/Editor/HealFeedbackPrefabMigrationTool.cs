#if UNITY_EDITOR
using System;
using BigMath;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BigInteger = System.Numerics.BigInteger;

public static class HealFeedbackPrefabMigrationTool
{
    private const int k_healProgressSortingOrder = 10;
    private const string k_enemySpawnPrefabPath = "Assets/2DArt/UI/EnemySpawn.prefab";
    private const string k_inBattlePrefabPath = "Assets/2DArt/UI/InBattle.prefab";
    private const string k_healValueTextPrefabPath = "Assets/2DArt/Pool_HealValueText.prefab";
    private const string k_normalHealEffectPrefabPath = "Assets/AssetResources/UI/NormalHealEffect.prefab";
    private const string k_strongHealEffectPrefabPath = "Assets/AssetResources/UI/StrongHealEffect.prefab";
    private const string k_normalHealAnimationPath = "Assets/2DArt/Animation/HealValue_Normal.anim";
    private const string k_strongHealAnimationPath = "Assets/2DArt/Animation/HealValue_Strong.anim";
    private const string k_strongHealEffectAnimationPath = "Assets/2DArt/Animation/StrongHealEffect.anim";
    private const string k_healProgressTrackPath = "Assets/2DArt/DesktopPet/ChibiPaper/support_atlas_v1.png";
    private const string k_healProgressFillPath = "Assets/2DArt/DesktopPet/ChibiPaper/support_atlas_v1.png";

    [MenuItem("Tools/Massage Cat/Migrate Heal Feedback UX")]
    public static void Migrate()
    {
        // 圖集已於紙繪素材遷移時設定切片與九宮格邊界。
        Sprite progressTrackSprite = ChibiPaperArtMigration.LoadNamedSprite(k_healProgressTrackPath, "HealProgressTrack");
        Sprite progressFillSprite = ChibiPaperArtMigration.LoadNamedSprite(k_healProgressFillPath, "HealProgressFill");
        MigrateEnemySpawnPrefab(progressTrackSprite, progressFillSprite);
        MigrateInBattlePrefab(progressTrackSprite, progressFillSprite);
        RenamePrefabRoot(k_healValueTextPrefabPath, "Pool_HealValueText");
        RenamePrefabRoot(k_normalHealEffectPrefabPath, "NormalHealEffect");
        RenamePrefabRoot(k_strongHealEffectPrefabPath, "StrongHealEffect");
        UpdateEditorClassIdentifier<HealValueHUD>(k_healValueTextPrefabPath);
        UpdateEditorClassIdentifier<NormalHealEffect>(k_normalHealEffectPrefabPath);
        UpdateEditorClassIdentifier<StrongHealEffect>(k_strongHealEffectPrefabPath);
        RenameAnimationClip(k_normalHealAnimationPath, "HealValue_Normal");
        RenameAnimationClip(k_strongHealAnimationPath, "HealValue_Strong");
        RenameAnimationClip(k_strongHealEffectAnimationPath, "StrongHealEffect");
        RestoreAddressableKey(k_normalHealEffectPrefabPath, "NormalHit");
        RestoreAddressableKey(k_strongHealEffectPrefabPath, "CriticleHit");
        AssetDatabase.SaveAssets();
        ValidateInternal();
        Debug.Log("療癒完成度動畫 UX Prefab 遷移與驗證完成。");
    }

    [MenuItem("Tools/Massage Cat/Validate Heal Feedback UX")]
    public static void Validate()
    {
        ValidateInternal();
        Debug.Log("療癒完成度動畫 UX Prefab 驗證通過。");
    }

    private static void MigrateEnemySpawnPrefab(Sprite progressTrackSprite, Sprite progressFillSprite)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(k_enemySpawnPrefabPath);
        try
        {
            RoleSpawnHUD roleSpawnHud = prefabRoot.GetComponent<RoleSpawnHUD>();
            if (roleSpawnHud == null)
                throw new InvalidOperationException("EnemySpawn.prefab 缺少 RoleSpawnHUD。");

            RectTransform progressRoot = RequireTransform(prefabRoot, "HealProgressRoot", "HpBar_Bg") as RectTransform;
            progressRoot.name = "HealProgressRoot";
            progressRoot.sizeDelta = new Vector2(400f, 64f);
            CreateOrUpdateProgressCanvas(progressRoot);

            Image progressTrack = progressRoot.GetComponent<Image>();
            if (progressTrack == null)
                progressTrack = progressRoot.gameObject.AddComponent<Image>();
            progressTrack.sprite = progressTrackSprite;
            progressTrack.color = Color.white;
            progressTrack.type = Image.Type.Sliced;
            progressTrack.fillCenter = true;
            progressTrack.preserveAspect = false;
            progressTrack.raycastTarget = false;

            RectTransform progressFill = RequireTransform(prefabRoot, "HealProgressFill", "HpBar_Bg (1)") as RectTransform;
            progressFill.name = "HealProgressFill";
            ConfigureProgressFill(progressFill, progressFillSprite);

            RectTransform progressText = RequireTransform(prefabRoot, "HealProgressText", "HintText") as RectTransform;
            progressText.name = "HealProgressText";
            ConfigureProgressText(progressText);

            Transform feedbackAnchor = RequireTransform(prefabRoot, "HealFeedbackAnchor", "HitPanel");
            feedbackAnchor.name = "HealFeedbackAnchor";

            Image pendingPulse = CreateOrUpdatePendingPulse(progressRoot, progressFillSprite);
            progressFill.SetSiblingIndex(0);
            pendingPulse.rectTransform.SetSiblingIndex(1);
            progressText.SetAsLastSibling();
            SerializedObject serializedHud = new SerializedObject(roleSpawnHud);
            serializedHud.FindProperty("m_healProgressRoot").objectReferenceValue = progressRoot;
            serializedHud.FindProperty("m_pendingHealPulse").objectReferenceValue = pendingPulse;
            serializedHud.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(prefabRoot, k_enemySpawnPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void ConfigureProgressFill(RectTransform progressFill, Sprite progressFillSprite)
    {
        progressFill.anchorMin = Vector2.zero;
        progressFill.anchorMax = Vector2.one;
        progressFill.pivot = new Vector2(0.5f, 0.5f);
        progressFill.offsetMin = new Vector2(80f, 14f);
        progressFill.offsetMax = new Vector2(-80f, -14f);
        progressFill.localScale = Vector3.one;

        Image fillImage = progressFill.GetComponent<Image>();
        if (fillImage == null)
            fillImage = progressFill.gameObject.AddComponent<Image>();
        fillImage.sprite = progressFillSprite;
        fillImage.color = Color.white;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = 0;
        fillImage.fillClockwise = true;
        fillImage.fillAmount = Mathf.Clamp01(fillImage.fillAmount);
        fillImage.preserveAspect = false;
        fillImage.raycastTarget = false;
    }

    private static void ConfigureProgressText(RectTransform progressText)
    {
        progressText.anchorMin = Vector2.zero;
        progressText.anchorMax = Vector2.one;
        progressText.pivot = new Vector2(0.5f, 0.5f);
        progressText.offsetMin = new Vector2(86f, 10f);
        progressText.offsetMax = new Vector2(-86f, -10f);
        progressText.localScale = Vector3.one;

        TextMeshProUGUI text = progressText.GetComponent<TextMeshProUGUI>();
        if (text == null)
            throw new InvalidOperationException("HealProgressText 缺少 TextMeshProUGUI。");
        text.color = new Color32(82, 65, 104, 255);
        text.fontSize = 18f;
        text.enableAutoSizing = false;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
    }

    private static Image CreateOrUpdatePendingPulse(
        RectTransform progressRoot,
        Sprite progressFillSprite)
    {
        Transform existingTransform = FindTransform(progressRoot.gameObject, "PendingHealPulse");
        GameObject pulseObject;
        if (existingTransform)
        {
            pulseObject = existingTransform.gameObject;
        }
        else
        {
            pulseObject = new GameObject(
                "PendingHealPulse",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
        }

        RectTransform pulseRect = pulseObject.GetComponent<RectTransform>();
        pulseRect.SetParent(progressRoot, false);
        pulseRect.anchorMin = Vector2.zero;
        pulseRect.anchorMax = Vector2.one;
        pulseRect.pivot = new Vector2(0.5f, 0.5f);
        pulseRect.offsetMin = new Vector2(80f, 14f);
        pulseRect.offsetMax = new Vector2(-80f, -14f);
        pulseRect.localScale = Vector3.one;

        Image pulseImage = pulseObject.GetComponent<Image>();
        pulseImage.raycastTarget = false;
        pulseImage.maskable = true;
        pulseImage.sprite = progressFillSprite;
        pulseImage.material = null;
        pulseImage.type = Image.Type.Simple;
        pulseImage.preserveAspect = false;
        pulseImage.pixelsPerUnitMultiplier = 1f;
        pulseImage.color = new Color(1f, 1f, 1f, 0f);
        return pulseImage;
    }

    private static void CreateOrUpdateProgressCanvas(RectTransform progressRoot)
    {
        Canvas progressCanvas = progressRoot.GetComponent<Canvas>();
        if (progressCanvas == null)
            progressCanvas = progressRoot.gameObject.AddComponent<Canvas>();

        progressCanvas.overrideSorting = true;
        progressCanvas.sortingLayerID = 0;
        progressCanvas.sortingOrder = k_healProgressSortingOrder;

        // 獨立開啟 EnemySpawn Prefab 時沒有上層 Canvas，需直接保存巢狀 Canvas 的排序旗標。
        SerializedObject serializedCanvas = new SerializedObject(progressCanvas);
        serializedCanvas.FindProperty("m_OverrideSorting").boolValue = true;
        serializedCanvas.FindProperty("m_SortingLayerID").intValue = 0;
        serializedCanvas.FindProperty("m_SortingOrder").intValue = k_healProgressSortingOrder;
        serializedCanvas.ApplyModifiedPropertiesWithoutUndo();

        GraphicRaycaster graphicRaycaster = progressRoot.GetComponent<GraphicRaycaster>();
        if (graphicRaycaster)
            UnityEngine.Object.DestroyImmediate(graphicRaycaster);
    }

    private static void MigrateInBattlePrefab(Sprite progressTrackSprite, Sprite progressFillSprite)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(k_inBattlePrefabPath);
        try
        {
            Transform fadeBar = FindTransform(prefabRoot, "FadeBar");
            if (fadeBar)
                UnityEngine.Object.DestroyImmediate(fadeBar.gameObject);

            RoleSpawnHUD roleSpawnHud = prefabRoot.GetComponentInChildren<RoleSpawnHUD>(true);
            if (roleSpawnHud == null)
                throw new InvalidOperationException("InBattle.prefab 缺少 RoleSpawnHUD。");

            // 舊 Prefab Variant 曾把底框 Image 的 RGB 覆寫為黑色；需在最終 InBattle 直接覆蓋為新素材設定。
            RectTransform progressRoot = RequireTransform(prefabRoot, "HealProgressRoot", "HpBar_Bg") as RectTransform;
            Image progressTrack = progressRoot.GetComponent<Image>();
            progressTrack.sprite = progressTrackSprite;
            progressTrack.color = Color.white;
            progressTrack.type = Image.Type.Sliced;
            progressTrack.raycastTarget = false;

            RectTransform progressFill = RequireTransform(prefabRoot, "HealProgressFill", "HpBar_Bg (1)") as RectTransform;
            Image fillImage = progressFill.GetComponent<Image>();
            fillImage.sprite = progressFillSprite;
            fillImage.color = Color.white;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0;
            fillImage.fillClockwise = true;

            Transform pendingPulseTransform = FindTransform(prefabRoot, "PendingHealPulse");
            Image pendingPulse = pendingPulseTransform ? pendingPulseTransform.GetComponent<Image>() : null;
            if (pendingPulse == null)
                throw new InvalidOperationException("InBattle.prefab 缺少 PendingHealPulse。");
            pendingPulse.sprite = progressFillSprite;
            pendingPulse.color = new Color(1f, 1f, 1f, 0f);

            Animation obsoleteAnimation = roleSpawnHud.GetComponent<Animation>();
            if (obsoleteAnimation)
                UnityEngine.Object.DestroyImmediate(obsoleteAnimation);

            HealFeedbackPanel feedbackPanel = prefabRoot.GetComponent<HealFeedbackPanel>();
            if (feedbackPanel)
                SetEditorClassIdentifier(feedbackPanel);

            SavePrefab(prefabRoot, k_inBattlePrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void RenamePrefabRoot(string prefabPath, string newName)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            prefabRoot.name = newName;
            SavePrefab(prefabRoot, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void RenameAnimationClip(string assetPath, string newName)
    {
        AnimationClip animationClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        if (animationClip == null)
            throw new InvalidOperationException($"找不到動畫資產：{assetPath}");

        animationClip.name = newName;
        EditorUtility.SetDirty(animationClip);
    }

    private static void RestoreAddressableKey(string assetPath, string address)
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
        AddressableAssetEntry entry = settings ? settings.FindAssetEntry(assetGuid) : null;
        if (entry == null)
            throw new InvalidOperationException($"找不到 Addressables 項目：{assetPath}");

        entry.SetAddress(address, false);
        EditorUtility.SetDirty(entry.parentGroup);
        EditorUtility.SetDirty(settings);
    }

    private static void UpdateEditorClassIdentifier<T>(string prefabPath) where T : Component
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            T component = prefabRoot.GetComponent<T>();
            if (component == null)
                throw new InvalidOperationException($"Prefab 缺少 {typeof(T).Name}：{prefabPath}");

            SetEditorClassIdentifier(component);
            SavePrefab(prefabRoot, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void SetEditorClassIdentifier(Component component)
    {
        SerializedObject serializedComponent = new SerializedObject(component);
        SerializedProperty identifierProperty = serializedComponent.FindProperty("m_EditorClassIdentifier");
        if (identifierProperty == null)
            return;

        identifierProperty.stringValue = $"Assembly-CSharp::{component.GetType().Name}";
        serializedComponent.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SavePrefab(GameObject prefabRoot, string prefabPath)
    {
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath, out bool success);
        if (success == false)
            throw new InvalidOperationException($"Prefab 儲存失敗：{prefabPath}");
    }

    private static Transform RequireTransform(GameObject prefabRoot, string newName, string oldName)
    {
        Transform target = FindTransform(prefabRoot, newName);
        if (target == null)
            target = FindTransform(prefabRoot, oldName);
        if (target == null)
            throw new InvalidOperationException($"{prefabRoot.name} 找不到 {newName}／{oldName}。");
        return target;
    }

    private static Transform FindTransform(GameObject root, string objectName)
    {
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform current = transforms[i];
            if (string.Equals(current.name, objectName, StringComparison.Ordinal))
                return current;
        }

        return null;
    }

    private static void ValidateInternal()
    {
        GameObject enemySpawnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(k_enemySpawnPrefabPath);
        RoleSpawnHUD roleSpawnHud = enemySpawnPrefab ? enemySpawnPrefab.GetComponent<RoleSpawnHUD>() : null;
        if (roleSpawnHud == null)
            throw new InvalidOperationException("EnemySpawn.prefab 的 RoleSpawnHUD 無效。");

        SerializedObject serializedHud = new SerializedObject(roleSpawnHud);
        if (serializedHud.FindProperty("m_healProgressRoot").objectReferenceValue == null
            || serializedHud.FindProperty("m_pendingHealPulse").objectReferenceValue == null
            || serializedHud.FindProperty("m_enemyHealProgressText").objectReferenceValue == null
            || serializedHud.FindProperty("m_enemyHealProgressBar").objectReferenceValue == null)
        {
            throw new InvalidOperationException("RoleSpawnHUD 的療癒進度引用未完整設定。");
        }

        Transform progressRoot = FindTransform(enemySpawnPrefab, "HealProgressRoot");
        Canvas progressCanvas = progressRoot ? progressRoot.GetComponent<Canvas>() : null;
        if (progressCanvas == null
            || progressCanvas.overrideSorting == false
            || progressCanvas.sortingLayerID != 0
            || progressCanvas.sortingOrder != k_healProgressSortingOrder)
        {
            throw new InvalidOperationException("HealProgressRoot 的前景排序 Canvas 設定無效。");
        }

        if (progressRoot.GetComponent<GraphicRaycaster>() != null)
            throw new InvalidOperationException("HealProgressRoot 不應包含 GraphicRaycaster。");

        if (FindTransform(enemySpawnPrefab, "HealProgressFill") == null
            || FindTransform(enemySpawnPrefab, "HealProgressText") == null
            || FindTransform(enemySpawnPrefab, "HealFeedbackAnchor") == null)
        {
            throw new InvalidOperationException("EnemySpawn.prefab 尚有療癒語意節點未完成遷移。");
        }

        Image progressTrack = progressRoot.GetComponent<Image>();
        Image progressFill = FindTransform(enemySpawnPrefab, "HealProgressFill").GetComponent<Image>();
        Image pendingPulse = FindTransform(enemySpawnPrefab, "PendingHealPulse").GetComponent<Image>();
        RectTransform progressRootRect = progressRoot as RectTransform;
        RectTransform progressFillRect = progressFill ? progressFill.rectTransform : null;
        RectTransform pendingPulseRect = pendingPulse ? pendingPulse.rectTransform : null;
        RectTransform progressTextRect = FindTransform(enemySpawnPrefab, "HealProgressText") as RectTransform;
        if (progressTrack == null
            || string.Equals(AssetDatabase.GetAssetPath(progressTrack.sprite), k_healProgressTrackPath, StringComparison.Ordinal) == false
            || progressTrack.type != Image.Type.Sliced
            || progressTrack.raycastTarget
            || progressFill == null
            || string.Equals(AssetDatabase.GetAssetPath(progressFill.sprite), k_healProgressFillPath, StringComparison.Ordinal) == false
            || progressFill.type != Image.Type.Filled
            || progressFill.fillMethod != Image.FillMethod.Horizontal
            || pendingPulse == null
            || string.Equals(AssetDatabase.GetAssetPath(pendingPulse.sprite), k_healProgressFillPath, StringComparison.Ordinal) == false)
        {
            throw new InvalidOperationException("EnemySpawn.prefab 未完整套用新療癒進度條素材與設定。");
        }

        ValidateProgressLayout(
            progressRootRect,
            progressFillRect,
            pendingPulseRect,
            progressTextRect,
            "EnemySpawn.prefab");

        GameObject inBattlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(k_inBattlePrefabPath);
        RoleSpawnHUD nestedHud = inBattlePrefab ? inBattlePrefab.GetComponentInChildren<RoleSpawnHUD>(true) : null;
        if (nestedHud == null || nestedHud.GetComponent<Animation>() != null)
            throw new InvalidOperationException("InBattle.prefab 仍有舊完成度 Animation override。");
        if (FindTransform(inBattlePrefab, "FadeBar") != null)
            throw new InvalidOperationException("InBattle.prefab 仍有舊 FadeBar override。");

        Image nestedProgressTrack = FindTransform(inBattlePrefab, "HealProgressRoot").GetComponent<Image>();
        Image nestedProgressFill = FindTransform(inBattlePrefab, "HealProgressFill").GetComponent<Image>();
        if (nestedProgressTrack == null
            || nestedProgressTrack.color != Color.white
            || string.Equals(AssetDatabase.GetAssetPath(nestedProgressTrack.sprite), k_healProgressTrackPath, StringComparison.Ordinal) == false
            || nestedProgressFill == null
            || nestedProgressFill.color != Color.white
            || string.Equals(AssetDatabase.GetAssetPath(nestedProgressFill.sprite), k_healProgressFillPath, StringComparison.Ordinal) == false)
        {
            throw new InvalidOperationException("InBattle.prefab 仍有舊療癒進度條顏色或素材 override。");
        }

        ValidateProgressLayout(
            nestedProgressTrack.rectTransform,
            nestedProgressFill.rectTransform,
            FindTransform(inBattlePrefab, "PendingHealPulse") as RectTransform,
            FindTransform(inBattlePrefab, "HealProgressText") as RectTransform,
            "InBattle.prefab");

        ValidateAsset<HealValueHUD>(k_healValueTextPrefabPath);
        ValidateAsset<NormalHealEffect>(k_normalHealEffectPrefabPath);
        ValidateAsset<StrongHealEffect>(k_strongHealEffectPrefabPath);
        ValidateAnimation(k_normalHealAnimationPath, "HealValue_Normal");
        ValidateAnimation(k_strongHealAnimationPath, "HealValue_Strong");
        ValidateAnimation(k_strongHealEffectAnimationPath, "StrongHealEffect");
        ValidateAddressableKey(k_normalHealEffectPrefabPath, "NormalHit");
        ValidateAddressableKey(k_strongHealEffectPrefabPath, "CriticleHit");
        ValidateScaledRatioCalculations();
    }

    private static void ValidateProgressLayout(
        RectTransform progressRoot,
        RectTransform progressFill,
        RectTransform pendingPulse,
        RectTransform progressText,
        string prefabName)
    {
        if (progressRoot == null
            || progressFill == null
            || pendingPulse == null
            || progressText == null
            || Vector2Approximately(progressRoot.sizeDelta, new Vector2(400f, 64f)) == false
            || HasExpectedStretchLayout(progressFill, new Vector2(80f, 14f), new Vector2(-80f, -14f)) == false
            || HasExpectedStretchLayout(pendingPulse, new Vector2(80f, 14f), new Vector2(-80f, -14f)) == false
            || HasExpectedStretchLayout(progressText, new Vector2(86f, 10f), new Vector2(-86f, -10f)) == false)
        {
            throw new InvalidOperationException($"{prefabName} 的完成度條版面未對齊。");
        }
    }

    private static bool HasExpectedStretchLayout(
        RectTransform rectTransform,
        Vector2 expectedOffsetMin,
        Vector2 expectedOffsetMax)
    {
        return rectTransform.anchorMin == Vector2.zero
            && rectTransform.anchorMax == Vector2.one
            && Vector2Approximately(rectTransform.offsetMin, expectedOffsetMin)
            && Vector2Approximately(rectTransform.offsetMax, expectedOffsetMax);
    }

    private static bool Vector2Approximately(Vector2 left, Vector2 right)
    {
        return Mathf.Approximately(left.x, right.x)
            && Mathf.Approximately(left.y, right.y);
    }

    private static void ValidateScaledRatioCalculations()
    {
        ValidateScaledRatio(0, 100, 0);
        ValidateScaledRatio(30, 100, 300_000);
        ValidateScaledRatio(100, 100, 1_000_000);
        ValidateScaledRatio(1, 3, 333_333);
        ValidateScaledRatio(2, 3, 666_667);

        BigInteger hugeUnit = BigInteger.Pow(10, 1000);
        ValidateScaledRatio(hugeUnit, hugeUnit * 3, 333_333);
        ValidateScaledRatio(hugeUnit * 2, hugeUnit * 3, 666_667);

        if (BigNumber.TryGetScaledRatio(1, 0, 1_000_000, out int invalidResult)
            || invalidResult != 0)
        {
            throw new InvalidOperationException("BigNumber 無效分母的縮放比例驗證失敗。");
        }
    }

    private static void ValidateScaledRatio(
        BigNumber numerator,
        BigNumber denominator,
        int expectedResult)
    {
        if (BigNumber.TryGetScaledRatio(
                numerator,
                denominator,
                1_000_000,
                out int actualResult) == false
            || actualResult != expectedResult)
        {
            throw new InvalidOperationException(
                $"BigNumber 縮放比例驗證失敗：{numerator}/{denominator}，"
                + $"預期 {expectedResult}，實際 {actualResult}。");
        }
    }

    private static void ValidateAsset<T>(string prefabPath) where T : Component
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null || prefab.GetComponent<T>() == null)
            throw new InvalidOperationException($"Prefab 缺少 {typeof(T).Name}：{prefabPath}");
    }

    private static void ValidateAnimation(string animationPath, string expectedName)
    {
        AnimationClip animationClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animationPath);
        if (animationClip == null || string.Equals(animationClip.name, expectedName, StringComparison.Ordinal) == false)
            throw new InvalidOperationException($"動畫名稱或引用無效：{animationPath}");
    }

    private static void ValidateAddressableKey(string assetPath, string expectedAddress)
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        AddressableAssetEntry entry = settings
            ? settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(assetPath))
            : null;
        if (entry == null || string.Equals(entry.address, expectedAddress, StringComparison.Ordinal) == false)
            throw new InvalidOperationException($"Addressables key 無效：{assetPath}，預期為 {expectedAddress}。");
    }
}
#endif
