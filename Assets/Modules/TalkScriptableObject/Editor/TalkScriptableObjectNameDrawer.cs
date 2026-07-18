#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Tables;

[CustomPropertyDrawer(typeof(TalkScriptableObject))]
public class TalkScriptableObjectNameDrawer : PropertyDrawer
{
    private const string PreviewLabel = "Chinese (Traditional) (zh-TW)";
    private const string TableAssetPath = "Assets/AssetResources/Localization/LocalizationCollection_zh-TW.asset";

    static TalkScriptableObjectNameDrawer()
    {
        EditorApplication.projectChanged += LocalizationTableCache.Clear;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty contentProperty = property.FindPropertyRelative("m_content");
        SerializedProperty durationProperty = property.FindPropertyRelative("m_duration");
        SerializedProperty conditionsProperty = property.FindPropertyRelative("m_conditions");

        float height = EditorGUIUtility.singleLineHeight; // foldout
        height += EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight; // preview field

        if (property.isExpanded)
        {
            height += EditorGUIUtility.standardVerticalSpacing + EditorGUI.GetPropertyHeight(contentProperty, true);
            height += EditorGUIUtility.standardVerticalSpacing + EditorGUI.GetPropertyHeight(durationProperty, true);
            height += EditorGUIUtility.standardVerticalSpacing + EditorGUI.GetPropertyHeight(conditionsProperty, true);
        }

        return height;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty contentProperty = property.FindPropertyRelative("m_content");
        SerializedProperty durationProperty = property.FindPropertyRelative("m_duration");
        SerializedProperty conditionsProperty = property.FindPropertyRelative("m_conditions");

        Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

        int initialIndent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = initialIndent + 1;

        Rect previewRect = new Rect(position.x, foldoutRect.yMax + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUIUtility.singleLineHeight);
        previewRect = EditorGUI.IndentedRect(previewRect);

        string previewText = GetPreviewText(contentProperty?.stringValue);
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUI.TextField(previewRect, PreviewLabel, previewText);
        }

        float yPosition = previewRect.yMax + EditorGUIUtility.standardVerticalSpacing;

        if (property.isExpanded)
        {
            Rect contentRect = new Rect(position.x, yPosition, position.width, EditorGUI.GetPropertyHeight(contentProperty, true));
            contentRect = EditorGUI.IndentedRect(contentRect);
            EditorGUI.PropertyField(contentRect, contentProperty, true);

            yPosition = contentRect.yMax + EditorGUIUtility.standardVerticalSpacing;

            Rect durationRect = new Rect(position.x, yPosition, position.width, EditorGUI.GetPropertyHeight(durationProperty, true));
            durationRect = EditorGUI.IndentedRect(durationRect);
            EditorGUI.PropertyField(durationRect, durationProperty, true);

            yPosition = durationRect.yMax + EditorGUIUtility.standardVerticalSpacing;

            Rect conditionsRect = new Rect(position.x, yPosition, position.width, EditorGUI.GetPropertyHeight(conditionsProperty, true));
            conditionsRect = EditorGUI.IndentedRect(conditionsRect);
            EditorGUI.PropertyField(conditionsRect, conditionsProperty, true);
        }

        EditorGUI.indentLevel = initialIndent;

        EditorGUI.EndProperty();
    }

    private static string GetPreviewText(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return "<ç©ºç??>";
        }

        StringTable table = LocalizationTableCache.Table;
        if (table == null)
        {
            return "<??¾ä????°å??è¡?>";
        }

        var entry = table.GetEntry(key);
        if (entry == null)
        {
            return "<??¾ä????°æ?????>";
        }

        string localizedValue = entry.GetLocalizedString();
        return string.IsNullOrEmpty(localizedValue) ? "<??§å®¹??ºç©º>" : localizedValue;
    }

    private static class LocalizationTableCache
    {
        private static StringTable s_table;

        internal static StringTable Table
        {
            get
            {
                if (s_table == null)
                {
                    s_table = AssetDatabase.LoadAssetAtPath<StringTable>(TableAssetPath);
                }

                return s_table;
            }
        }

        internal static void Clear()
        {
            s_table = null;
        }
    }
}
#endif
