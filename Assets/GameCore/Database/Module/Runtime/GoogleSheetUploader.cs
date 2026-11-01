using System;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine;
using System.Diagnostics;

#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
#else
//using JSLCore.Coroutine;
#endif

namespace GameCore.Database
{
    public class GoogleSheetUploader
    {
        private const string FORMAT_URL = "https://script.google.com/macros/s/{0}/exec";
        private const string FIELD_NAME_KEY = "key";
        private const string FIELD_NAME_GID = "gid";
        private const string FIELD_NAME_CSV = "csv";
        private const string FIELD_NAME_SHEETNAME = "sheetName";
        private const string LOG_UPLOAD = "Upload csv to url {0} cost {1} seconds.";
        private const string FORMAT_UPLOAD_URL = "https://docs.google.com/spreadsheets/d/{0}/edit#gid={1}";

        private string m_url;
        private string m_key;
        private string m_gid;
        private string m_csv;
        private string m_sheetName;
        private Action<float> m_onProgressChanged;
        private Action<string> m_onUploadFinish;
        private Stopwatch m_stopwatch;

        public GoogleSheetUploader(string gas, string key, string gid, string csv , string sheetName, Action<float> onProgressChanged, Action<string> onUploadFinish)
        {
            m_url = string.Format(FORMAT_URL, gas);
            m_key = key;
            m_gid = gid;
            m_csv = csv;
            m_sheetName = sheetName;
            m_onProgressChanged = onProgressChanged;
            m_onUploadFinish = onUploadFinish;

#if UNITY_EDITOR
            m_stopwatch = new Stopwatch();
            m_stopwatch.Start();

            EditorCoroutineUtility.StartCoroutine(Uploading(), this);
#else
            //CoroutineManager.Instance.StartCoroutine(Uploading());
#endif
        }

        private IEnumerator Uploading()
        {
            WWWForm form = new WWWForm();
            form.AddField(FIELD_NAME_KEY, m_key);
            form.AddField(FIELD_NAME_GID, m_gid);
            form.AddField(FIELD_NAME_CSV, m_csv);
            form.AddField(FIELD_NAME_SHEETNAME, m_sheetName);

            UnityWebRequest unityWebRequest = UnityWebRequest.Post(m_url, form);
            unityWebRequest.uploadHandler.contentType = "application/json";
            unityWebRequest.SendWebRequest();

            while (!unityWebRequest.isDone)
            {
                m_onProgressChanged?.Invoke(unityWebRequest.downloadProgress);
                yield return null;
            }

#if UNITY_EDITOR
            m_stopwatch.Stop();
            UnityEngine.Debug.Log(string.Format(LOG_UPLOAD, string.Format(FORMAT_UPLOAD_URL, m_key, m_gid), m_stopwatch.Elapsed.Seconds));
#endif

            m_onUploadFinish?.Invoke(unityWebRequest.error);
        }
    }
}