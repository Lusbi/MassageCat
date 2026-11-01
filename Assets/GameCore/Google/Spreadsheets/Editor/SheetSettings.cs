using UnityEditor;
using UnityEditor.SettingsManagement;
using UnityEngine;

namespace GameCore.Google.Spreadsheets.Editor
{
    class SheetSettings
    {
        internal const string KEYNAME_CREDS_FILEPATH = "google.spreadsheets.credentialsFilePath";
        internal const string KEYNAME_APPLICATION_NAME = "google.spreadsheets.applicationName";
        const string CREDS_HELP_MESSAGE = "When create access credentials, choose \"Service account\" for the authentication method.";
        const string GUIDES_URL = "https://developers.google.com/workspace/guides/create-credentials";

        static readonly GUIContent credsFilePathGUIContent = new("Credentials File Path", "The path to credentials json file. (Required)");
        static readonly GUIContent selectFileGUIContent = EditorGUIUtility.IconContent("d_FolderOpened Icon", "|Browse...");
        static readonly Vector2 iconSize = new(16f, 16f);
        static Settings Settings => SheetSettingsManager.Settings;

        [UserSetting]
        static UserSetting<string> credentialsFilePath = new(Settings, KEYNAME_CREDS_FILEPATH, string.Empty);

        [UserSetting("Sheet Service", "Application Name", "The Application name to be used in the User-Agent header. (Optional)")]
        static UserSetting<string> applicationName = new(Settings, KEYNAME_APPLICATION_NAME, string.Empty);

        [UserSettingBlock("Sheet Service")]
        static void DrawCredentialsGUI(string searchContext)
        {
            using var check = new EditorGUI.ChangeCheckScope();
            using (new EditorGUILayout.HorizontalScope())
            {
                credentialsFilePath.value = SettingsGUILayout.SettingsTextField(credsFilePathGUIContent, credentialsFilePath, searchContext);
                using (new EditorGUIUtility.IconSizeScope(iconSize))
                {
                    if (GUILayout.Button(selectFileGUIContent, GUILayout.Width(24f)))
                    {
                        var selectedPath = EditorUtility.OpenFilePanel("Select Credentials File", null, "json");
                        if (!string.IsNullOrEmpty(selectedPath))
                            credentialsFilePath.value = selectedPath;
                    }
                }
            }

            if (check.changed)
            {
                credentialsFilePath.ApplyModifiedProperties();
                Settings.Save();
            }

            EditorGUILayout.HelpBox(CREDS_HELP_MESSAGE, MessageType.Info);

            if (EditorGUILayout.LinkButton("Go to Guides"))
            {
                Application.OpenURL(GUIDES_URL);
            }
        }
    }
}