using UnityEditor;
using UnityEditor.SettingsManagement;

namespace GameCore.Google.Spreadsheets.Editor
{
    static class SheetSettingsProvider
    {
        internal const string PROJECT_SETTINGS_PATH = "Project/Google/Spreadsheets";

        [SettingsProvider]
        private static SettingsProvider CreateSettingsProvider()
        {
            return new UserSettingsProvider(PROJECT_SETTINGS_PATH, SheetSettingsManager.Settings, new[] { typeof(SheetSettingsProvider).Assembly }, SettingsScope.Project);
        }
    }
}