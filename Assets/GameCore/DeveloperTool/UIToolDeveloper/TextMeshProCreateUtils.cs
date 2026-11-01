#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;

public static class TextMeshProCreateUtils
{

    [MenuItem("GameObject/天命奇御/介面/Text (含多國)", false, 0)]
    public static void CreateUIUse()
    {
        GameObject[] gameObjects = Selection.gameObjects;
        if (gameObjects == null || gameObjects.Length == 0)
        {
            GameObject textMeshProObject = new GameObject("TextMeshProUGUI - Text");
            TextMeshProUGUI textMeshProUGUI = textMeshProObject.AddComponent<TextMeshProUGUI>();
            AddFontPro(textMeshProUGUI);
            AddLocalizationPro(textMeshProUGUI);
            SetContentFont(textMeshProObject);
        }
        else
        {
            foreach (GameObject gameObject in gameObjects)
            {
                GameObject textMeshProObject = new GameObject("TextMeshProUGUI - Text");
                TextMeshProUGUI textMeshProUGUI = textMeshProObject.AddComponent<TextMeshProUGUI>();
                AddFontPro(textMeshProUGUI);
                AddLocalizationPro(textMeshProUGUI);
                SetContentFont(textMeshProObject);

                textMeshProObject.transform.SetParent(gameObject.transform);
                textMeshProObject.transform.localPosition = Vector3.zero;
                textMeshProObject.transform.localScale = Vector3.one;
            }
        }
    }

    [MenuItem("GameObject/天命奇御/介面/Text (無多國)", false, 0)]
    public static void CreateDesignerUse()
    {
        GameObject[] gameObjects = Selection.gameObjects;
        if (gameObjects == null || gameObjects.Length == 0)
        {
            GameObject textMeshProObject = new("TextMeshProUGUI - Text");
            TextMeshProUGUI textMeshProUGUI = textMeshProObject.AddComponent<TextMeshProUGUI>();
            AddFontPro(textMeshProUGUI);
            SetContentFont(textMeshProObject);
        }
        else
        {
            foreach (GameObject gameObject in gameObjects)
            {
                GameObject textMeshProObject = new("TextMeshProUGUI - Text");
                TextMeshProUGUI textMeshProUGUI = textMeshProObject.AddComponent<TextMeshProUGUI>();
                AddFontPro(textMeshProUGUI);
                SetContentFont(textMeshProObject);

                textMeshProObject.transform.SetParent(gameObject.transform);
                textMeshProObject.transform.localPosition = Vector3.zero;
                textMeshProObject.transform.localScale = Vector3.one;

                GameObject.Instantiate(textMeshProObject);
            }
        }
    }

    private static void AddFontPro(TextMeshProUGUI textMeshProUGUI)
    {
        //textMeshProUGUI.gameObject.AddComponent<LocalizationFontPro>();
    }

    private static void AddLocalizationPro(TextMeshProUGUI textMeshProUGUI)
    {
        //textMeshProUGUI.gameObject.AddComponent<LocalizationTextPro>();
    }

    public static void SetTitleFont(GameObject gameObject)
    {
        if (gameObject.TryGetComponent<TextMeshProUGUI>(out TextMeshProUGUI textMeshProUGUI))
        {
            textMeshProUGUI.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/AssetBundleResources/Font/FSTitle SDF.asset");
            EditorUtility.SetDirty(textMeshProUGUI);
        }
    }
    public static void SetContentFont(GameObject gameObject)
    {
        if (gameObject.TryGetComponent<TextMeshProUGUI>(out TextMeshProUGUI textMeshProUGUI))
        {
            textMeshProUGUI.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/AssetBundleResources/Font/FSContent SDF.asset");
            EditorUtility.SetDirty(textMeshProUGUI);
        }
    }
}

#endif // UNITY_EDITOR