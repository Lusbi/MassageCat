using UnityEditor;

namespace GameCore.Database.Dependency.Editor
{
    public static class DependencyMenuItems
    {
        [MenuItem("GameCore/Database/Flush Dependency Cache", priority = 100)]
        private static void FlushCache()
        {
            DependencyManager.FlushCache();
        }

        [MenuItem("GameCore/Database/Reload Dependency Cache", priority = 100)]
        private static void Reload()
        {
            DependencyManager.BeginReloadCache();
        }

        [MenuItem("GameCore/Database/Scan Project Dependencies", priority = 100)]
        private static void ScanProject()
        {
            DependencyManager.Scan();
        }
    }
}