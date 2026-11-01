using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace GameCore.Database.Editor
{
    [InitializeOnLoad]
    public static class DatabaseBuilder
    {
        public const string EDITOR_SCRIPT_PATH = "Assets/AssetResources/Database/Editor";
        public const string SCRIPT_PATH = "Assets/AssetResources/Database/Scripts";
        private const string MANAGER_PATH = "Assets/AssetResources/Database/Manager";
        private const string TEMPLATE_ROOT = "Assets/GameCore/Database/Module/Editor/Templates/";
        private const string TEMPLATE_DATA_PATH = TEMPLATE_ROOT + "Template_Data.txt";
        private const string TEMPLATE_REPOSITORY_PATH = TEMPLATE_ROOT + "Template_Repository.txt";
        private const string TEMPLATE_REFERENCE_PATH = TEMPLATE_ROOT + "Template_Reference.txt";
        private const string TEMPLATE_EDITOR_WINDOW_PATH = TEMPLATE_ROOT + "Template_EditorWindow.txt";
        private const string TEMPLATE_DATABASE_MANAGER_PATH = TEMPLATE_ROOT + "Template_DatabaseManager.txt";
        private const string DATABASE_INIT_EXPR_FORMAT = "Database<{0}>.Initialize(OnDatabaseInitialized);";
        private const int INDENT_SPACES = 4;
        private const string TOKEN_DATABASE_COUNT = "$DATABASE_MANAGER_COUNT";
        private const string TOKEN_DATABASE_INIT = "$DATABASE_MANAGER_INITIALIZE";
        private const string EDITORPREFS_MANAGER_HASH = "DatabaseManagerContentHash";
        private const string MANAGER_CLASS_NAME = "DatabaseManager";
        private static readonly StringBuilder stringBuilder = new();
        private static readonly Dictionary<string, string> templateTokenValues = new();

        static DatabaseBuilder()
        {
            EditorApplication.playModeStateChanged += OnEditorPlayModeStateChanged;
            if (!EditorPrefs.HasKey(EDITORPREFS_MANAGER_HASH))
                BeginUpdateManagerScript();
        }

        public static async Task BuildManagerScriptAsync(bool force = false)
        {
            var sortedNames = EditorDatabaseHelper.EnumerateDataClassNameOfAssets().OrderBy(x => x);
            string hash = ComputeHash(sortedNames);
            if (!force && hash == EditorPrefs.GetString(EDITORPREFS_MANAGER_HASH)) return;

            EditorPrefs.SetString(EDITORPREFS_MANAGER_HASH, hash);

            string rawText = LoadTemplate(TEMPLATE_DATABASE_MANAGER_PATH);
            if (string.IsNullOrEmpty(rawText)) return;

            stringBuilder.Clear();
            int databaseCount = 0;
            string lineIndent = new(' ', INDENT_SPACES * 3);
            foreach (var name in sortedNames)
            {
                if (databaseCount > 0)
                    stringBuilder.Append(lineIndent);

                stringBuilder.AppendFormat(DATABASE_INIT_EXPR_FORMAT, name).AppendLine();
                databaseCount++;
            }

            string databaseInitExpr = stringBuilder.ToString();
            stringBuilder.Clear()
                         .Append(rawText)
                         .Replace(TOKEN_DATABASE_COUNT, databaseCount.ToString())
                         .Replace(TOKEN_DATABASE_INIT, databaseInitExpr);

            await GenerateScriptAsync(MANAGER_PATH, MANAGER_CLASS_NAME, stringBuilder.ToString());
        }

        public static async Task CreateAsync(string menuItemName, int priority, string dataBriefName)
        {
            SetTemplateTokenValues(menuItemName, priority, dataBriefName);
            await BuildScriptsFromTemplateAsync(dataBriefName);
        }

        public static bool DataExists(string dataName)
        {
            string path = GetScriptDirectory(dataName);
            return Directory.Exists(path);
        }

        public static string GetScriptDirectory(string dataBriefName) => Path.Combine(SCRIPT_PATH, dataBriefName);

        public static async Task ModifyAsync(string menuItemName, int priority, string dataBriefName)
        {
            SetTemplateTokenValues(menuItemName, priority, dataBriefName);
            await Task.WhenAll(BuildScriptAsync(dataBriefName, EDITOR_SCRIPT_PATH, TEMPLATE_EDITOR_WINDOW_PATH),
                               BuildManagerScriptAsync());
            AssetDatabase.Refresh();
        }

        private static async Task BuildScriptsFromTemplateAsync(string dataBriefName)
        {
            string directoryPath = GetScriptDirectory(dataBriefName);
            await Task.WhenAll(BuildScriptAsync(dataBriefName, directoryPath, TEMPLATE_DATA_PATH),
                               BuildScriptAsync(dataBriefName, directoryPath, TEMPLATE_REPOSITORY_PATH),
                               BuildScriptAsync(dataBriefName, directoryPath, TEMPLATE_REFERENCE_PATH),
                               BuildScriptAsync(dataBriefName, EDITOR_SCRIPT_PATH, TEMPLATE_EDITOR_WINDOW_PATH),
                               BuildManagerScriptAsync());
            AssetDatabase.Refresh();
        }

        private static async Task BuildScriptAsync(string dataBriefName, string directoryPath, string templatePath)
        {
            string template = LoadTemplate(templatePath);
            foreach (var kvp in templateTokenValues)
            {
                template = template.Replace(kvp.Key, kvp.Value);
            }

            int suffixStart = templatePath.LastIndexOf('_') + 1;
            int suffixEnd = templatePath.LastIndexOf('.');
            string suffix = templatePath[suffixStart..suffixEnd];

            await GenerateScriptAsync(directoryPath, dataBriefName + suffix, template);
        }

        private static string ComputeHash(IEnumerable<string> collection)
        {
            string s = string.Join(string.Empty, collection);
            using MD5 md5 = MD5.Create();
            byte[] bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(s));
            StringBuilder sb = new();
            for (int i = 0; i < bytes.Length; i++)
                sb.Append(bytes[i].ToString("x2"));

            return sb.ToString();
        }

        private static async Task GenerateScriptAsync(string directoryPath, string fileName, string contents)
        {
            Directory.CreateDirectory(directoryPath);
            string filePath = Path.Combine(directoryPath, fileName + ".cs");
            //contents = contents.FixLineEndings();
            await File.WriteAllTextAsync(filePath, contents, Encoding.UTF8);
        }

        private static string LoadTemplate(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.Log("The path of template file does not exist: " + filePath);
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<TextAsset>(filePath).text;
        }

        private static void OnEditorPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode) return;
            BuildManagerScriptAsync().Wait(1000);
            AssetDatabase.Refresh();
        }

        private static void SetTemplateTokenValues(string menuItemName, int priority, string dataBriefName)
        {
            templateTokenValues["$MENU_ITEM_NAME"] = menuItemName;
            templateTokenValues["$MENU_ITEM_PRIORITY"] = priority.ToString();
            templateTokenValues["$DATA_TYPE"] = dataBriefName;

            int index = menuItemName.LastIndexOf('/');
            templateTokenValues["$EDITOR_WINDOW_TITLE"] = index != -1 ? menuItemName[(index + 1)..] : menuItemName;
        }

        [MenuItem("GameCore/Database/Update Manager Script")]
        private static async void BeginUpdateManagerScript()
        {
            await BuildManagerScriptAsync(true);
            AssetDatabase.Refresh();
            Debug.Log($"The contents of script {MANAGER_CLASS_NAME}.cs is up-to-date.");
        }
    }
}