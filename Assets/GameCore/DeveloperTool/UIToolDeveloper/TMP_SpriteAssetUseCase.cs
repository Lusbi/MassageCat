#if UNITY_EDITOR
using GameCore.Log;
using GameCore.Utils;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.UI;

[System.Serializable]
public class EditorTMPSpriteAssetInfo
{
    [HideLabel, HideInInspector] public GameObject gameObject;
    [HideLabel, HideInInspector] public TMP_SpriteCharacter tMP_SpriteCharacter;

    [OnValueChanged("UpdateGlyphMetrics", true)]
    public GlyphMetrics glyphMetrics;


    private TextMeshProUGUI m_textMeshProUGUI;
    public EditorTMPSpriteAssetInfo(GameObject gameObject, TMP_SpriteCharacter tMP_SpriteCharacter)
    {
        this.gameObject = gameObject;
        this.tMP_SpriteCharacter = tMP_SpriteCharacter;
        glyphMetrics = tMP_SpriteCharacter.glyph.metrics;

        m_textMeshProUGUI = gameObject.GetComponent<TextMeshProUGUI>();
        SetText();
    }
    private void SetText()
    {
        m_textMeshProUGUI.rectTransform.sizeDelta = new Vector2(500, 50);
        m_textMeshProUGUI.text = $"圖示 <sprite name=\"{tMP_SpriteCharacter.name}\"> 對齊調整";
    }
    private void UpdateGlyphMetrics()
    {
        for (int i = 0; i < m_textMeshProUGUI.spriteAsset.spriteCharacterTable.Count; i++)
        {
            if (m_textMeshProUGUI.spriteAsset.spriteCharacterTable[i].name == tMP_SpriteCharacter.name)
            {
                m_textMeshProUGUI.spriteAsset.spriteCharacterTable[i].glyph.metrics = glyphMetrics;
            }
        }
        m_textMeshProUGUI.enabled = false;
        eLog.Log("更新設定");
        m_textMeshProUGUI.enabled = true;
    }
}

[System.Serializable]
public class TMP_SpriteAssetUseCase
{
    private const string TEMPLATE_NAME = "[SpriteAsset Template] TextMeshUgui";
    private const string PARENT_NAME = "[TextMeshUgui Tool] Parent";

    [LabelText("要調整的 SpriteAsset"), AssetSelector]
    public TMP_SpriteAsset tMP_SpriteAsset;

    private GameObject m_parent;
    private GameObject m_template;

    private Dictionary<int, EditorTMPSpriteAssetInfo> m_caches = new Dictionary<int, EditorTMPSpriteAssetInfo>();

    public TMP_SpriteAssetUseCase()
    {
        Selection.selectionChanged -= OnSelectionChanged;
        Selection.selectionChanged += OnSelectionChanged;
    }

    ~TMP_SpriteAssetUseCase()
    {
        if (m_parent != null)
        {
            GameObject.DestroyImmediate(m_parent.gameObject);
            m_caches.Clear();
        }

        Selection.selectionChanged -= OnSelectionChanged;
    }

    [InfoBox("於 Hierarchy 選擇到物件工具生成的子物件後，顯示資訊")]
    [PropertyOrder(20)]
    public List<EditorTMPSpriteAssetInfo> m_selection = new List<EditorTMPSpriteAssetInfo>();
    private void OnSelectionChanged()
    {
        m_selection.Clear();
        if (m_caches == null)
            return;
        GameObject[] gameObjects = Selection.gameObjects;
        foreach (GameObject obj in gameObjects)
        {
            int id = obj.GetInstanceID();

            if (m_caches.TryGetValue(id, out var data))
            {
                m_selection.Add(data);
            }
        }
    }

    [Button("創建子物件"), ShowIf(nameof(tMP_SpriteAsset)), PropertyOrder(5)]
    public void Execute()
    {
        CreateTemplate();
        CreateParent();
    }

    [Button("儲存資料"), ShowIf(nameof(tMP_SpriteAsset)), PropertyOrder(6)]
    public void Save()
    {
        EditorUtility.SetDirty(tMP_SpriteAsset);
        AssetDatabase.SaveAssets();
    }

    private bool hasParent => m_parent != null;
    [Button("刪除編輯用物件"), ShowIf(nameof(hasParent))]
    public void Delete()
    {
        if (m_parent != null)
            GameObject.DestroyImmediate(m_parent.gameObject);
        if (m_parent)
            GameObject.DestroyImmediate(m_template.gameObject);
        m_caches.Clear();
    }
    private void CreateTemplate()
    {
        if (m_template == null)
        {
            m_template = GameObject.Find(PARENT_NAME);
            if (m_template == null)
            {
                m_template = new GameObject(TEMPLATE_NAME);
                var tmp_text = m_template.AddComponent<TextMeshProUGUI>();
                tmp_text.spriteAsset = tMP_SpriteAsset;
                tmp_text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/AssetBundleResources/Font/FSContent/FSContent SDF.asset");

                m_template.gameObject.SetActive(false);
            }
        }
    }

    private void CreateParent()
    {
        Delete();
        if (m_parent == null)
        {
            m_parent = GameObject.Find(PARENT_NAME);
            if (m_parent == null)
                m_parent = new GameObject(PARENT_NAME);

            m_parent.transform.SetParent(GameObject.FindAnyObjectByType<Canvas>().transform);
            m_parent.transform.localPosition = Vector3.zero;
            m_parent.transform.localScale = Vector3.one;

            var gridGroup = m_parent.AddComponentIfNull<GridLayoutGroup>();
            gridGroup.cellSize = new Vector2(380, 100);

            RectTransform rectTransform = gridGroup.GetComponent<RectTransform>();
            rectTransform.anchorMax = Vector2.one;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.pivot = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.rect.Set(0, 0, 0, 0);

        }

        int index = 0;
        foreach (var spriteCharacter in tMP_SpriteAsset.spriteCharacterTable)
        {
            GameObject clone = GameObject.Instantiate(m_template);
            clone.name = spriteCharacter.name;
            clone.transform.SetParent(m_parent.transform);
            clone.transform.localPosition = new Vector3(0, 500 + index * -80, 0);
            clone.transform.localScale = Vector3.one;
            clone.gameObject.SetActive(true);

            EditorTMPSpriteAssetInfo editorTMPSpriteAssetInfo = new EditorTMPSpriteAssetInfo(clone, spriteCharacter);
            m_caches.Add(clone.GetInstanceID(), editorTMPSpriteAssetInfo);

            index++;
        }
    }

}

#endif