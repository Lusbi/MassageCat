using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameCore.Database.Editor
{
    /// <summary>
    /// 預設通用資料搜尋選項
    /// </summary>
    public class DefaultDataSearchOptions<T> : DataSearchOptions<T> where T : Data
    {
        protected override bool DrawsPropertyTree => false;

        private readonly string[] m_userNames;
        private int m_flags = ~0;

        public DefaultDataSearchOptions()
        {
            m_userNames = EditorDatabaseHelper.EnumerateUserNames().ToArray();
        }

        public override bool Filter(T data)
        {
            string owner = GetOwnerFromDataAsset(data);
            if (!string.IsNullOrEmpty(owner))
            {
                for (int i = 0; i < m_userNames.Length; i++)
                {
                    if ((m_flags & 1 << i) != 0 && owner == m_userNames[i])
                        return true;
                }
            }
            return false;
        }

        public override void OnGUI()
        {
            using (EditorGUI.ChangeCheckScope check = new())
            {
                m_flags = EditorGUILayout.MaskField(EditorGUIUtility.TrTextContent("所有者", "篩選資料來自哪些使用者"), m_flags, m_userNames);
                if (check.changed)
                {
                    Enabled = ((m_flags + 1) & ~0) > 1; // Disabled for Nothing or Everything
                    RaiseOptionsChanged();
                }
            }
        }

        public override void Reset()
        {
            base.Reset();
            m_flags = ~0;
        }

        private string GetOwnerFromDataAsset(T data)
        {
            if (!EditorUtility.IsPersistent(data))
                data = DatabaseEditorUtils.LoadData<T>(data.key);
            string assetPath = AssetDatabase.GetAssetPath(data);
            Object mainAsset = string.IsNullOrEmpty(assetPath) ? null : AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (mainAsset)
            {
                int index = mainAsset.name.LastIndexOf('_');
                if (index != -1)
                    return mainAsset.name[(index + 1)..];
            }
            return string.Empty;
        }
    }
}