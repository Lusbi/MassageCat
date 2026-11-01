using System;
using System.Collections.Generic;
using GameCore.Database.Dependency;
using JSL.UnityRect.Extensions;
using Sirenix.OdinInspector.Editor.Internal.UIToolkitIntegration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace GameCore.Database.Editor
{
    public abstract class DataEditorWindow<T> : EditorWindow, IDataEditorWindow<T> where T : Data
    {
        private class Styles
        {
            public static readonly GUIStyle toolbarTextButton;

            static Styles()
            {
                toolbarTextButton = new GUIStyle(EditorStyles.iconButton)
                {
                    fixedWidth = 0f,
                    stretchWidth = false,
                    alignment = TextAnchor.MiddleLeft,
                    imagePosition = ImagePosition.ImageLeft
                };
            }
        }

        public bool IsNewDocument { get; private set; }

        /// <summary>The copy of data a user is currently editing.</summary>
        [SerializeField] private T m_editingCopy;
        [SerializeField] private T m_source;

        private const float LEFT_PANEL_WIDTH = 300f;
        private const float MARGIN = 5f;
        private static readonly Lazy<T> empty = new(() => CreateInstance<T>());
        private MonoScript m_monoScript;
        private RepositorySettingsWindow m_repositorySettingsWindow;
        private DependencyDrawer m_dependencyDrawer;
        private bool m_isEditingDependencies = false;
        private IReadOnlyList<DependencyInfo> m_dependencies;
        private AssetControlsGUI<T> m_assetControlsGUI;
        private IOControlsGUI<T> m_ioControlsGUI;
        private DataMenuDrawer<T> m_dataMenuDrawer;
        private string m_currentUserName;
        private float m_userButtonWidth;
        private UnityEditor.Editor m_editor;
        private DataEditor m_dataEditor;
        private TwoPaneSplitView m_splitView;
        private IMGUIContainer m_noDocView;
        private VisualElement m_dataVisualElement;
        private Label m_keyLabel;
        private ScrollView m_dataScrollView;
        private bool m_ignoresNextModification;

        private IDataEditorWindow<T> Window => this;
        private IDataSelector<T> DataSelector => m_dataMenuDrawer;
        private bool HasDependencies => m_dependencies?.Count > 0;
        private bool HasEditingCopy => m_editingCopy != null;

        private async void OnEnable()
        {
            EditorSceneManager.sceneOpened += OnEditorSceneOpened;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            m_currentUserName = DatabaseUser.GetUserName();
            DatabaseUser.OnUserChanged += OnDatabaseUserChanged;

            m_assetControlsGUI = new AssetControlsGUI<T>(this);
            m_ioControlsGUI = new IOControlsGUI<T>(this);
            string repoName = DatabaseEditorUtils.ConvertDataName<T>(nameof(Repository));
            m_dataMenuDrawer = new DataMenuDrawer<T>(this, RepositorySettingsHelper.GetDefaultFilter(repoName));

            if (!m_monoScript)
                m_monoScript = GetMonoScript();

            minSize = new Vector2(650f, 500f);
            saveChangesMessage = TextConst.SAVE_CHANGES_MESSAGE;

            if (!IsNewDocument && m_source)
            {
                if (m_editingCopy)
                    DataSelector.Select(m_source);

                using var _ = ListPool<DependencyInfo>.Get(out var list);
                if (await DependencyManager.TryGetDependenciesAsync(m_source, list))
                    m_dependencies = list.ToArray();
            }
        }

        private void OnEditorSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
        {
            if (mode == OpenSceneMode.Single && !m_editingCopy)
                Rebuild();
        }

        private void OnDisable()
        {
            EditorSceneManager.sceneOpened -= OnEditorSceneOpened;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            DatabaseUser.OnUserChanged -= OnDatabaseUserChanged;
            m_dataMenuDrawer.Dispose();
            m_assetControlsGUI.Dispose();
            m_ioControlsGUI.Dispose();
            DestroyImmediate(m_editor);

            if (m_repositorySettingsWindow)
                m_repositorySettingsWindow.Close();
        }

        private void CreateGUI()
        {
            IMGUIContainer toolbar = new(DrawToolbar);
            rootVisualElement.Add(toolbar);
            m_splitView = new TwoPaneSplitView(0, LEFT_PANEL_WIDTH, TwoPaneSplitViewOrientation.Horizontal);
            rootVisualElement.Add(m_splitView);

            IMGUIContainer leftPane = new(() =>
            {
                m_assetControlsGUI.OnGUI();
                m_ioControlsGUI.OnGUI();
                m_dataMenuDrawer.OnGUI();
            });
            var style = leftPane.style;
            style.minWidth = LEFT_PANEL_WIDTH;
            SetMargins(style, MARGIN);
            m_splitView.Add(leftPane);

            m_noDocView = new IMGUIContainer(() =>
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField("請新增或選擇一筆資料以開始編輯", EditorStyles.centeredGreyMiniLabel);
                    GUILayout.FlexibleSpace();
                }
            });
            m_noDocView.style.flexGrow = 1f;

            if (!m_editingCopy && DataSelector.TryGetData(out T data))
                m_editingCopy = Instantiate(data);

            if (m_editingCopy)
            {
                m_dataVisualElement = CreateDataVisualElement(m_editingCopy);
                m_splitView.Add(m_dataVisualElement);
            }
            else
            {
                m_splitView.Add(m_noDocView);
            }
        }

        private void Update()
        {
            m_dataMenuDrawer.Update();
        }

        public override void SaveChanges()
        {
            Window.Save();
            base.SaveChanges();
        }

        private VisualElement CreateDataVisualElement(T data)
        {
            VisualElement root = new() { name = "Root" };
            root.style.flexGrow = 1f;
            m_keyLabel = new Label(data.key);
            m_keyLabel.selection.isSelectable = true;
            m_keyLabel.focusable = true;
            var style = m_keyLabel.style;
            style.unityFontStyleAndWeight = FontStyle.Bold;
            SetMargins(style, MARGIN);
            root.Add(m_keyLabel);
            m_dataEditor = CreateEditor(data, ref m_editor);
            m_dataEditor.OnKeyChanged(value => m_keyLabel.text = value);
            m_dataEditor.OnModified(OnDataModified);
            IMGUIContainer editorContainer = new(m_editor.OnInspectorGUI) { name = "Editor IMGUIContainer" };
            OdinImGuiElement odinElement = new(editorContainer);
            SetMargins(odinElement.style, MARGIN);
            IMGUIContainer dependenciesElement = new(DrawDependencies) { name = "Dependencies IMGUIContainer" };
            style = dependenciesElement.style;
            SetMargins(style, MARGIN);
            style.marginTop = 15f;
            m_dataScrollView = new ScrollView(ScrollViewMode.Vertical);
            m_dataScrollView.Add(odinElement);
            m_dataScrollView.Add(dependenciesElement);
            root.Add(m_dataScrollView);
            return root;
        }

        private DataEditor CreateEditor(UnityEngine.Object targetObject, ref UnityEditor.Editor previousEditor)
        {
            UnityEditor.Editor.CreateCachedEditor(targetObject, typeof(DataEditor), ref previousEditor);
            return previousEditor as DataEditor;
        }

        private void DrawToolbar()
        {
            using (EditorGUILayout.HorizontalScope scope = new("box"))
            {
                if (m_userButtonWidth == 0f)
                    m_userButtonWidth = EditorStyles.miniButton.CalcSize(EditorGUIUtility.TrTempContent(m_currentUserName)).x + EditorStyles.iconButton.fixedWidth;

                GUIContent content = EditorGUIUtility.TrTextContentWithIcon(m_currentUserName, "切換使用者", "SoftlockProjectBrowser Icon");
                if (GUILayout.Button(content, Styles.toolbarTextButton, GUILayout.Width(m_userButtonWidth)))
                {
                    GetWindow<DatabaseBuildWindow>(ObjectNames.NicifyVariableName(nameof(DatabaseBuildWindow)));
                }

                GUILayout.FlexibleSpace();
                if (GUILayout.Button(EditorGUIUtility.TrIconContent("d__Popup", "Settings"), EditorStyles.iconButton))
                {
                    string repoName = DatabaseEditorUtils.ConvertDataName<T>(nameof(Repository));
                    m_repositorySettingsWindow = RepositorySettingsWindow.Open(repoName);
                    Rect rect = m_repositorySettingsWindow.position;
                    rect.position = position.position + new Vector2(position.width + 3f, 0f);
                    m_repositorySettingsWindow.position = rect;
                }
                if (GUILayout.Button(EditorGUIUtility.TrIconContent("d__Menu"), EditorStyles.iconButton) || scope.rect.MouseDown(1))
                {
                    ShowContextMenu();
                }
            }

            void ShowContextMenu()
            {
                GenericMenu menu = new();
                bool hasDependencies = HasDependencies;
                AddMenuItem(EditorGUIUtility.TrTextContent("Refresh"), false, Window.Refresh, true);
                AddMenuItem(EditorGUIUtility.TrTextContent("Rename Key"), false, EnableKeyRenaming, !IsNewDocument && m_source);
                menu.AddSeparator(string.Empty);
                AddMenuItem(EditorGUIUtility.TrTextContent("Edit Script"), false, EditScript, m_monoScript);
                AddMenuItem(EditorGUIUtility.TrTextContent("Locate Script"), false, SelectScript, m_monoScript);
                AddMenuItem(EditorGUIUtility.TrTextContent("Locate Asset"), false, LocateAsset, m_source);
                menu.AddSeparator(string.Empty);
                AddMenuItem(EditorGUIUtility.TrTextContent("Scan Dependencies"), false, ScanDatabase, m_source);
                AddMenuItem(EditorGUIUtility.TrTextContent("Edit Dependencies"), m_isEditingDependencies, () => m_isEditingDependencies ^= true, hasDependencies);
                AddMenuItem(EditorGUIUtility.TrTextContent("Remove All Dependencies"), false, RemoveAllDependencies, hasDependencies);

                menu.ShowAsContext();

                void AddMenuItem(GUIContent content, bool on, GenericMenu.MenuFunction func, bool enabled)
                {
                    if (enabled)
                        menu.AddItem(content, on, func);
                    else
                        menu.AddDisabledItem(content);
                }
            }
        }

        private void DrawDependencies()
        {
            if (!m_source || !HasDependencies) return;

            if (m_isEditingDependencies)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Editing", EditorStyles.miniBoldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(EditorGUIUtility.TrTempContent("Back"), EditorStyles.miniButton, GUILayout.Width(54f)))
                    {
                        m_isEditingDependencies = false;
                        DependencyManager.TryGetDependencies(m_source, out m_dependencies);
                    }
                }
            }
            m_dependencyDrawer ??= new DependencyDrawer();
            m_dependencyDrawer.DrawInspectorGUI(m_source.key, m_dependencies, m_isEditingDependencies);
        }

        private void EnableKeyRenaming()
        {
            if (!m_source || IsNewDocument) return;
            m_dataEditor.SetKeyFieldEditable(true, true);
            m_dataScrollView.ScrollTo(m_dataEditor.MenuSettingsFoldout);
        }

        private T GetData(string key) => DatabaseEditorUtils.LoadData<T>(key);

        private string GetDataFoldoutGroupName() => m_editingCopy ? "Data - " + m_editingCopy.key : "Data";

        public void SetKey(string key)
        {
            T data = GetData(key);
            Window.OnDataSelected(data);
            DataSelector.Select(data);
        }

        private void LocateAsset()
        {
            if (!m_source) return;
            EditorUtility.FocusProjectWindow();
            EditorGUIUtility.PingObject(m_source);
        }

        /// <summary>Returns false if the user cancels the overwriting to the corresponding Data created by another user.</summary>
        private bool CheckOwnership()
        {
            if (IsNewDocument)
                return true;

            string repoUserName = m_source.key != m_editingCopy.key ? DatabaseEditorUtils.GetRepositoryUserName(m_source) : DatabaseEditorUtils.GetRepositoryUserName(m_editingCopy);
            if (string.IsNullOrEmpty(repoUserName) || m_currentUserName == repoUserName)
                return true;

            string message = string.Format(TextConst.DATA_EXIST_MSG_FORMAT, repoUserName, DatabaseEditorUtils.GetRepoAssetName(m_editingCopy), typeof(T).Name, m_editingCopy.key);
            return EditorUtility.DisplayDialog(TextConst.DATA_EXIST_TITLE, message, TextConst.DATA_EXIST_OK, TextConst.CANCEL);
        }

        private void ScanDatabase()
        {
            if (!m_source) return;
            m_isEditingDependencies = false;
            DependencyManager.ResolveDependenciesTo(m_source);
            DependencyManager.TryGetDependencies(m_source, out m_dependencies);
        }

        private void RemoveAllDependencies()
        {
            if (!m_source) return;
            m_isEditingDependencies = false;
            if (EditorUtility.DisplayDialog(TextConst.DEPENDENCY_RM_TITLE, TextConst.DEPENDENCY_RM_MESSAGE, "Confirm", "Cancel"))
            {
                DependencyManager.ClearDependencies(m_source);
            }
        }

        private void EditScript()
        {
            if (m_monoScript == null) return;
            AssetDatabase.OpenAsset(m_monoScript);
        }

        private void SelectScript()
        {
            if (m_monoScript == null) return;
            Selection.activeObject = m_monoScript;
            EditorUtility.FocusProjectWindow();
            EditorGUIUtility.PingObject(m_monoScript);
        }

        private MonoScript GetMonoScript()
        {
            MonoScript[] array = UnityEngine.Resources.FindObjectsOfTypeAll<MonoScript>();
            Type dataType = typeof(T);
            Type classType;
            for (int i = 0; i < array.Length; i++)
            {
                classType = array[i].GetClass();
                if (classType != null && classType.Equals(dataType))
                {
                    return array[i];
                }
            }

            return null;
        }

        private void OnDatabaseUserChanged(string userName)
        {
            m_currentUserName = userName;
            m_userButtonWidth = EditorStyles.miniButton.CalcSize(EditorGUIUtility.TrTempContent(m_currentUserName)).x + EditorStyles.iconButton.fixedWidth;
            Repaint();
        }

        private void OnDataModified(SerializedObject serializedObject)
        {
            if (m_ignoresNextModification)
            {
                m_ignoresNextModification = false;
                return;
            }

            hasUnsavedChanges = true;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                if (!m_editingCopy)
                    Rebuild();
            }
            else if (state == PlayModeStateChange.EnteredPlayMode)
            {
                if (!m_editingCopy)
                    hasUnsavedChanges = false;
            }
        }

        private void Rebuild()
        {
            DestroyImmediate(m_editor);
            hasUnsavedChanges = false;
            rootVisualElement.Clear();
            CreateGUI();
        }

        private void SetMargins(IStyle style, StyleLength styleLength)
        {
            style.marginBottom = style.marginLeft = style.marginRight = style.marginTop = styleLength;
        }

        private void UpdateRightPane(bool showsDocument = true)
        {
            if (showsDocument && m_editingCopy)
            {
                if (m_splitView.Contains(m_noDocView))
                    m_splitView.Remove(m_noDocView);

                m_dataVisualElement ??= CreateDataVisualElement(m_editingCopy);
                m_keyLabel.text = m_editingCopy.key;
                if (!m_splitView.Contains(m_dataVisualElement))
                    m_splitView.Add(m_dataVisualElement);
            }
            else
            {
                if (m_splitView.Contains(m_dataVisualElement))
                    m_splitView.Remove(m_dataVisualElement);

                if (!m_splitView.Contains(m_noDocView))
                    m_splitView.Add(m_noDocView);
            }
        }

        #region IDataEditorWindow<T>
        bool IDataEditorWindow<T>.IsEditing => m_dataVisualElement != null && m_splitView.Contains(m_dataVisualElement) && m_editingCopy;

        void IDataEditorWindow<T>.Copy(ref T destination)
        {
            if (destination)
                EditorUtility.CopySerializedManagedFieldsOnly(m_editingCopy, destination);
            else
                destination = Instantiate(m_editingCopy);
        }

        T IDataEditorWindow<T>.NewEditingCopy()
        {
            IsNewDocument = true;
            if (m_editingCopy)
                EditorUtility.CopySerializedManagedFieldsOnly(empty.Value, m_editingCopy);
            else
                m_editingCopy = CreateInstance<T>();

            m_editingCopy.key = DatabaseEditorUtils.GetNewKey<T>();
            DataSelector.Select(null);
            m_dependencies = null;
            UpdateRightPane();
            m_dataEditor.SetKeyFieldEditable(true);
            hasUnsavedChanges = true;
            if (m_dataScrollView.Contains(m_dataEditor.MenuSettingsFoldout))
                m_dataScrollView.ScrollTo(m_dataEditor.MenuSettingsFoldout);

            if (m_repositorySettingsWindow)
                m_repositorySettingsWindow.Refresh();
            return m_editingCopy;
        }

        bool IDataEditorWindow<T>.Delete()
        {
            if (string.IsNullOrEmpty(m_editingCopy.key))
            {
                //JSLDebug.LogErrorFormatTag(DatabaseTag.TAG_DATABASE, "Can't delete data with null or empty key.");
                return false;
            }

            string repoUserName = DatabaseEditorUtils.GetRepositoryUserName(m_editingCopy);
            if (!string.IsNullOrEmpty(repoUserName) && m_currentUserName != repoUserName)
            {
                string message = string.Format(TextConst.DELETE_MSG_FORMAT, repoUserName, m_editingCopy.key);
                if (!EditorUtility.DisplayDialog(TextConst.DELETE_TITLE, message, TextConst.DELETE_OK, TextConst.CANCEL, DialogOptOutDecisionType.ForThisMachine, TextConst.DELETE_DECISION_KEY))
                    return false;
            }

            IsNewDocument = false;
            DataSelector.OnDelete(m_editingCopy);
            DependencyManager.ClearDependenciesFrom(m_editingCopy);
            DatabaseEditorUtils.DeleteData(m_editingCopy);

            Window.OnDataSelected(null);
            return true;
        }

        void IDataEditorWindow<T>.Duplicate()
        {
            if (m_source)
            {
                IsNewDocument = true;
                EditorUtility.CopySerializedManagedFieldsOnly(m_source, m_editingCopy);
                m_editingCopy.key = DatabaseEditorUtils.GetNewKey<T>();
                DataSelector.Select(null);
                m_dependencies = null;
                m_dataEditor.SetKeyFieldEditable(true);
                UpdateRightPane();
                m_dataScrollView.ScrollTo(m_dataEditor.MenuSettingsFoldout);

                if (m_repositorySettingsWindow)
                    m_repositorySettingsWindow.Refresh();
            }
        }

        void IDataEditorWindow<T>.Paste(T copied)
        {
            if (copied)
            {
                string key = m_editingCopy.key;
                EditorUtility.CopySerializedManagedFieldsOnly(copied, m_editingCopy);
                m_editingCopy.key = key;
            }
        }

        void IDataEditorWindow<T>.Refresh()
        {
            T data = m_source ? GetData(m_source.key) : null;
            Window.OnDataSelected(data);
            DataSelector.Refresh();
            DataSelector.Select(data);
            Repaint();
        }

        void IDataEditorWindow<T>.Revert()
        {
            if (IsNewDocument)
            {
                EditorUtility.CopySerializedManagedFieldsOnly(empty.Value, m_editingCopy);
                m_editingCopy.key = DatabaseEditorUtils.GetNewKey<T>();
            }
            else if (m_source)
            {
                EditorUtility.CopySerializedManagedFieldsOnly(m_source, m_editingCopy);
                hasUnsavedChanges = false;
                m_ignoresNextModification = true;
            }
        }

        void IDataEditorWindow<T>.Save()
        {
            if (m_splitView.Contains(m_noDocView)) return;

            if (string.IsNullOrEmpty(m_editingCopy.key))
            {
                //JSLDebug.LogErrorFormatTag(DatabaseTag.TAG_DATABASE, "Can't save data with null or empty key.");
                return;
            }

            string errorLog = m_editingCopy.GetErrorLog();
            if (!string.IsNullOrEmpty(errorLog))
            {
                string message = string.Format(TextConst.ERRLOG_MSG_FORMAT, errorLog);
                EditorUtility.DisplayDialog(TextConst.ERRLOG_TITLE, message, TextConst.OK);
                return;
            }

            string warningLog = m_editingCopy.GetWarningLog();
            if (!string.IsNullOrEmpty(warningLog))
            {
                string message = string.Format(TextConst.WARNLOG_MSG_FORMAT, warningLog);
                if (!EditorUtility.DisplayDialog(TextConst.WARNLOG_TITLE, message, TextConst.WARNLOG_OK, TextConst.CANCEL))
                    return;
            }

            if (CheckOwnership())
            {
                bool removesOldEntry = false;
                string oldKey = null;
                if (!IsNewDocument)
                {
                    removesOldEntry = m_source.GetTitle() != m_editingCopy.GetTitle();
                    oldKey = m_source.key;
                }

                m_source = DatabaseEditorUtils.SaveData(m_editingCopy, oldKey);
                DependencyManager.ResolveGameCoreData(m_source);

                if (!string.IsNullOrEmpty(oldKey) && m_source.key != oldKey)
                    DependencyManager.Retarget(typeof(T), oldKey, m_source.key);

                IsNewDocument = false;
                m_dataEditor.SetKeyFieldEditable(false);
                DataSelector.OnSave(m_editingCopy, removesOldEntry, oldKey);
                DependencyManager.TryGetDependencies(m_source, out m_dependencies);
                hasUnsavedChanges = false;
                m_ignoresNextModification = true;
                ShowNotification(EditorGUIUtility.TrTempContent("已儲存"), 0.55D);
            }
        }

        void IDataEditorWindow<T>.OnDataSelected(T source)
        {
            m_source = source;
            if (m_source)
            {
                if (m_editingCopy == null)
                    m_editingCopy = CreateInstance<T>();

                EditorUtility.CopySerializedManagedFieldsOnly(m_source, m_editingCopy);
                DependencyManager.TryGetDependencies(m_source, out m_dependencies);
            }
            else
            {
                m_dependencies = null;
            }

            IsNewDocument = false;
            hasUnsavedChanges = false;
            m_ignoresNextModification = true;
            UpdateRightPane(m_source);
            if (m_dataEditor)
                m_dataEditor.SetKeyFieldEditable(false);
        }
        #endregion
    }
}