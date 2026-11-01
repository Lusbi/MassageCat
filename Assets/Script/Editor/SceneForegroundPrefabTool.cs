using System;
using System.Collections.Generic;
using GameCore.Database;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class SceneForegroundPrefabTool
{
    private const int k_sceneCount = 11;
    private const long k_invalidManualRootFileId = 100100000;
    private const string k_repositoryPath = "Assets/AssetResources/Database/Lusbi/ScenemapRepository.asset";
    private const string k_foregroundFolder = "Assets/2DArt/Scene/Foreground";
    private const string k_prefabFolder = k_foregroundFolder + "/Prefabs";

    [MenuItem("Tools/Massage Cat/Rebuild Scene Foregrounds")]
    public static void RebuildAndValidate()
    {
        if (TryLoadSourceData(out ScenemapRepository repository, out Sprite[] sprites,
                out ScenemapData[] sceneMaps) == false)
        {
            Debug.LogError("前景 Prefab 重建已中止：來源資料預檢失敗，未寫入場景資料引用。");
            return;
        }

        GameObject[] prefabs = new GameObject[k_sceneCount];
        for (int i = 0; i < k_sceneCount; i++)
        {
            if (TryRebuildPrefab(i, sprites[i], out prefabs[i]) == false)
            {
                Debug.LogError($"前景 Prefab 重建失敗：SceneMap_{i:00}，未寫入場景資料引用。");
                return;
            }
        }

        for (int i = 0; i < k_sceneCount; i++)
        {
            SerializedObject serializedSceneMap = new SerializedObject(sceneMaps[i]);
            serializedSceneMap.Update();

            SerializedProperty spawnObjectReference =
                serializedSceneMap.FindProperty("m_spawnObjectReference");
            if (spawnObjectReference == null)
            {
                Debug.LogError($"找不到 SceneMap_{i:00} 的 m_spawnObjectReference，場景資料引用未完成。");
                return;
            }

            spawnObjectReference.objectReferenceValue = prefabs[i];
            serializedSceneMap.ApplyModifiedProperties();
            EditorUtility.SetDirty(sceneMaps[i]);
        }

        EditorUtility.SetDirty(repository);
        AssetDatabase.SaveAssets();

        if (ValidateInternal(true))
            Debug.Log("11 組場景前景 Prefab 已重建、綁定並通過驗證。");
        else
            Debug.LogError("前景 Prefab 已重建，但完整驗證失敗，請查看先前的錯誤訊息。");
    }

    [MenuItem("Tools/Massage Cat/Validate Scene Foregrounds")]
    public static void Validate()
    {
        ValidateInternal(true);
    }

    private static bool TryLoadSourceData(out ScenemapRepository repository, out Sprite[] sprites,
        out ScenemapData[] sceneMaps)
    {
        repository = AssetDatabase.LoadAssetAtPath<ScenemapRepository>(k_repositoryPath);
        sprites = new Sprite[k_sceneCount];
        sceneMaps = new ScenemapData[k_sceneCount];

        if (repository == null)
        {
            Debug.LogError($"找不到場景資料庫：{k_repositoryPath}");
            return false;
        }

        IReadOnlyList<ScenemapData> repositoryData = repository.ReadOnlyList;
        if (repositoryData == null || repositoryData.Count != k_sceneCount)
        {
            int dataCount = repositoryData == null ? 0 : repositoryData.Count;
            Debug.LogError($"場景資料筆數應為 {k_sceneCount}，目前為 {dataCount}。");
            return false;
        }

        bool isValid = true;
        for (int i = 0; i < repositoryData.Count; i++)
        {
            ScenemapData sceneMap = repositoryData[i];
            if (sceneMap == null)
            {
                Debug.LogError($"場景資料第 {i} 筆為空值。");
                isValid = false;
                continue;
            }

            int sceneMapNo = sceneMap.SceneMapNo;
            if (sceneMapNo < 0 || sceneMapNo >= k_sceneCount)
            {
                Debug.LogError($"場景編號超出範圍：{sceneMapNo}。");
                isValid = false;
                continue;
            }

            if (sceneMaps[sceneMapNo] != null)
            {
                Debug.LogError($"場景編號重複：SceneMap_{sceneMapNo:00}。");
                isValid = false;
                continue;
            }

            sceneMaps[sceneMapNo] = sceneMap;
        }

        for (int i = 0; i < k_sceneCount; i++)
        {
            if (sceneMaps[i] == null)
            {
                Debug.LogError($"缺少場景資料：SceneMap_{i:00}。");
                isValid = false;
            }
            else
            {
                SerializedObject serializedSceneMap = new SerializedObject(sceneMaps[i]);
                if (serializedSceneMap.FindProperty("m_spawnObjectReference") == null)
                {
                    Debug.LogError($"SceneMap_{i:00} 缺少 m_spawnObjectReference 序列化欄位。");
                    isValid = false;
                }
            }

            string spritePath = GetSpritePath(i);
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprites[i] == null)
            {
                Debug.LogError($"找不到前景 Sprite：{spritePath}");
                isValid = false;
            }
        }

        return isValid;
    }

    private static bool TryRebuildPrefab(int sceneMapNo, Sprite sprite, out GameObject prefab)
    {
        string prefabName = GetPrefabName(sceneMapNo);
        string prefabPath = GetPrefabPath(sceneMapNo);
        GameObject source = new GameObject("__Rebuild_" + prefabName, typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));

        try
        {
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer < 0)
            {
                Debug.LogError("專案缺少 UI Layer，無法建立前景 Prefab。");
                prefab = null;
                return false;
            }

            source.layer = uiLayer;
            RectTransform rectTransform = source.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            Image image = source.GetComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = false;
            image.maskable = true;
            image.useSpriteMesh = true;

            // 先用不同名稱儲存，避免 Unity 依名稱沿用舊手寫 Prefab 的錯誤 local fileID。
            PrefabUtility.SaveAsPrefabAsset(source, prefabPath, out bool resetSuccess);
            if (resetSuccess == false)
            {
                prefab = null;
                return false;
            }

            source.name = prefabName;
            prefab = PrefabUtility.SaveAsPrefabAsset(source, prefabPath, out bool saveSuccess);
            if (saveSuccess == false || prefab == null)
                return false;

            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(prefab, out string guid, out long localId) == false
                || string.IsNullOrEmpty(guid) || localId == 0 || localId == k_invalidManualRootFileId)
            {
                Debug.LogError($"{prefabName} 仍使用無效的 local fileID：{localId}。");
                prefab = null;
                return false;
            }

            return true;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(source);
        }
    }

    private static bool ValidateInternal(bool logSuccess)
    {
        if (TryLoadSourceData(out ScenemapRepository _, out Sprite[] sprites,
                out ScenemapData[] sceneMaps) == false)
            return false;

        bool isValid = true;
        string[] repositoryDependencies = AssetDatabase.GetDependencies(k_repositoryPath, true);
        for (int i = 0; i < k_sceneCount; i++)
        {
            string prefabPath = GetPrefabPath(i);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null || PrefabUtility.IsPartOfPrefabAsset(prefab) == false)
            {
                Debug.LogError($"前景 Prefab 無效：{prefabPath}");
                isValid = false;
                continue;
            }

            Image image = prefab.GetComponent<Image>();
            RectTransform rectTransform = prefab.GetComponent<RectTransform>();
            if (image == null || image.sprite == null || image.sprite != sprites[i])
            {
                Debug.LogError($"{prefab.name} 的 Image 或 Sprite 綁定無效。");
                isValid = false;
            }
            else if (image.raycastTarget)
            {
                Debug.LogError($"{prefab.name} 不應攔截 UI Raycast。");
                isValid = false;
            }

            if (rectTransform == null || rectTransform.anchorMin != Vector2.zero
                || rectTransform.anchorMax != Vector2.one || rectTransform.anchoredPosition != Vector2.zero
                || rectTransform.sizeDelta != Vector2.zero)
            {
                Debug.LogError($"{prefab.name} 的 RectTransform 未設定為全螢幕拉伸。");
                isValid = false;
            }

            if (sceneMaps[i].SpawnObjectReference != prefab)
            {
                Debug.LogError($"SceneMap_{i:00} 未綁定到正確的前景 Prefab。");
                isValid = false;
            }

            if (ContainsPath(repositoryDependencies, prefabPath) == false)
            {
                Debug.LogError($"ScenemapRepository 的依賴鏈缺少前景 Prefab：{prefabPath}");
                isValid = false;
            }

            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(prefab, out string guid, out long localId) == false
                || string.IsNullOrEmpty(guid) || localId == 0 || localId == k_invalidManualRootFileId)
            {
                Debug.LogError($"{prefab.name} 的 Prefab root local fileID 無效：{localId}。");
                isValid = false;
            }
        }

        if (isValid && logSuccess)
            Debug.Log("11 組場景前景 Prefab 與場景資料引用均有效。");

        return isValid;
    }

    private static bool ContainsPath(string[] paths, string targetPath)
    {
        for (int i = 0; i < paths.Length; i++)
        {
            if (string.Equals(paths[i], targetPath, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static string GetSpritePath(int sceneMapNo)
    {
        return $"{k_foregroundFolder}/{GetPrefabName(sceneMapNo)}.png";
    }

    private static string GetPrefabPath(int sceneMapNo)
    {
        return $"{k_prefabFolder}/{GetPrefabName(sceneMapNo)}.prefab";
    }

    private static string GetPrefabName(int sceneMapNo)
    {
        return $"SceneMap_{sceneMapNo:00}_Foreground";
    }
}
