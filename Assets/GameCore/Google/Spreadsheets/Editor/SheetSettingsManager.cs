using UnityEditor;
using UnityEditor.SettingsManagement;

namespace GameCore.Google.Spreadsheets.Editor
{
    public static class SheetSettingsManager
    {
        public static string CredentialsFilePath => Settings.Get<string>(SheetSettings.KEYNAME_CREDS_FILEPATH);
        public static string ApplicationName => Settings.Get<string>(SheetSettings.KEYNAME_APPLICATION_NAME);

        internal const string PACKAGE_NAME = "com.jsl.google-spreadsheets";

        internal static Settings Settings { get; private set; }

        static SheetSettingsManager()
        {
            Settings = new Settings(PACKAGE_NAME);
            Settings.afterSettingsSaved += OnSettingsSaved;
        }

        public static EditorWindow OpenProjectSettings() => SettingsService.OpenProjectSettings(SheetSettingsProvider.PROJECT_SETTINGS_PATH);

        private static void OnSettingsSaved()
        {
            EditorSheetsAgent.Reset();
        }
    }
}