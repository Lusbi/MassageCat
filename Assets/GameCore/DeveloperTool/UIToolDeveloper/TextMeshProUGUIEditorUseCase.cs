#if UNITY_EDITOR
using Sirenix.OdinInspector;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class TextMeshProUGUIEditorUseCase
{
    [ShowInInspector]
    public int uisCount => m_lists.Count;

    private List<TextMeshProUGUI> m_lists = new List<TextMeshProUGUI>();

    [SerializeField , PropertyOrder(100) , HideLabel , Title("顏色設定")]
    private ColorGammaToLinnerUseCase m_colorGammaToLinnerUseCase = new ColorGammaToLinnerUseCase();

    public void Clear() => m_lists.Clear();

    public void Apply(GameObject[] gameObjects )
    {
        Clear();
        foreach ( GameObject go in gameObjects )
        {
            if (go.activeInHierarchy == false) continue;

            if (go.TryGetComponent(out TextMeshProUGUI textMeshProUGUI))
            {
                
                m_lists.Add(textMeshProUGUI);
            }
        }
    }

    private TMP_FontAsset m_tempFontAssetTitle = null;
    private TMP_FontAsset m_tempFontAssetContent = null;
    private TMP_SpriteAsset m_tempSpriteAsset = null;

    //[Button("設定內文字型"), ButtonGroup("字型群組")]
    //private void SetContentFont()
    //{
    //    AddLocalizationFont("內文");
    //    m_tempFontAssetContent = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/AssetBundleResources/Font/FSContent/FSContent SDF.asset");
    //    foreach (TextMeshProUGUI textMeshProUGUI in m_lists)
    //    {
    //        Undo.RecordObject(textMeshProUGUI, $"Setting Content Font - {textMeshProUGUI.gameObject.name}");
    //        textMeshProUGUI.font = m_tempFontAssetContent;
    //        EditorUtility.SetDirty(textMeshProUGUI);
    //    }
    //}
    //[Button("設定隸書字型"), ButtonGroup("字型群組")]
    //private void SetLiSuContentFont()
    //{
    //    //AddLocalizationFont("隸書");
    //    m_tempFontAssetContent = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/AssetBundleResources/Font/FSLiSu/FSLiSu SDF.asset");
    //    foreach (TextMeshProUGUI textMeshProUGUI in m_lists)
    //    {
    //        Undo.RecordObject(textMeshProUGUI, $"Setting LiSu Font - {textMeshProUGUI.gameObject.name}");
    //        textMeshProUGUI.font = m_tempFontAssetContent;
    //        EditorUtility.SetDirty(textMeshProUGUI);
    //    }
    //}
    ////[Button("設定多國語系字型"), ButtonGroup("多國語系")]
    //private void AddLocalizationFont(string key = null)
    //{
    //    foreach (TextMeshProUGUI textMeshProUGUI in m_lists)
    //    {
    //        LocalizationFontPro pro = textMeshProUGUI.gameObject.AddComponentIfNull<LocalizationFontPro>();
    //        pro.UpdateReferenceKey(key);
    //        EditorUtility.SetDirty(textMeshProUGUI);
    //    }
    //}
    //[Button("設定多國語系文字(介面用)"), ButtonGroup("多國語系")]
    //private void AddLocalizationText()
    //{
    //    foreach (TextMeshProUGUI textMeshProUGUI in m_lists)
    //    {
    //        Undo.RecordObject(textMeshProUGUI, $"Setting Text Localization - {textMeshProUGUI.gameObject.name}");
    //        textMeshProUGUI.gameObject.AddComponentIfNull<LocalizationTextPro>();
    //        EditorUtility.SetDirty(textMeshProUGUI);
    //    }
    //}

    //[Button("設定圖集")]
    //private void AddTMPSprite()
    //{
    //    if (m_tempSpriteAsset == null)
    //    {
    //        m_tempSpriteAsset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>("Assets/AssetBundleResources/Font/Sprite/FSSprite.asset");
    //    }
    //    foreach (TextMeshProUGUI textMeshProUGUI in m_lists)
    //    {
    //        Undo.RecordObject(textMeshProUGUI, $"Setting Text Sprite- {textMeshProUGUI.gameObject.name}");
    //        textMeshProUGUI.spriteAsset = m_tempSpriteAsset;
    //        EditorUtility.SetDirty(textMeshProUGUI);
    //    }
    //}


    [Button("設定 Gamma Color To Text"), ButtonGroup("文字顏色"), PropertyOrder(101)]
    public void ApplyGammaColorToText()
    {
        foreach (TextMeshProUGUI textMeshProUGUI in m_lists)
        {
            Undo.RecordObject(textMeshProUGUI, $"Setting Text Localization - {textMeshProUGUI.gameObject.name}");
            textMeshProUGUI.color = m_colorGammaToLinnerUseCase.gammaColor;
            EditorUtility.SetDirty(textMeshProUGUI);
        }
    }

    [Button("設定 Linear Color To Text") , ButtonGroup("文字顏色"), PropertyOrder(101)]
    public void ApplyLinearColorToText()
    {
        foreach (TextMeshProUGUI textMeshProUGUI in m_lists)
        {
            Undo.RecordObject(textMeshProUGUI, $"Setting Text Localization - {textMeshProUGUI.gameObject.name}");
            textMeshProUGUI.color = m_colorGammaToLinnerUseCase.linearColor;
            EditorUtility.SetDirty(textMeshProUGUI);
        }
    }

}

#endif // UNITY_EDITOR