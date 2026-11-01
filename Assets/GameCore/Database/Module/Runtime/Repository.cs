using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Serialization;


#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using GameCore.Database.Editor;
using GameCore.Google.Spreadsheets.Editor;
#endif

using Object = UnityEngine.Object;

namespace GameCore.Database
{
    public abstract class Repository : ScriptableObject
    {
        public abstract Data[] GetBaseDataArray();

#if UNITY_EDITOR

        public abstract Data GetBaseData(string key);

        [BoxGroup("Batch"), Button(ButtonSizes.Medium)]
        public void ShowWarningLogs()
        {
            Data[] datas = GetBaseDataArray();
            if (datas == null || datas.Length == 0)
            {
                return;
            }

            for (int i = 0; i < datas.Length; i++)
            {
                string warningLog = datas[i].GetWarningLog();
                if (string.IsNullOrEmpty(warningLog))
                {
                    continue;
                }

                //JSLDebug.LogWarning($"{datas[i].GetType().Name} key = {datas[i].key}" + warningLog, datas[i]);
            }
        }

        [BoxGroup("Batch"), Button(ButtonSizes.Medium)]
        public void ShowErrorLogs()
        {
            Data[] datas = GetBaseDataArray();
            if (datas == null || datas.Length == 0)
            {
                return;
            }

            for (int i = 0; i < datas.Length; i++)
            {
                string errorLog = datas[i].GetErrorLog();
                if (string.IsNullOrEmpty(errorLog))
                {
                    continue;
                }

                //JSLDebug.LogError($"{datas[i].GetType().Name} key = {datas[i].key}" + errorLog, datas[i]);
            }
        }

        public string GetUserName()
        {
            int index = name.LastIndexOf('_');
            return index != -1 ? name[(index + 1)..] : string.Empty;
        }
#endif
    }

    public abstract class Repository<T> : Repository where T : Data
    {
        public IReadOnlyList<T> ReadOnlyList => m_dataList.AsReadOnly();

        [HideInInspector]
        [FormerlySerializedAs("m_datas")]
        [SerializeField]
        private List<T> m_dataList = new();

        public override Data[] GetBaseDataArray() => m_dataList.ToArray();

#if UNITY_EDITOR
        [PropertyOrder(-1)]
        [DisplayAsString]
        [LabelText("當前使用者", SdfIconType.PersonFill)]
        [ShowInInspector]
        private string m_currentUserName;

        [HideIf(nameof(m_isCurrentUserOwner))]
        [PropertyOrder(-1)]
        [OnValueChanged(nameof(OnImportToOwnerChanged), InvokeOnInitialize = true)]
        [LabelText("$m_importToOwnerLabelText")]
        [ShowInInspector]
        private bool m_importToOwner;

        [PropertyOrder(-1)]
        [DisplayAsString]
        [LabelText("資料筆數")]
        [ShowInInspector]
        private int DataCount => m_dataList.Count;

        private string m_repoUserName;
        private string m_importToOwnerLabelText;
        private bool m_isCurrentUserOwner;

        public T GetData(string key) => m_dataList.Find(x => x.key == key);

        public IReadOnlyList<T> FilterDataKey(string filter)
        {
            if (m_dataList == null)
                return null;

            if (string.IsNullOrEmpty(filter))
                return m_dataList.AsReadOnly();

            return m_dataList.FindAll(x => x.key.Contains(filter));
        }


        public override Data GetBaseData(string key) => m_dataList.Find(x => x.key == key);

        public T Save(T data, string oldkey = null, bool savesAsCopy = true)
        {
            bool isKeyRenamed = !string.IsNullOrEmpty(oldkey) && data.key != oldkey;
            string targetKey = isKeyRenamed ? oldkey : data.key;

            T existing = m_dataList.Find(x => x.key == targetKey);
            if (existing)
            {
                EditorUtility.CopySerializedManagedFieldsOnly(data, existing);
                if (isKeyRenamed)
                    existing.name = data.name;
            }
            else
            {
                if (savesAsCopy)
                {
                    existing = Instantiate(data);
                    existing.name = data.name;
                }
                else
                {
                    existing = data;
                }

                int index = m_dataList.BinarySearch(existing);
                if (index < 0)
                {
                    index = ~index;
                    m_dataList.Insert(index, existing);
                    AssetDatabase.AddObjectToAsset(existing, this);
                }
            }

            RefreshAsset();
            return existing;
        }

