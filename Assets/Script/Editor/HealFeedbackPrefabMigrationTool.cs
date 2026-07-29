#if UNITY_EDITOR
using System;
using BigMath;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.UI;
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

    private struct PendingPulseVisual
    {
        public Sprite Sprite;
        public Material Material;
        public Color Color;
        public Image.Type Type;
        public Image.FillMethod FillMethod;
        public int FillOrigin;
        public bool FillClockwise;
        public float FillAmount;
        public bool PreserveAspect;
        public float PixelsPerUnitMultiplier;
    }

    [MenuItem("Tools/Massage Cat/Migrate Heal Feedback UX")]
    public static void Migrate()
    {
        PendingPulseVisual pendingPulseVisual = CapturePendingPulseVisual();
        MigrateEnemySpawnPrefab(pendingPulseVisual);
        MigrateInBattlePrefab();
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

    private static PendingPulseVisual CapturePendingPulseVisual()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(k_inBattlePrefabPath);
        try
        {
            Transform fadeBar = FindTransform(prefabRoot, "FadeBar");
            Image sourceImage = fadeBar ? fadeBar.GetComponent<Image>() : null;
            if (sourceImage == null)
                return default;

            return new PendingPulseVisual
            {
                Sprite = sourceImage.sprite,
                Material = sourceImage.material,
                Color = sourceImage.color,
                Type = sourceImage.type,
                FillMethod = sourceImage.fillMethod,
                FillOrigin = sourceImage.fillOrigin,
                FillClockwise = sourceImage.fillClockwise,
                FillAmount = sourceImage.fillAmount,
                PreserveAspect = sourceImage.preserveAspect,
                PixelsPerUnitMultiplier = sourceImage.pixelsPerUnitMultiplier,
            };
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void MigrateEnemySpawnPrefab(PendingPulseVisual pendingPulseVisual)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(k_enemySpawnPrefabPath);
        try
        {
            RoleSpawnHUD roleSpawnHud = prefabRoot.GetComponent<RoleSpawnHUD>();
            if (roleSpawnHud == null)
                throw new InvalidOperationException("EnemySpawn.prefab 缺少 RoleSpawnHUD。");

            RectTransform progressRoot = RequireTransform(prefabRoot, "HealProgressRoot", "HpBar_Bg") as RectTransform;
            progressRoot.name = "HealProgressRoot";
            CreateOrUpdateProgressCanvas(progressRoot);

            RectTransform progressFill = RequireTransform(prefabRoot, "HealProgressFill", "HpBar_Bg (1)") as RectTransform;
            progressFill.name = "HealProgressFill";

            Transform progressText = RequireTransform(prefabRoot, "HealProgressText", "HintText");
            progressText.name = "HealProgressText";

            Transform feedbackAnchor = RequireTransform(prefabRoot, "HealFeedbackAnchor", "HitPanel");
            feedbackAnchor.name = "HealFeedbackAnchor";

            Image pendingPulse = CreateOrUpdatePendingPulse(progressRoot, progressFill.GetComponent<Image>(), pendingPulseVisual);
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

    private static Image CreateOrUpdatePendingPulse(
        RectTransform progressRoot,
        Image fallbackImage,
        PendingPulseVisual pendingPulseVisual)
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
        pulseRect.anchorMin = new Vector2(0f, 0.5f);
        pulseRect.anchorMax = new Vector2(1f, 0.5f);
        pulseRect.pivot = new Vector2(0.5f, 0.5f);
        pulseRect.offsetMin = new Vector2(0f, -5f);
        pulseRect.offsetMax = new Vector2(0f, 5f);
        pulseRect.localScale = Vector3.one;
        pulseRect.SetAsLastSibling();

        Image pulseImage = pulseObject.GetComponent<Image>();
        pulseImage.raycastTarget = false;
        pulseImage.maskable = true;
        pulseImage.sprite = pendingPulseVisual.Sprite ? pendingPulseVisual.Sprite : fallbackImage.sprite;
        pulseImage.material = pendingPulseVisual.Material ? pendingPulseVisual.Material : fallbackImage.material;
        pulseImage.type = pendingPulseVisual.Sprite ? pendingPulseVisual.Type : fallbackImage.type;
        pulseImage.fillMethod = pendingPulseVisual.FillMethod;
        pulseImage.fillOrigin = pendingPulseVisual.FillOrigin;
        pulseImage.fillClockwise = pendingPulseVisual.FillClockwise;
        pulseImage.fillAmount = pendingPulseVisual.Sprite ? pendingPulseVisual.FillAmount : 1f;
        pulseImage.preserveAspect = pendingPulseVisual.PreserveAspect;
        pulseImage.pixelsPerUnitMultiplier = pendingPulseVisual.PixelsPerUnitMultiplier > 0f
            ? pendingPulseVisual.PixelsPerUnitMultiplier
            : 1f;

        Color pulseColor = pendingPulseVisual.Sprite ? pendingPulseVisual.Color : fallbackImage.color;
        pulseColor.a = 0f;
        pulseImage.color = pulseColor;
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

    private static void MigrateInBattlePrefab()
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

        GameObject inBattlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(k_inBattlePrefabPath);
        RoleSpawnHUD nestedHud = inBattlePrefab ? inBattlePrefab.GetComponentInChildren<RoleSpawnHUD>(true) : null;
        if (nestedHud == null || nestedHud.GetComponent<Animation>() != null)
            throw new InvalidOperationException("InBattle.prefab 仍有舊完成度 Animation override。");
        if (FindTransform(inBattlePrefab, "FadeBar") != null)
            throw new InvalidOperationException("InBattle.prefab 仍有舊 FadeBar override。");

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
