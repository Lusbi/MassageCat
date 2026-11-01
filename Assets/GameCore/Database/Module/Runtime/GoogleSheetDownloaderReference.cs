using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GameCore.Database
{
    [System.Serializable]
    public class GoogleSheetDownloaderReference
    {
        [BoxGroup("Buttons"), SerializeField, LabelText("Google Sheet - Key") , OnValueChanged("EditorPrefsSave")] private string m_key = "";
        [BoxGroup("Buttons"), SerializeField, LabelText("Google Sheet - Gid"), OnValueChanged("EditorPrefsSave")] private string m_gid = "";
        [BoxGroup("Buttons"), SerializeField, LabelText("Google Sheet - Gas"), OnValueChanged("EditorPrefsSave")] private string m_gas = "";
        [BoxGroup("Buttons"), SerializeField, LabelText("SheetName"), OnValueChanged("EditorPrefsSave")] private string m_sheetName = "";
        [BoxGroup("Buttons"), SerializeField, OnValueChanged("EditorPrefsSave")] private string m_referenceTitle = "";

        public string Key { get { return m_key; } set { m_key = value; } }
        public string Gid { get { return m_gid; } set { m_gid = value; } }

        private GoogleSheetDownloader m_googleSheetDownloader = null;
        private GoogleSheetUploader m_googleSheetUploader = null;
        private System.Action<float> m_OnDownloadProgressChanged = null;
        private System.Action<string> m_OnDownloadFinish = null;

#if UNITY_EDITOR
        private string editorPrefsHead;
        public GoogleSheetDownloaderReference() { }
        public GoogleSheetDownloaderReference(string editorPrefsHead)
        {
            this.editorPrefsHead = editorPrefsHead;
            m_key = EditorPrefs.GetString($"{editorPrefsHead}_Key", null);
            m_gid = EditorPrefs.GetString($"{editorPrefsHead}_Gid", null);
            m_gas = EditorPrefs.GetString($"{editorPrefsHead}_Gas", null);
            m_sheetName = EditorPrefs.GetString($"{editorPrefsHead}_SheetName", null);
            m_referenceTitle = EditorPrefs.GetString($"{editorPrefsHead}_ReferenceTitle", null);
        }

        private void EditorPrefsSave()
        {
            if (string.IsNullOrEmpty(editorPrefsHead) == false)
            {
                EditorPrefs.SetString($"{editorPrefsHead}_Key", m_key);
                EditorPrefs.SetString($"{editorPrefsHead}_Gid", m_gid);
                EditorPrefs.SetString($"{editorPrefsHead}_Gas", m_gas); 
                EditorPrefs.SetString($"{editorPrefsHead}_SheetName", m_sheetName);
                EditorPrefs.SetString($"{editorPrefsHead}_ReferenceTitle", m_referenceTitle);
            }
        }
#else
        private void EditorPrefsSave(){}
#endif

        public void Setup(System.Action<float> OnDownloadProgressChanged , System.Action<string> OnDownloadFinish )
        {
            m_OnDownloadProgressChanged = OnDownloadProgressChanged;
            m_OnDownloadFinish = OnDownloadFinish;
        }

        [Button("使用此組進行下載")]
        public void DownLoad()
        {
            if (m_OnDownloadFinish == null || m_OnDownloadProgressChanged == null)
            {
                Debug.LogError("資料未初始化");
                return;
            }
            m_googleSheetDownloader = new GoogleSheetDownloader(m_key, m_gid, m_OnDownloadProgressChanged , m_OnDownloadFinish);
        }

        public void Upload(string texts , System.Action<float> OnUploadProgressChanged, System.Action<string> OnUploadFinish)
        {
            m_googleSheetUploader = new GoogleSheetUploader(m_gas, m_key, m_gid, texts, m_sheetName, OnUploadProgressChanged, OnUploadFinish);
        }
    }
}