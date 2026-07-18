// WARNING: Generated file. Do not modify!
using UnityEditor;

namespace GameCore.Database.Editor
{
    public partial class RoleEditorWindow : DataEditorWindow<RoleData>
    {
        [MenuItem("Database/Database/Role", priority = 0)]
        private static void OpenWindow()
        {
            RoleEditorWindow window = GetWindow<RoleEditorWindow>("Role");
            window.Show();
            window.Focus();
        }

        [MenuItem("Database/Database/Role", true)]
        private static bool OpenWindowValidate()
        {
            return DatabaseUser.HasUserName();
        }

        private static void OpenWindowWithKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            RoleEditorWindow window = GetWindow<RoleEditorWindow>("Role");
            window.Show();
            window.Focus();
            window.SetKey(key);
        }
    }
}