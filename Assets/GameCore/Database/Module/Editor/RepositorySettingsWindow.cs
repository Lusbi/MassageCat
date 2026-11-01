using UnityEditor;
using UnityEngine;

namespace GameCore.Database.Editor
{
    public class RepositorySettingsWindow : EditorWindow
    {
        private const string TITLE_FORMAT = "{0} Settings";
        private string m_repositoryName;
        private string m_defaultFilter;
        private int m_serialNumber;
        private int m_serialNumberInterval;
        private bool m_isChanged;
        private Vector2 m_scrollPosition;

        public static RepositorySettingsWindow Open(string repositoryName)
        {
            string title = string.Format(TITLE_FORMAT, ObjectNames.NicifyVariableName(repositoryName));
            var window = GetWindow<RepositorySettingsWindow>(true, title);
            window.Initialize(repositoryName);
            return window;
        }

        public void Refresh()
        {
            m_defaultFilter = RepositorySettingsHelper.GetDefaultFilter(m_repositoryName);
            m_serialNumber = RepositorySettingsHelper.GetSerialNumber(m_repositoryName);
            m_serialNumberInterval = RepositorySettingsHelper.GetSerialNumberInterval(m_repositoryName);
            Repaint();
        }

        private void Initialize(string repositoryName)
        {
            m_repositoryName = repositoryName;
            Refresh();
        }

        private void Apply()
        {
            RepositorySettingsHelper.SetDefaultFilter(m_repositoryName, m_defaultFilter);
            RepositorySettingsHelper.SetSerialNumber(m_repositoryName, m_serialNumber);
            RepositorySettingsHelper.SetSerialNumberInterval(m_repositoryName, m_serialNumberInterval);
        }

        private void OnGUI()
        {
            using (EditorGUILayout.ScrollViewScope scrollView = new(m_scrollPosition))
            {
                m_scrollPosition = scrollView.scrollPosition;
                EditorGUILayout.Space();
                using EditorGUI.ChangeCheckScope check = new();

                m_defaultFilter = EditorGUILayout.TextField(EditorGUIUtility.TrTextContent("預設篩選詞", "啟動時自動填入的搜尋文字"), m_defaultFilter);

                EditorGUILayout.LabelField("資料新增", EditorStyles.boldLabel);
                using (new EditorGUI.IndentLevelScope())
                {
                    m_serialNumber = Mathf.Max(EditorGUILayout.IntField("從編號...開始", m_serialNumber), 1);
                    m_serialNumberInterval = Mathf.Max(EditorGUILayout.IntField("編號間隔", m_serialNumberInterval), 1);
                }

                if (check.changed)
                    m_isChanged = true;
            }

            using (new EditorGUI.DisabledScope(!m_isChanged))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Revert", EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                    {
                        Refresh();
                        m_isChanged = false;
                    }
                    if (GUILayout.Button("Apply", EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                    {
                        Apply();
                        m_isChanged = false;
                    }
                }
            }
            EditorGUILayout.Space(8f);
        }
    }
}