#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using GameCore.GoogleSheets;
using System;

[System.Serializable]
public class GoogleSheetsPackage
{
    public bool enable;
    public string title;
    public string url;

    public GoogleSheetsDownloader googleSheetsDownloader;

    public void Download(Action onComplete = null)
    {
        googleSheetsDownloader = new GoogleSheetsDownloader(url);
        googleSheetsDownloader.OnComplete(onComplete).Download();
    }
}

[CreateAssetMenu(fileName = "GoogleSheets_Reference" , menuName = "Editor Tool/雲端下載設定檔")]
public class GoogleSheetsSO : ScriptableObject
{
    public List<GoogleSheetsPackage> googleSheetsPackages = new List<GoogleSheetsPackage>();
    private int m_waiitCount = 0;

    public string Get(string key)
    {
        foreach (GoogleSheetsPackage googleSheetsPackage in googleSheetsPackages)
        {
            if (googleSheetsPackage.title.Equals(key))
            {
                return googleSheetsPackage.googleSheetsDownloader.text;
            }
        }
        return string.Empty;
    }

    public void Download()
    {
        m_waiitCount = googleSheetsPackages.Count;
        Debug.LogError($"此物件 {this.name} 總數量為：{m_waiitCount}");
        foreach (GoogleSheetsPackage googleSheetsPackage in googleSheetsPackages)
        {
            if (googleSheetsPackage == null)
            {
                m_waiitCount--;
                continue;
            }
            if (googleSheetsPackage.enable == false)
            {
                m_waiitCount--;
                continue;
            }
            googleSheetsPackage.Download(Finished);
        }
    }

    private void Finished()
    {
        m_waiitCount--;
        Debug.LogError($"此物件 {this.name} 剩餘下載數量：{m_waiitCount}");
    }
}

#endif