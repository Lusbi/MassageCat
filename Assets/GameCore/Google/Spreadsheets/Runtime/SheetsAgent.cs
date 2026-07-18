using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util;
using GameCore.Google.Spreadsheets.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("GameCore.Google.Spreadsheets.Editor")]

namespace GameCore.Google.Spreadsheets
{
    public class SheetsAgent : IDisposable
    {
        internal sealed class ValueScope<T> : IDisposable
        {
            private readonly Action<T> setter;
            private readonly T m_default;

            public ValueScope(Action<T> setter, T defaultValue, T initialValue = default)
            {
                this.setter = setter ?? throw new ArgumentNullException(nameof(setter));
                m_default = defaultValue;
                setter(initialValue);
            }

            public void Dispose() => setter(m_default);
        }

        public bool IsUploading { get; protected set; }
        public bool IsDownloading { get; protected set; }

        private const string SHEET_NAMES_REQUEST_FIELDS = "sheets(properties(title))";

        private readonly string m_credentialsFilePath;
        private readonly string m_applicationName;
        private SheetsService m_sheetsService;

        public SheetsAgent(string credentialsFilePath, string applicationName = null)
        {
            m_credentialsFilePath = credentialsFilePath;
            m_applicationName = applicationName;
        }

        /// <summary>
        /// Loads credentials file and creates a <see cref="SheetsService"/> instance.
        /// </summary>
        /// <returns>True if success, otherwise false.</returns>
        public async Task<bool> SetupSheetsServiceAsync()
        {
            if (string.IsNullOrWhiteSpace(m_credentialsFilePath))
            {
                //JSLDebugLogger.LogErrorTag(nameof(SheetsAgent), "Please make sure the path to credentials file is set properly.");
                return false;
            }

            if (!File.Exists(m_credentialsFilePath))
            {
                //JSLDebugLogger.LogErrorTag(nameof(SheetsAgent), "File path does not exist: " + m_credentialsFilePath);
                return false;
            }

            GoogleCredential credential;
            using (var stream = File.OpenRead(m_credentialsFilePath))
            {
                credential = await GoogleCredential.FromStreamAsync(stream, new CancellationTokenSource().Token);
                credential.CreateScoped(SheetsService.Scope.Spreadsheets);
            }

            m_sheetsService = new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = m_applicationName,
            });

            return true;
        }

        /// <summary>
        /// Sets (overwrites) values to a single sheet asynchronously.
        /// </summary>
        /// <param name="spreadsheetId">The ID of source spreadsheet.</param>
        /// <param name="sheetName">The name of source sheet used for specifying a range.</param>
        /// <param name="values">Arrays of values to upload.</param>
        /// <param name="verbose">Logs response messages if true.</param>
        public virtual async Task UploadAsync(string spreadsheetId, string sheetName, IList<IList<object>> values, bool verbose = false)
        {
            if (string.IsNullOrEmpty(spreadsheetId))
            {
                //JSLDebugLogger.LogErrorTag(nameof(SheetsAgent), "Invalid spreadsheet ID.");
                return;
            }

            if (string.IsNullOrEmpty(sheetName))
            {
                //JSLDebugLogger.LogError("Invalid sheet name.");
                return;
            }

            if (!await EnsureSheetsServiceAsync())
                return;

            using var _ = new ValueScope<bool>(value => IsUploading = value, false, true);

            ValueRange body = new() { Values = values };

            SpreadsheetsResource.ValuesResource.UpdateRequest request = m_sheetsService.Spreadsheets.Values.Update(body, spreadsheetId, sheetName);
            request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;

            UpdateValuesResponse response = await request.ExecuteAsync();

            //if (verbose)
            //    JSLDebugLogger.Log(JsonConvert.SerializeObject(response));
        }

        /// <summary>
        /// Gets values from a single sheet by its name asynchronously.
        /// </summary>
        /// <param name="spreadsheetId">The ID of source spreadsheet.</param>
        /// <param name="sheetName">The name of source sheet used for specifying a range.</param>
        /// <returns>A csv-formatted string.</returns>
        public virtual async Task<string> DownloadAsync(string spreadsheetId, string sheetName)
        {
            if (string.IsNullOrEmpty(spreadsheetId))
            {
                //JSLDebugLogger.LogError("Invalid spreadsheet ID.");
                return null;
            }

            if (string.IsNullOrEmpty(sheetName))
            {
                //JSLDebugLogger.LogError("Invalid sheet name.");
                return null;
            }

            if (!await EnsureSheetsServiceAsync())
                return null;

            using var _ = new ValueScope<bool>(value => IsDownloading = value, false, true);

            SpreadsheetsResource.ValuesResource.GetRequest request = m_sheetsService.Spreadsheets.Values.Get(spreadsheetId, sheetName);
            ValueRange response = await request.ExecuteAsync();
            return response.ToCsv();
        }

        /// <summary>
        /// Gets values from one or more sheets asynchronously.
        /// </summary>
        /// <param name="spreadsheetId">The ID of source spreadsheet.</param>
        /// <param name="sheetNames">The names of sheets to get values from.</param>
        /// <returns>A csv-formatted string.</returns>
        public virtual async Task<string> BatchDownloadAsync(string spreadsheetId, IEnumerable<string> sheetNames)
        {
            if (string.IsNullOrEmpty(spreadsheetId))
            {
                //JSLDebugLogger.LogError("Invalid spreadsheet ID.");
                return null;
            }

            if (sheetNames == null)
            {
                //JSLDebugLogger.LogError("Invalid sheet names.");
                return null;
            }

            if (!await EnsureSheetsServiceAsync())
                return null;

            using var _ = new ValueScope<bool>(value => IsDownloading = value, false, true);

            SpreadsheetsResource.ValuesResource.BatchGetRequest request = m_sheetsService.Spreadsheets.Values.BatchGet(spreadsheetId);
            request.Ranges = new Repeatable<string>(sheetNames);

            BatchGetValuesResponse response = await request.ExecuteAsync();
            return response.ToCsv();
        }

        public void Dispose()
        {
            m_sheetsService?.Dispose();
            m_sheetsService = null;
        }

        protected internal virtual async Task FetchSheetNamesAsync(string spreadsheetId, List<string> results, string regexPattern = null)
        {
            if (string.IsNullOrEmpty(spreadsheetId))
            {
                //JSLDebugLogger.LogError("Invalid spreadsheet ID.");
                return;
            }

            if (!await EnsureSheetsServiceAsync())
                return;

            using var _ = new ValueScope<bool>(value => IsDownloading = value, false, true);

            results ??= new List<string>();
            results.Clear();

            SpreadsheetsResource.GetRequest request = m_sheetsService.Spreadsheets.Get(spreadsheetId);
            request.Fields = SHEET_NAMES_REQUEST_FIELDS;

            Spreadsheet response = await request.ExecuteAsync();
            if (response.Sheets.Count == 0)
                return;

            Regex regex = null;
            if (!string.IsNullOrEmpty(regexPattern))
                regex = new(regexPattern, RegexOptions.Compiled);

            results.AddRange(response.Sheets.Select(x => x.Properties.Title).Where(x => regex?.IsMatch(x) ?? true));
        }

        private async Task<bool> EnsureSheetsServiceAsync()
        {
            if (m_sheetsService == null)
                return await SetupSheetsServiceAsync();
            return true;
        }
    }
}