        public void Delete(T data)
        {
            if (!data) return;

            int index = m_dataList.FindIndex(x => x.key == data.key);
            if (index == -1)
            {
                Debug.LogError($"[{name}] Failed to delete data with Key {data.key}");
                return;
            }

            AssetDatabase.RemoveObjectFromAsset(m_dataList[index]);
            m_dataList.RemoveAt(index);
            RefreshAsset();
        }

        public void Delete(IEnumerable<T> dataCollection)
        {
            if (dataCollection == null) return;

            var list = m_dataList.FindAll(x => dataCollection.Any(y => y.key == x.key));
            foreach (var item in list)
            {
                AssetDatabase.RemoveObjectFromAsset(item);
                m_dataList.Remove(item);
            }
            RefreshAsset();
        }

        [OnInspectorInit]
        private void OnInspectorInit()
        {
            InitializeSheetImportBlocks();
            if (string.IsNullOrEmpty(m_repoUserName))
                m_repoUserName = GetUserName();
            if (string.IsNullOrEmpty(m_currentUserName))
                m_currentUserName = DatabaseUser.GetUserName();
            if (string.IsNullOrEmpty(m_importToOwnerLabelText))
                m_importToOwnerLabelText = $"匯入資料至{m_repoUserName} (擁有者)";

            DatabaseUser.OnUserChanged -= OnDatabaseUserChanged;
            DatabaseUser.OnUserChanged += OnDatabaseUserChanged;
            m_isCurrentUserOwner = m_currentUserName == m_repoUserName;
        }

        private void OnDatabaseUserChanged(string userName) => m_currentUserName = userName;

        private void OnImportToOwnerChanged()
        {
            if (HasSheetImportBlocks)
            {
                SheetImportBlock block;
                for (int i = 0; i < sheetImportBlocks.Count; i++)
                {
                    block = sheetImportBlocks[i];
                    block.SetUserNameForImport(m_importToOwner ? m_repoUserName : null);
                    sheetImportBlocks[i] = block;
                }
            }
        }

        private void RefreshAsset()
        {
            if (m_dataList == null || m_dataList.Count == 0)
            {
                string assetPath = AssetDatabase.GetAssetPath(this);
                AssetDatabase.DeleteAsset(assetPath);
            }
            else
            {
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssetIfDirty(this);
            }
            AssetDatabase.Refresh();
        }

        private const string kSheetUrlFormat = "https://docs.google.com/spreadsheets/d/{0}/edit#gid={1}";
        private GoogleSheetDownloader m_googleSheetDownloader = null;

        [EnableIf(nameof(HasKeyAndGid))]
        [ButtonGroup("Google Spreadsheet/IO")]
        [Button("Open")]
        private void OpenSpreadsheetInBrowser()
        {
            Application.OpenURL(string.Format(kSheetUrlFormat, m_key, m_gid));
        }

        private bool HasKey => !string.IsNullOrWhiteSpace(m_key);
        private bool HasKeyAndGid => HasKey && !string.IsNullOrWhiteSpace(m_gid);
        private bool IsImportReady => m_googleSheetDownloader == null && HasKeyAndGid;

        [BoxGroup("Google Spreadsheet"), SerializeField, LabelText("Google Sheet - Key")] private string m_key = string.Empty;
        [BoxGroup("Google Spreadsheet"), SerializeField, LabelText("Google Sheet - Gid")] private string m_gid = string.Empty;
        [ButtonGroup("Google Spreadsheet/IO"), EnableIf(nameof(IsImportReady)), Button("Import", ButtonSizes.Medium)]
        private void ImportFromGoogleSheet()
        {
            m_googleSheetDownloader = new GoogleSheetDownloader(m_key, m_gid, OnDownloadProgressChanged, OnDownloadFinish);
        }

        private const string LOG_DOWNLOADING = "Downloading csv from Google Sheet...";
        private void OnDownloadProgressChanged(float progress)
        {
            EditorUtility.DisplayProgressBar(LOG_DOWNLOADING, string.Empty, progress);
        }

