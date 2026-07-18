using GameCore;
using System;
using UnityEngine.Localization.Settings;
using GameCore.Log;
using UnityEngine.Localization.Tables;
using System.Threading.Tasks;

public class LocalizationManager : Singleton<LocalizationManager>, IInitlization
{
    private const string TABLE_NAME = "LocalizationCollection";
    private StringTable m_localizationStringTable;
    private Action m_initlizationCallBack;

    public async void Initlization(Action callBack = null)
    {
        m_initlizationCallBack = callBack;
        await Load();
    }

    private async Task Load()
    {
        m_localizationStringTable = null;
        var localizationOperation = LocalizationSettings.StringDatabase.GetTableAsync(TABLE_NAME);
        await localizationOperation.Task;

        if (localizationOperation.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Failed)
        {
            eLog.Error($"{GetType().ToString()}載入多國語系失敗");
            m_initlizationCallBack?.Invoke();
            return;
        }

        m_localizationStringTable = localizationOperation.Result;
        m_initlizationCallBack?.Invoke();
    }

    /// <summary>
    /// 取多國值
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public string GetLocalization(string key)
    {
        if (m_localizationStringTable == null)
        {
            return "多國語系載入失敗，無法讀取。";
        }

        string result = m_localizationStringTable.GetEntry(key)?.GetLocalizedString();
        return string.IsNullOrEmpty(result) ? key : result;
    }

}
