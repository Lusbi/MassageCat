using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

namespace GameCore.Database.Dependency
{
    public class DependencyDrawer
    {
        private const string FOLDOUT_LABEL_FORMAT = "Used by ({0})";
        private const string OPEN_ASSET_TOOLTIP = "Open the asset referencing this data.";
        private const string SELECT_ASSET_TOOLTIP = "Click to select the asset.";
        private static readonly MethodInfo openFsmEditorWindowWithTemplateMI;
        private static readonly GUIStyle entryStyle;
        private bool m_isVisible;
        private int m_dependencyCount;
        private string m_foldoutLabel;

        static DependencyDrawer()
        {
            entryStyle = new GUIStyle(EditorStyles.label);
            entryStyle.hover.textColor = Color.white;
            entryStyle.fixedHeight = EditorGUIUtility.singleLineHeight;
        }

        public void DrawInspectorGUI(string dataKey, IReadOnlyList<DependencyInfo> m_dependencies, bool isEditing = false)
        {
            if (isEditing)
                m_isVisible = true;
            if (m_dependencyCount != m_dependencies.Count)
            {
                m_dependencyCount = m_dependencies.Count;
                m_foldoutLabel = string.Format(FOLDOUT_LABEL_FORMAT, m_dependencyCount);
            }
            m_isVisible = EditorGUILayout.Foldout(m_isVisible, m_foldoutLabel, true);
            if (!m_isVisible)
                return;

            var groups = m_dependencies.GroupBy(x => x.GroupType);
            using (new EditorGUI.IndentLevelScope())
            {
                string groupLabel;
                foreach (var group in groups)
                {
                    using (ListPool<DependencyInfo>.Get(out var list))
                    {
                        list.AddRange(group);
                        groupLabel = $"{ObjectNames.NicifyVariableName(group.Key.Name)} ({list.Count})";
                        EditorGUILayout.LabelField(groupLabel, EditorStyles.boldLabel);
                        DrawDependencyGroup(list, dataKey, isEditing);
                    }
                }
            }
        }

        private static void DrawDependencyGroup(IEnumerable<DependencyInfo> group, string dataKey, bool isEditing)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                entryStyle.contentOffset = new Vector2(EditorGUI.indentLevel * 15f, 0f);
                GUIContent guiContent2 = EditorGUIUtility.TrTextContent("Open", OPEN_ASSET_TOOLTIP);
                foreach (var dependency in group)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (isEditing)
                        {
                            if (GUILayout.Button(EditorGUIUtility.TrIconContent("d_winbtn_win_close_h"), EditorStyles.iconButton))
                            {
                                DependencyManager.RemoveDependencyFrom(dependency.ObjectHash, dataKey);
                            }
                        }

                        if (isEditing || string.IsNullOrEmpty(dependency.AssetPath))
                        {
                            EditorGUILayout.LabelField(dependency.Name);
                            continue;
                        }

                        Texture icon = AssetDatabase.GetCachedIcon(dependency.AssetPath);
                        GUIContent guiContent = EditorGUIUtility.TrTextContentWithIcon(dependency.Name, SELECT_ASSET_TOOLTIP, icon);
                        if (GUILayout.Button(guiContent, entryStyle))
                        {
                            dependency.OnSelectAsset();
                        }

                        if (GUILayout.Button(guiContent2, EditorStyles.miniButton, GUILayout.Width(54f)))
                        {
                            // Cannot access BehaviorDesignerWindow.ShowWindow, FsmEditorWindow.OpenWindow (Assembly-CSharp-Editor.dll), so hard-coded using System.Reflection
                            if (dependency.GroupType.IsSubclassOf(typeof(Data)))
                            {
                                if (TryGetEditorWindowType(dependency, out Type type))
                                {
                                    MethodInfo methodInfo = type.GetMethod("OpenWindowWithKey", BindingFlags.Static | BindingFlags.NonPublic);
                                    methodInfo?.Invoke(null, new object[] { dependency.Name });
                                }
                            }
                            else
                                dependency.OnOpenAsset();
                        }
                    }
                    EditorGUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
                }
            }
        }

        private static bool TryGetEditorWindowType(DependencyInfo dependency, out Type type)
        {
            var assemblyQualifiedName = dependency.GroupType.AssemblyQualifiedName;
            StringBuilder sb = new(assemblyQualifiedName);
            var firstCommaIndex = assemblyQualifiedName.IndexOf(',');
            var insertIndex1 = assemblyQualifiedName.LastIndexOf('.', firstCommaIndex);
            var insertIndex2 = assemblyQualifiedName.LastIndexOf("Data", firstCommaIndex);
            sb.Replace("Data", "EditorWindow", insertIndex2, 4);
            sb.Insert(insertIndex1, ".Editor");
            sb.Replace("Assembly-CSharp", "Assembly-CSharp-Editor");
            type = Type.GetType(sb.ToString());
            return type != null;
        }
    }
}