// WARNING: Generated file. Do not modify!
using UnityEditor;

namespace GameCore.Database.Editor
{
    public partial class FlagEditorWindow : DataEditorWindow<FlagData>
    {
        [MenuItem("Database/Database/Flag", priority = 0)]
        private static void OpenWindow()
        {
            FlagEditorWindow window = GetWindow<FlagEditorWindow>("Flag");
            window.Show();
            window.Focus();
        }

        [MenuItem("Database/Database/Flag", true)]
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

            FlagEditorWindow window = GetWindow<FlagEditorWindow>("Flag");
            window.Show();
            window.Focus();
            window.SetKey(key);
        }
    }
}