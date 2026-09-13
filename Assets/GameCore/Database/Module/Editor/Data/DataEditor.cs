using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.Internal.UIToolkitIntegration;
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace GameCore.Database.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Data), true, isFallback = true)]
    public class DataEditor : OdinEditor
    {
        private protected class ValidatedTextField : IDisposable
        {
            protected readonly TextField m_textField;
            protected readonly SerializedProperty m_bindedProperty;
            protected readonly Func<string, string> m_validator;
            protected readonly Func<string, string> m_sanitizer;
            protected readonly HelpBox m_helpBox;
            protected string m_oldText;
            protected bool m_rollbacksOnFocusOut;
            protected Action<string> m_onValueChanged;
            protected Action<string, string, TextField, SerializedProperty> m_onComplete;

            public ValidatedTextField(TextField textField, SerializedProperty bindedProperty, Func<string, string> validator, Func<string, string> sanitizer = null, HelpBox helpBox = null, bool rollbacksOnFocusOut = false)
            {
                m_textField = textField ?? throw new ArgumentNullException(nameof(textField));
                m_bindedProperty = bindedProperty ?? throw new ArgumentNullException(nameof(bindedProperty));
                m_validator = validator ?? throw new ArgumentNullException(nameof(validator));
                m_sanitizer = sanitizer;
                m_helpBox = helpBox;
                m_rollbacksOnFocusOut = rollbacksOnFocusOut;

                m_textField.RegisterCallback<FocusInEvent>(HandleFocusIn);
                m_textField.RegisterCallback<FocusOutEvent>(HandleFocusOut);
                m_textField.RegisterValueChangedCallback(HandleValueChanged);
            }

            public void Dispose()
            {
                m_textField.UnregisterCallback<FocusInEvent>(HandleFocusIn);
                m_textField.UnregisterCallback<FocusOutEvent>(HandleFocusOut);
                m_textField.UnregisterValueChangedCallback(HandleValueChanged);
            }

            public void OnComplete(Action<string, string, TextField, SerializedProperty> onComplete) => m_onComplete = onComplete;

            public void OnValueChanged(Action<string> onValueChanged) => m_onValueChanged = onValueChanged;

            private void HandleFocusIn(FocusInEvent e)
            {
                m_oldText = m_textField.value;
                ChangeBorderWidth(m_textField, 1f);
            }

            private void HandleFocusOut(FocusOutEvent e)
            {
                string text = m_textField.value;
                string sanitized = m_sanitizer?.Invoke(text);
                string newText = sanitized ?? text;

                if (Validate(newText, false))
                {
                    if (sanitized != null && sanitized != text)
                    {
                        m_bindedProperty.stringValue = sanitized;
                        m_bindedProperty.serializedObject.ApplyModifiedProperties();
                        m_textField.SetValueWithoutNotify(sanitized);
                    }
                    ChangeBorderWidth(m_textField, StyleKeyword.Initial);
                    ChangeBorderColors(m_textField, StyleKeyword.Initial);
                }
                else if (m_rollbacksOnFocusOut)
                {
                    m_bindedProperty.stringValue = m_oldText;
                    m_bindedProperty.serializedObject.ApplyModifiedProperties();
                    m_textField.SetValueWithoutNotify(m_oldText);
                    newText = m_oldText;
                    m_helpBox.RemoveFromHierarchy();
                    ChangeBorderWidth(m_textField, StyleKeyword.Initial);
                    ChangeBorderColors(m_textField, StyleKeyword.Initial);
                }
                m_onValueChanged?.Invoke(newText);
                m_onComplete?.Invoke(m_oldText, newText, m_textField, m_bindedProperty);
            }

            private void HandleValueChanged(ChangeEvent<string> e)
            {
                if (Validate(e.newValue))
                    m_onValueChanged?.Invoke(e.newValue);
            }

            private bool Validate(string value, bool mark = true)
            {
                string message = m_validator(value);
                if (string.IsNullOrEmpty(message))
                {
                    if (m_textField.parent.Contains(m_helpBox))
                        m_textField.parent.Remove(m_helpBox);

                    if (mark)
                        ChangeBorderColors(m_textField, Color.green);
                    return true;
                }
                else
                {
                    if (!m_textField.parent.Contains(m_helpBox))
                        m_textField.parent.Add(m_helpBox);
                    m_helpBox.text = message;

                    if (mark)
                        ChangeBorderColors(m_textField, Color.red);
                    return false;
                }
            }
        }

        public Foldout MenuSettingsFoldout => m_foldout;

        [SerializeField]
        private VisualTreeAsset m_inspectorXml;

        private const char BACKSLASH = '\\';
        private const string INVALID_CHARS_MSG_FORMAT = "輸入值不能含有以下字元: \"{0}\"";
        private const string DUPLICATE_KEY_MSG = "Key必須是唯一";
        private const string EMPTY_KEY_MSG = "Key不能為空值或空白字元";
        private const int HIGHLIGHT_DURATION_MS = 1500;
        private SerializedProperty m_propKey;
        private SerializedProperty m_propPath;
        private SerializedProperty m_propComment;
        private OdinImGuiElement m_odinElement;
        private TextField m_textFieldKey;
        private TextField m_textFieldPath;
        private TextField m_textFieldComment;
        private ValidatedTextField m_validatedFieldKey;
        private ValidatedTextField m_validatedFieldPath;
        private ValidatedTextField m_validatedFieldComment;
        private Foldout m_foldout;
        private IValueAnimation m_highlightAnim;
        private HelpBox m_helpBox;
        private bool m_isKeyEditable;
        private Action<SerializedObject> m_onSerializedObjectValueChanged;

        public override void OnInspectorGUI()
        {
            if (!serializedObject.targetObject) return;

            serializedObject.Update();

            ImguiElementUtils.EmbedVisualElementAndDrawItHere(m_odinElement);
            DrawDefaultInspector();

            serializedObject.ApplyModifiedProperties();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            m_propKey = serializedObject.FindProperty("key");
            m_propPath = serializedObject.FindProperty("m_titlePath");
            m_propComment = serializedObject.FindProperty("m_comment");
            m_odinElement = new OdinImGuiElement(CreateVisualElement());
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            m_validatedFieldKey?.Dispose();
            m_validatedFieldPath?.Dispose();
            m_validatedFieldComment?.Dispose();
        }

        private VisualElement CreateVisualElement()
        {
            VisualElement root = new();
            m_inspectorXml.CloneTree(root);

            m_helpBox = new HelpBox(string.Empty, HelpBoxMessageType.Error);
            m_helpBox.Q<Label>().style.fontSize = 12f;
            m_foldout = root.Q<Foldout>("MenuSettings");
            m_foldout.style.minHeight = 50f;

            // Odin breaks the binding path settings from UXML, properties need to do rebinding here
            m_textFieldKey = root.Q<TextField>("Key");
            m_textFieldKey.BindProperty(m_propKey);
            m_textFieldKey.style.minHeight = 20;
            m_validatedFieldKey = new ValidatedTextField(m_textFieldKey, m_propKey, OnValidateKey, OnSanitizeKeyOrComment, m_helpBox, true);

            m_textFieldPath = root.Q<TextField>("Path");
            m_textFieldPath.BindProperty(m_propPath);
            m_textFieldPath.style.minHeight = 20;
            m_validatedFieldPath = new ValidatedTextField(m_textFieldPath, m_propPath, OnValidatePath, OnSanitizePath, m_helpBox);

            m_textFieldComment = root.Q<TextField>("Comment");
            m_textFieldComment.BindProperty(m_propComment);
            m_textFieldComment.style.minHeight = 20;
            m_validatedFieldComment = new ValidatedTextField(m_textFieldComment, m_propComment, OnValidateComment, OnSanitizeKeyOrComment, m_helpBox);

            m_isKeyEditable = !(serializedObject.isEditingMultipleObjects || ExistsAsset(m_propKey.stringValue));
            if (m_isKeyEditable)
                HighlightMenuSettings();
            else
                m_textFieldKey.RemoveFromHierarchy();

            root.TrackSerializedObjectValue(serializedObject, obj => m_onSerializedObjectValueChanged?.Invoke(obj));
            return root;
        }

        private static void ChangeBorderColors(VisualElement element, StyleColor styleColor)
        {
            var style = element.style;
            style.borderBottomColor = style.borderLeftColor = style.borderRightColor = style.borderTopColor = styleColor;
        }

        private static void ChangeBorderWidth(VisualElement element, StyleFloat styleFloat)
        {
            var style = element.style;
            style.borderBottomWidth = style.borderLeftWidth = style.borderRightWidth = style.borderTopWidth = styleFloat;
        }

        private bool ExistsAsset(string key) => DatabaseEditorUtils.LoadData(serializedObject.targetObject.GetType(), key);

        private string GetInvalidChars(string value, params char[] chars) => string.Join(" ", chars.Where(x => value.Contains(x)));

        private void HighlightMenuSettings()
        {
            m_foldout.value = true;
            m_highlightAnim = m_foldout.experimental.animation.Start(Color.yellow, Color.clear, HIGHLIGHT_DURATION_MS, (element, color) => element.style.backgroundColor = color).Ease(Easing.OutQuad);
        }

        private string OnSanitizeKeyOrComment(string value)
        {
            return value.Replace(Data.COMMENT_SEPARATOR.ToString(), string.Empty).Replace(Data.PATH_SEPARATOR.ToString(), string.Empty).Trim();
        }

        private string OnValidateKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return EMPTY_KEY_MSG;

            string invalidChars = GetInvalidChars(value, Data.COMMENT_SEPARATOR, Data.PATH_SEPARATOR);
            if (invalidChars.Length > 0)
                return string.Format(INVALID_CHARS_MSG_FORMAT, invalidChars);

            #region DataEditorWindow
            if (m_isKeyEditable && m_isRenaming && value == m_oldKey)
                return null;
            #endregion

            if (ExistsAsset(value))
                return DUPLICATE_KEY_MSG;

            return null;
        }

        private string OnSanitizePath(string value)
        {
            return value.Replace(BACKSLASH, Data.PATH_SEPARATOR).Replace(Data.COMMENT_SEPARATOR.ToString(), string.Empty).Trim(' ', Data.PATH_SEPARATOR);
        }

        private string OnValidatePath(string value)
        {
            string invalidChars = GetInvalidChars(value, Data.COMMENT_SEPARATOR);
            if (invalidChars.Length > 0)
                return string.Format(INVALID_CHARS_MSG_FORMAT, invalidChars);
            return null;
        }

        private string OnValidateComment(string value)
        {
            string invalidChars = GetInvalidChars(value, Data.COMMENT_SEPARATOR, Data.PATH_SEPARATOR);
            if (invalidChars.Length > 0)
                return string.Format(INVALID_CHARS_MSG_FORMAT, invalidChars);
            return null;
        }

        #region DataEditorWindow
        private string m_oldKey;
        private bool m_isRenaming;

        internal void OnKeyChanged(Action<string> action) => m_validatedFieldKey.OnValueChanged(action);

        internal void OnModified(Action<SerializedObject> action) => m_onSerializedObjectValueChanged = action;

        internal void SetKeyFieldEditable(bool value, bool isRenaming = false)
        {
            m_isRenaming = isRenaming;
            if (m_isKeyEditable == value) return;
            m_isKeyEditable = value;
            if (m_isKeyEditable)
            {
                m_oldKey = m_propKey.stringValue;
                if (!m_foldout.Contains(m_textFieldKey))
                    m_foldout.Insert(0, m_textFieldKey);

                HighlightMenuSettings();
            }
            else
            {
                m_textFieldKey.RemoveFromHierarchy();
                m_helpBox.RemoveFromHierarchy();
                if (m_highlightAnim?.isRunning ?? false)
                    m_highlightAnim.Stop();
                m_foldout.style.backgroundColor = StyleKeyword.Initial;
            }
        }
        #endregion
    }
}