        private const string LOG_DOWNLOAD_FAIL = "Download from url failed, please check the key and gid are correct and the Google Sheet is shared.";
        private void OnDownloadFinish(string text)
        {
            EditorUtility.ClearProgressBar();
            m_googleSheetDownloader = null;
            if (string.IsNullOrEmpty(text))
            {
                //JSLDebug.LogErrorFormatTag(DatabaseTag.TAG_DATABASE, LOG_DOWNLOAD_FAIL);
                return;
            }

            if (m_importToOwner)
                DatabaseEditorUtils.ImportToUser<T>(text, m_repoUserName);
            else
                DatabaseEditorUtils.Import<T>(text);
        }

        private bool IsExportReady => HasKey && !string.IsNullOrEmpty(m_sheetName) && !EditorSheetsAgent.IsUploading;

        [BoxGroup("Google Spreadsheet"), SerializeField, LabelText("Google Sheet - Gas")] private string m_gas = string.Empty;
        [BoxGroup("Google Spreadsheet"), SerializeField, LabelText("Google Sheet - Name")] private string m_sheetName = string.Empty;
        [ButtonGroup("Google Spreadsheet/IO"), EnableIf(nameof(IsExportReady)), Button("Export", ButtonSizes.Medium)]
        private void ExportToGoogleSheet()
        {
            _ = EditorSheetsAgent.UploadAsync(m_key, m_sheetName, DatabaseCSV.ToArrays(m_dataList));
        }

        private bool IsFetchReady => HasKey && !EditorSheetsAgent.IsDownloading;

        [EnableIf(nameof(IsFetchReady))]
        [ButtonGroup("Google Spreadsheet/IO2")]
        private async void FetchSheets()
        {
            using var _ = ListPool<string>.Get(out var list);
            await EditorSheetsAgent.FetchSheetNamesAsync(m_key, list);
            if (list.Count == 0)
                return;

            sheetImportBlocks ??= new List<SheetImportBlock>();
            sheetImportBlocks.Clear();
            sheetImportBlocks.AddRange(list.Select(x => new SheetImportBlock(m_key, x, m_importToOwner ? m_repoUserName : null)));
        }

        private bool IsImportAllReady => HasKey && !EditorSheetsAgent.IsDownloading && sheetImportBlocks?.Count > 0;

        [EnableIf(nameof(IsImportAllReady))]
        [ButtonGroup("Google Spreadsheet/IO2")]
        private async void ImportAll()
        {
            string result = await EditorSheetsAgent.BatchDownloadAsync(m_key, sheetImportBlocks.Select(x => x.sheetName));
            if (!string.IsNullOrEmpty(result))
            {
                if (m_importToOwner)
                    DatabaseEditorUtils.ImportToUser<T>(result, m_repoUserName);
                else
                    DatabaseEditorUtils.Import<T>(result);
            }
        }

        [ShowIf(nameof(HasSheetImportBlocks))]
        [BoxGroup("Google Spreadsheet")]
        [PropertyOrder(1)]
        [ListDrawerSettings(IsReadOnly = true)]
        [LabelText("Sheets")]
        [ShowInInspector]
        private List<SheetImportBlock> sheetImportBlocks;

        private bool HasSheetImportBlocks => sheetImportBlocks?.Count > 0;

        private void InitializeSheetImportBlocks()
        {
            if (sheetImportBlocks != null && sheetImportBlocks.Any(x => string.IsNullOrEmpty(x.sheetName)))
                sheetImportBlocks.Clear();
        }

        private const string LOG_UPLOADING = "Uploading csv to Google Sheet...";
        private void OnUploadProgressChanged(float progress)
        {
            EditorUtility.DisplayProgressBar(LOG_UPLOADING, string.Empty, progress);
        }

        private const string LOG_UPLOAD_FAIL = "Export csv to Google Sheet failed with error \"{0}\".";
        private void OnUploadFinish(string text)
        {
            EditorUtility.ClearProgressBar();
            //m_googleSheetUploader = null;

            if (!string.IsNullOrEmpty(text))
            {
                //JSLDebug.LogErrorFormatTag(DatabaseTag.TAG_DATABASE, LOG_UPLOAD_FAIL, text);
            }
            else
            {
                string url = string.Format("https://docs.google.com/spreadsheets/d/{0}/edit#gid={1}", m_key, m_gid);
                Application.OpenURL(url);
            }
        }

        private Type typeOfEditowWindow;

