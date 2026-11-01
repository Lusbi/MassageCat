// WARNING: Generated file. Do not modify!
using UnityEditor;

namespace GameCore.Database.Editor
{
    public partial class ScenemapEditorWindow : DataEditorWindow<ScenemapData>
    {
        [MenuItem("Database/Database/Scenemap", priority = 0)]
        private static void OpenWindow()
        {
            ScenemapEditorWindow window = GetWindow<ScenemapEditorWindow>("Scenemap");
            window.Show();
            window.Focus();
        }

        [MenuItem("Database/Database/Scenemap", true)]
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

            ScenemapEditorWindow window = GetWindow<ScenemapEditorWindow>("Scenemap");
            window.Show();
            window.Focus();
            window.SetKey(key);
        }
    }
}