#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace GameCore
{
    public class DeveloperEditorWindow : OdinMenuEditorWindow
    {
        [MenuItem(MenuItemConfig.TOOL_DEVELOPERWINDOW)]
        public static void OpenWindow()
        {
            DeveloperEditorWindow window = GetWindow<DeveloperEditorWindow>();
            window.titleContent = new GUIContent("Developer Tool");
            window.Show();
        }

        public List<DeveloperToolBase> m_developerToolBases = new List<DeveloperToolBase>();

        protected override OdinMenuTree BuildMenuTree()
        {
            m_developerToolBases = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.IsSubclassOf(typeof(DeveloperToolBase)))
                .Select(t => (DeveloperToolBase)Activator.CreateInstance(t))
                .ToList();

            OdinMenuTree menuTree = new OdinMenuTree();

            menuTree.DefaultMenuStyle.IconSize = 30f;
            menuTree.Config.DrawSearchToolbar = false;
            menuTree.Config.DrawScrollView = true;

            foreach (DeveloperToolBase developerToolBase in m_developerToolBases)
            {
                menuTree.Add(developerToolBase.DeveloperName, developerToolBase);
                developerToolBase.Initlization();
            }

            return menuTree;
        }
    }
}
#endif