        [PropertySpace]
        [Button(ButtonSizes.Medium)]
        private void OpenEditorWindow()
        {
            Type type = GetType();
            if (typeOfEditowWindow != null || TryGetEditorWindowType(type, out typeOfEditowWindow))
                EditorWindow.GetWindow(typeOfEditowWindow);
            //else
            //    JSLDebug.LogError("Failed to find the corresponding type of EditorWindow for " + type.FullName);
        }

        private bool TryGetEditorWindowType(Type type, out Type typeOfEditowWindow)
        {
            var assemblyQualifiedName = type.AssemblyQualifiedName;
            var firstCommaIndex = assemblyQualifiedName.IndexOf(',');
            var insertIndex = assemblyQualifiedName.LastIndexOf('.', firstCommaIndex, firstCommaIndex);
            var typeName = type.AssemblyQualifiedName.Insert(insertIndex, ".Editor").Replace("Repository", "EditorWindow").Replace("Assembly-CSharp", "Assembly-CSharp-Editor");
            typeOfEditowWindow = Type.GetType(typeName);
            return typeOfEditowWindow != null;
        }

        [Button(ButtonSizes.Medium)]
        private void ExportToFile()
        {
            string dataName = DatabaseEditorUtils.ConvertDataName<T>(string.Empty);

            string path = EditorUtility.SaveFilePanel("Export to CSV", "", dataName, "csv");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            string csv = DatabaseCSV.ToCSV(m_dataList.ToArray());
            File.WriteAllText(path, csv, System.Text.Encoding.UTF8);

            EditorUtility.RevealInFinder(path);
        }

        [PropertySpace]
        [InfoBox("搭配 Inspector Lock 使用，刪除資料庫底下所選擇到的檔案")]
        [Button(ButtonSizes.Medium)]
        private void RemoveDataAssetsFromSelection()
        {
            Object[] objects = Selection.objects;
            Delete(objects.Cast<T>());
        }

        private bool m_initilizated = false;
        [BoxGroup("External"), LabelText("擴充下載方案"), OnValueChanged("OnExternalInitlizated"), InlineButton("OnExternalInitlizated", "R")] public bool m_externalImportFromGoogleSheet = false;
        [BoxGroup("External"), Title("獨立下載"), HideLabel, OnValueChanged("OnExternalInitlizated"), SerializeField, ShowIf("m_initilizated")] private List<GoogleSheetDownloaderReference> m_googleSheetDownloaderReferences = new List<GoogleSheetDownloaderReference>();
        private void OnExternalInitlizated()
        {
            if (m_externalImportFromGoogleSheet == false)
            {
                return;
            }
            if (m_googleSheetDownloaderReferences == null)
            {
                return;
            }
            foreach (GoogleSheetDownloaderReference googleSheetDownloaderReference in m_googleSheetDownloaderReferences)
            {
                googleSheetDownloaderReference.Setup(OnDownloadProgressChanged, OnDownloadFinish);
            }
            m_initilizated = true;
        }

        [Serializable]
        public struct SheetImportBlock
        {
            [EnableIf(nameof(IsReady))]
            [InlineButton(nameof(OnClickImport), "Import")]
            [DisplayAsString]
            [HideLabel]
            [ShowInInspector]
            [NonSerialized]
            public string sheetName;

            private readonly string m_spreadsheetId;
            private string m_userName;

            private readonly bool IsReady => !EditorSheetsAgent.IsDownloading;

            public SheetImportBlock(string spreadsheetId, string sheetName, string userName = null)
            {
                m_spreadsheetId = spreadsheetId;
                this.sheetName = sheetName;
                m_userName = userName;
            }

            /// <summary>
            /// 設定資料匯入到哪一個使用者
            /// </summary>
            /// <param name="userName">使用者名稱，null表示使用目前的使用者</param>
            public void SetUserNameForImport(string userName)
            {
                m_userName = userName;
            }

            private async readonly void OnClickImport()
            {
                string result = await EditorSheetsAgent.DownloadAsync(m_spreadsheetId, sheetName);
                if (!string.IsNullOrEmpty(result))
                {
                    if (!string.IsNullOrEmpty(m_userName))
                        DatabaseEditorUtils.ImportToUser<T>(result, m_userName);
                    else
                        DatabaseEditorUtils.Import<T>(result);
                }
            }
        }
#endif
    }
}