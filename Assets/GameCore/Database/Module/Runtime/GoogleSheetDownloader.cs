using System;
using System.Collections;
using UnityEngine.Networking;
using System.Diagnostics;

#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
#else
//using JSLCore.Coroutine;
#endif

namespace GameCore.Database
{
    public class GoogleSheetDownloader
    {
        private const string URL_FORMAT = "https://docs.google.com/spreadsheet/ccc?key={0}&gid={1}&usp=sharing&output=csv";
        private const string LOG_DOWNLOAD = "Download csv from url {0} cost {1} seconds.";

        private string m_url;
        private Action<float> m_onProgressChanged;
        private Action<string> m_onDownloadFinish;
        private Stopwatch m_stopwatch;

        public GoogleSheetDownloader(string key, string gid, Action<float> onProgressChanged, Action<string> onDownloadFinish)
        {
            m_url = string.Format(URL_FORMAT, key, gid);
            m_onProgressChanged = onProgressChanged;
            m_onDownloadFinish = onDownloadFinish;

#if UNITY_EDITOR
            m_stopwatch = new Stopwatch();
            m_stopwatch.Start();

            EditorCoroutineUtility.StartCoroutine(Downloading(), this);
#else
            //CoroutineManager.Instance.StartCoroutine(Downloading());
#endif
            
        }

        private IEnumerator Downloading()
        {
            UnityWebRequest unityWebRequest = UnityWebRequest.Get(m_url);
            unityWebRequest.SendWebRequest();

            while (!unityWebRequest.isDone)
            {
                m_onProgressChanged?.Invoke(unityWebRequest.downloadProgress);
                yield return null;
            }

#if UNITY_EDITOR
            m_stopwatch.Stop();
            UnityEngine.Debug.Log(string.Format( LOG_DOWNLOAD, m_url, m_stopwatch.Elapsed.Seconds));
#endif

            if (string.IsNullOrEmpty(unityWebRequest.error))
            {
                m_onDownloadFinish?.Invoke(unityWebRequest.downloadHandler.text);
            }
            else
            {
                m_onDownloadFinish?.Invoke(string.Empty);
#if UNITY_EDITOR
                UnityEngine.Debug.LogError(unityWebRequest.error);
#endif
            }
        }
    }
}