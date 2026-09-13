using Sirenix.OdinInspector;
using System.IO;
using UnityEditor;

namespace GameCore.Database.Editor
{
    public class IOControlsGUI<T> : DataEditorControlsGUI<T> where T : Data
    {
        private readonly IDataEditorWindow<T> m_editor;

        public IOControlsGUI(IDataEditorWindow<T> editor)
        {
            m_editor = editor ?? throw new System.ArgumentNullException(nameof(editor));
        }

        [FoldoutGroup("External")]
        [ButtonGroup("External/IO")]
        [Button("匯入檔案", ButtonSizes.Medium)]
        private static async void ImportFromFile()
        {
            string filePath = EditorUtility.OpenFilePanel("Select CSV File", string.Empty, "csv");
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            string dataName = DatabaseEditorUtils.ConvertDataName<T>(string.Empty);
            if (dataName != Path.GetFileNameWithoutExtension(filePath))
            {
                //JSLDebug.LogErrorFormatTag(DatabaseTag.TAG_DATABASE, "The file name is not the same as {0}.csv, please check the file name.", dataName);
                return;
            }

            string text = await File.ReadAllTextAsync(filePath);
            DatabaseEditorUtils.Import<T>(text);
        }

        [ButtonGroup("External/IO")]
        [Button("匯入檔案 (beta)", ButtonSizes.Medium)]
        private async void ImportFromCsv()
        {
            string filePath = EditorUtility.OpenFilePanel("Select CSV File", string.Empty, "csv");
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            string dataName = DatabaseEditorUtils.ConvertDataName<T>(string.Empty);
            if (dataName != Path.GetFileNameWithoutExtension(filePath))
            {
                //JSLDebug.LogErrorFormatTag(DatabaseTag.TAG_DATABASE, "The file name must be {0}.csv.", dataName);
                return;
            }

            await DatabaseEditorUtils.ImportCsvAsync<T>(filePath);
            m_editor.Refresh();
        }

        [ButtonGroup("External/IO")]
        [Button("匯出檔案", ButtonSizes.Medium)]
        private static async void ExportToFile()
        {
            string dataName = DatabaseEditorUtils.ConvertDataName<T>(string.Empty);

            string path = EditorUtility.SaveFilePanel("Export to CSV", "", dataName, "csv");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            string csv = DatabaseEditorUtils.Export<T>(string.Empty);
            await File.WriteAllTextAsync(path, csv, System.Text.Encoding.UTF8);

            EditorUtility.RevealInFinder(path);
        }

        [FoldoutGroup("External")]
        [Button("匯出檔案 (篩選)", ButtonSizes.Medium, Style = ButtonStyle.FoldoutButton, Expanded = true)]
        private static async void ExportToFileByFilter(string filter)
        {
            string dataName = DatabaseEditorUtils.ConvertDataName<T>(string.Empty);

            string path = EditorUtility.SaveFilePanel("Export to CSV", "", dataName, "csv");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            string csv = DatabaseEditorUtils.Export<T>(filter);
            await File.WriteAllTextAsync(path, csv, System.Text.Encoding.UTF8);

            EditorUtility.RevealInFinder(path);
        }
    }
}