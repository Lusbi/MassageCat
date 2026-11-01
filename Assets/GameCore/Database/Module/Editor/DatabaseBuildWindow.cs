using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameCore.Database.Editor
{
    public class DatabaseBuildWindow : EditorWindow
    {
        private string m_currentUserName;

        [MenuItem("GameCore/Database/Window", priority = 1)]
        private static void OpenWindow()
        {
            DatabaseBuildWindow databaseBuildWindow = GetWindow<DatabaseBuildWindow>("Database Build Window");
            databaseBuildWindow.Show();
            databaseBuildWindow.Focus();
        }

        private void OnEnable()
        {
            m_currentUserName = DatabaseUser.GetUserName();
        }

        private Vector2 m_scrollPosition;
        private void OnGUI()
        {
            m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);
            EditorGUILayout.BeginVertical();

            if (string.IsNullOrEmpty(m_currentUserName))
            {
                DrawCreateUserName();
            }
            else
            {
                UpdateExistUserNames();
                DrawModifyUserName();
                UpdateDataNames();
                DrawCreateDatabase();
                DrawModifyMenuItem();
                DrawDeleteDatabase();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private bool m_showCreateUserName = true;
        private string m_userName;
        private void DrawCreateUserName()
        {
            EditorGUILayout.Space();

            m_showCreateUserName = EditorGUILayout.Foldout(m_showCreateUserName, "Create User Name");
            if (!m_showCreateUserName)
            {
                return;
            }

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 1;

            m_userName = EditorGUILayout.TextField("User Name", m_userName);

            EditorGUI.indentLevel = indent;

            if (string.IsNullOrEmpty(m_userName))
            {
                return;
            }

            if (GUILayout.Button("Create User Name"))
            {
                DatabaseUser.SetUserName(m_userName);
                m_currentUserName = m_userName;
                m_userName = string.Empty;
            }
        }

        private readonly List<string> m_existUserNames = new();
        private bool m_hasExistUserNames;
        private string[] m_existUserNamesDisplayOptions;
        private int[] m_existUserNamesOptionValues;
        private void UpdateExistUserNames()
        {
            m_existUserNames.Clear();
            m_existUserNames.AddRange(EditorDatabaseHelper.EnumerateUserNames());
            if (m_existUserNames.Count == 0)
            {
                m_hasExistUserNames = false;
                return;
            }

            int length = m_existUserNames.Count;
            m_existUserNamesDisplayOptions = new string[length];
            m_existUserNamesOptionValues = new int[length];

            for (int i = 0; i < length; i++)
            {
                m_existUserNamesDisplayOptions[i] = m_existUserNames[i];
                m_existUserNamesOptionValues[i] = i;
            }

            m_hasExistUserNames = true;
        }

        private bool m_showModifyUserName = true;
        private bool m_createNewUserName = false;
        private int m_existUserNameIndex;
        private void DrawModifyUserName()
        {
            EditorGUILayout.Space();

            m_showModifyUserName = EditorGUILayout.Foldout(m_showModifyUserName, "Modify User Name");
            if (!m_showModifyUserName)
            {
                return;
            }

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 1;

            EditorGUILayout.LabelField("Current User Name", DatabaseUser.GetUserName());

            m_createNewUserName = EditorGUILayout.Toggle("Create New User Name", m_createNewUserName);
            if (m_createNewUserName)
            {
                m_userName = EditorGUILayout.TextField("New User Name", m_userName);
            }
            else if (m_hasExistUserNames)
            {
                m_existUserNameIndex = EditorGUILayout.IntPopup("Exist User Name", m_existUserNameIndex, m_existUserNamesDisplayOptions, m_existUserNamesOptionValues);
                m_userName = m_existUserNames[m_existUserNameIndex];
            }

            EditorGUI.indentLevel = indent;

            if (string.IsNullOrEmpty(m_userName) ||
                m_userName == DatabaseUser.GetUserName())
            {
                return;
            }

            if (GUILayout.Button("Modify User Name"))
            {
                DatabaseUser.SetUserName(m_userName);
                m_currentUserName = m_userName;
                m_userName = string.Empty;
            }
        }

        private string[] m_dataNames;
        private string[] m_dataNamesDisplayOptions;
        private int[] m_dataNamesOptionValues;
        private void UpdateDataNames()
        {
            m_dataNames = EditorDatabaseHelper.EnumerateDataClassBriefNames().ToArray();
            if (m_dataNames == null || m_dataNames.Length == 0)
            {
                return;
            }

            int length = m_dataNames.Length;
            m_dataNamesDisplayOptions = new string[length];
            m_dataNamesOptionValues = new int[length];

            for (int i = 0; i < length; i++)
            {
                m_dataNamesDisplayOptions[i] = m_dataNames[i];
                m_dataNamesOptionValues[i] = i;
            }
        }

        private bool m_showCreateDatabase = true;
        private string m_dataName;
        private string m_menuItemPath;
        private int m_menuItemPriority;
        private void DrawCreateDatabase()
        {
            EditorGUILayout.Space();

            m_showCreateDatabase = EditorGUILayout.Foldout(m_showCreateDatabase, "Create Database");
            if (!m_showCreateDatabase)
            {
                return;
            }

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 1;

            m_dataName = EditorGUILayout.TextField("Data Name", m_dataName);
            m_menuItemPath = EditorGUILayout.TextField("Menu Item Path", m_menuItemPath);
            m_menuItemPriority = EditorGUILayout.IntField("Menu Item Priority", m_menuItemPriority);

            EditorGUI.indentLevel = indent;

            DrawCreateDatabaseButton();
        }

        private void DrawCreateDatabaseButton()
        {
            if (string.IsNullOrEmpty(m_dataName))
            {
                EditorGUILayout.LabelField(string.Format("The data name is null or empty, can't create the database."));
                return;
            }

            if (DatabaseBuilder.DataExists(m_dataName))
            {
                EditorGUILayout.LabelField(string.Format("The database {0} is already exist, can't create it again.", m_dataName));
                return;
            }

            if (string.IsNullOrEmpty(m_menuItemPath))
            {
                EditorGUILayout.LabelField(string.Format("The menu item path is null or empty, can't create the database.", m_menuItemPath));
                return;
            }

            if (m_menuItemPath[m_menuItemPath.Length - 1] == '/')
            {
                EditorGUILayout.LabelField(string.Format("The menu item path {0} is invalid, please enter the valid name.", m_menuItemPath));
                return;
            }

            if (GUILayout.Button("Create Database"))
            {
                _ = DatabaseBuilder.CreateAsync(m_menuItemPath, m_menuItemPriority, m_dataName);
            }
        }

        private bool m_showModifyMenuItem = true;
        private int m_modifyDataType;
        private string m_modifyMenuItemPath;
        private int m_modifyMenuItemPriority;
        private void DrawModifyMenuItem()
        {
            if (m_dataNames == null || m_dataNames.Length == 0)
            {
                return;
            }

            EditorGUILayout.Space();

            m_showModifyMenuItem = EditorGUILayout.Foldout(m_showModifyMenuItem, "Modify Menu Item");
            if (!m_showModifyMenuItem)
            {
                return;
            }

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 1;

            m_modifyDataType = EditorGUILayout.IntPopup("Data Name", m_modifyDataType, m_dataNamesDisplayOptions, m_dataNamesOptionValues);
            m_modifyMenuItemPath = EditorGUILayout.TextField("Menu Item Path", m_modifyMenuItemPath);
            m_modifyMenuItemPriority = EditorGUILayout.IntField("Menu Item Priority", m_modifyMenuItemPriority);

            EditorGUI.indentLevel = indent;

            DrawModifyMenuItemButton();
        }

        private void DrawModifyMenuItemButton()
        {
            if (m_modifyDataType < 0 ||
                m_modifyDataType >= m_dataNames.Length)
            {
                return;
            }

            string modifyDataName = m_dataNames[m_modifyDataType];

            if (string.IsNullOrEmpty(modifyDataName))
            {
                EditorGUILayout.LabelField(string.Format("The data name is null or empty, can't modify the database.", modifyDataName));
                return;
            }

            if (string.IsNullOrEmpty(m_modifyMenuItemPath))
            {
                EditorGUILayout.LabelField(string.Format("The menu item path is null or empty, can't modify the database.", m_modifyMenuItemPath));
                return;
            }

            if (m_modifyMenuItemPath[m_modifyMenuItemPath.Length - 1] == '/')
            {
                EditorGUILayout.LabelField(string.Format("The menu item path {0} is invalid, please enter the valid name.", m_modifyMenuItemPath));
                return;
            }

            if (GUILayout.Button("Modify Menu Item"))
            {
                _ = DatabaseBuilder.ModifyAsync(m_modifyMenuItemPath, m_modifyMenuItemPriority, modifyDataName);
            }
        }

        private bool m_showDeleteDatabase = true;
        private int m_deleteDataType;
        private void DrawDeleteDatabase()
        {
            if (m_dataNames == null || m_dataNames.Length == 0)
            {
                return;
            }

            EditorGUILayout.Space();

            m_showDeleteDatabase = EditorGUILayout.Foldout(m_showDeleteDatabase, "Delete Database");
            if (!m_showDeleteDatabase)
            {
                return;
            }

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 1;

            m_deleteDataType = EditorGUILayout.IntPopup("Data Name", m_deleteDataType, m_dataNamesDisplayOptions, m_dataNamesOptionValues);

            EditorGUI.indentLevel = indent;

            DrawDeleteDatabaseButton();
        }

        private void DrawDeleteDatabaseButton()
        {
            if (m_deleteDataType < 0 ||
                m_deleteDataType >= m_dataNames.Length ||
                m_dataNames[m_deleteDataType].Contains("Localization"))
            {
                int indent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 1;
                EditorGUILayout.LabelField("Localization data can't be delete from Database Window.");
                EditorGUI.indentLevel = indent;
                return;
            }

            if (GUILayout.Button("Delete Database"))
            {
                BeginDeleteDatabase(m_dataNames[m_deleteDataType]);
                m_deleteDataType = -1;
            }
        }

        public static async void BeginDeleteDatabase(string dataName)
        {
            string path = Application.dataPath.Replace("Assets", Path.Combine("Assets/AssetResources", "Database", "AssetBundleResources"));
            if (Directory.Exists(path))
            {
                FileInfo[] fileInfos = new DirectoryInfo(path).GetFiles("*.asset");
                if (fileInfos != null)
                {
                    for (int i = 0; i < fileInfos.Length; i++)
                    {
                        if (fileInfos[i].Name.Contains(string.Format("{0}{1}", dataName, typeof(Repository).Name)))
                        {
                            try
                            {
                                fileInfos[i].Delete();
                            }
                            catch { }
                        }
                    }
                }
            }

            string generateScriptPath = DatabaseBuilder.GetScriptDirectory(dataName);
            if (Directory.Exists(generateScriptPath))
            {
                Directory.Delete(generateScriptPath, true);
                var metaFile = Path.ChangeExtension(generateScriptPath, ".meta");
                File.Delete(metaFile);
            }
            string editorWindowFileName = dataName + "EditorWindow.cs";
            string filePath = Path.Combine(DatabaseBuilder.EDITOR_SCRIPT_PATH, editorWindowFileName);
            if (File.Exists(filePath))
                File.Delete(filePath);

            await DatabaseBuilder.BuildManagerScriptAsync();
            AssetDatabase.Refresh();
        }

    }
}