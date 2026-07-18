using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;

namespace GameCore.Google.Spreadsheets.Editor
{
    public static class EditorSheetsAgent
    {
        public static bool IsUploading => agent?.IsUploading ?? false;
        public static bool IsDownloading => agent?.IsDownloading ?? false;

        private const string SET_CREDS_FILE_PATH_PROMPT = "The file path of credentials is not set yet. Do you want to set it now?";
        private const string UPDATE_CREDS_FILE_PATH_PROMPT = "The file path of credentials is incorrect. Do you want to update it now?";
        private const string POSITIVE_DIALOG_RESPONSE = "Yes";
        private const string NEGATIVE_DIALOG_RESPONSE = "No";
        private const string UPLOAD_MESSAGE_FORMAT = "Uploading {0}";
        private const string DOWNLOAD_MESSAGE_FORMAT = "Downloading {0}";
        private const string BATCH_DOWNLOAD_MESSAGE = "Batch downloading";
        private const string FETCH_MESSAGE = "Fetching sheet names";

        private static readonly string displayName = ObjectNames.NicifyVariableName(nameof(EditorSheetsAgent));
        private static readonly Progress.Options options = Progress.Options.Indefinite;
        private static SheetsAgent agent;

        /// <summary>
        /// Sets (overwrites) values to a single sheet asynchronously.
        /// </summary>
        /// <param name="spreadsheetId">The ID of source spreadsheet.</param>
        /// <param name="sheetName">The name of source sheet used for specifying a range.</param>
        /// <param name="values">Arrays of values to upload.</param>
        /// <param name="verbose">Logs response messages if true.</param>
        public static async Task UploadAsync(string spreadsheetId, string sheetName, IList<IList<object>> values)
        {
            int progressId = Progress.Start(displayName, string.Format(UPLOAD_MESSAGE_FORMAT, sheetName, spreadsheetId), options);
            if (!await EnsureAgent())
            {
                Progress.Finish(progressId, Progress.Status.Failed);
                return;
            }
            await agent.UploadAsync(spreadsheetId, sheetName, values);
            Progress.Finish(progressId);
        }

        /// <summary>
        /// Gets values from a single sheet by its name asynchronously.
        /// </summary>
        /// <param name="spreadsheetId">The ID of source spreadsheet.</param>
        /// <param name="sheetName">The name of source sheet used for specifying a range.</param>
        /// <returns>A csv-formatted string.</returns>
        public static async Task<string> DownloadAsync(string spreadsheetId, string sheetName)
        {
            int progressId = Progress.Start(displayName, string.Format(DOWNLOAD_MESSAGE_FORMAT, sheetName, spreadsheetId), options);
            if (!await EnsureAgent())
            {
                Progress.Finish(progressId, Progress.Status.Failed);
                return null;
            }
            string result = await agent.DownloadAsync(spreadsheetId, sheetName);
            Progress.Finish(progressId);
            return result;
        }

        /// <summary>
        /// Gets values from one or more sheets asynchronously.
        /// </summary>
        /// <param name="spreadsheetId">The ID of source spreadsheet.</param>
        /// <param name="sheetNames">The names of sheets to get values from.</param>
        /// <returns>A csv-formatted string.</returns>
        public static async Task<string> BatchDownloadAsync(string spreadsheetId, IEnumerable<string> sheetNames)
        {
            int progressId = Progress.Start(displayName, BATCH_DOWNLOAD_MESSAGE, options);
            if (!await EnsureAgent())
            {
                Progress.Finish(progressId, Progress.Status.Failed);
                return null;
            }
            string result = await agent.BatchDownloadAsync(spreadsheetId, sheetNames);
            Progress.Finish(progressId);
            return result;
        }

        public static async Task FetchSheetNamesAsync(string spreadsheetId, List<string> results, string regexPattern = null)
        {
            int progressId = Progress.Start(displayName, FETCH_MESSAGE, options | Progress.Options.Synchronous);
            if (!await EnsureAgent())
            {
                Progress.Finish(progressId, Progress.Status.Failed);
                return;
            }
            await agent.FetchSheetNamesAsync(spreadsheetId, results, regexPattern);
            Progress.Finish(progressId);
        }

        internal static void Reset()
        {
            agent?.Dispose();
            agent = null;
        }

        private static async Task<bool> EnsureAgent()
        {
            if (agent == null)
            {
                if (string.IsNullOrWhiteSpace(SheetSettingsManager.CredentialsFilePath))
                {
                    if (EditorUtility.DisplayDialog("Unspecified File Path", SET_CREDS_FILE_PATH_PROMPT, POSITIVE_DIALOG_RESPONSE, NEGATIVE_DIALOG_RESPONSE))
                        SheetSettingsManager.OpenProjectSettings();
                    return false;
                }

                agent = new(SheetSettingsManager.CredentialsFilePath, SheetSettingsManager.ApplicationName);

                if (!await agent.SetupSheetsServiceAsync())
                {
                    if (EditorUtility.DisplayDialog("Invalid File Path", UPDATE_CREDS_FILE_PATH_PROMPT, POSITIVE_DIALOG_RESPONSE, NEGATIVE_DIALOG_RESPONSE))
                        SheetSettingsManager.OpenProjectSettings();
                    return false;
                }
            }
            return true;
        }
    }
}