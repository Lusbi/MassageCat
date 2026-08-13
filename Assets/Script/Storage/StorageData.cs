using System;
using System.Collections.Generic;
using BigMath;
using GameCore.Database;
using GameCore.Event;
using GameCore.Log;

[System.Serializable]
public class StorageData
{
    [UnityEngine.SerializeField]
    private string m_currentSceneMap;
    [UnityEngine.SerializeField]
    private BattleStorageData m_battleStorageData = new BattleStorageData(string.Empty, 0);
    [UnityEngine.SerializeField]
    private List<FlagStorageData> m_flagStorageDatas = new List<FlagStorageData>();
    [UnityEngine.SerializeField]
    private List<EnemyStorageData> m_enemyStorageDatas = new List<EnemyStorageData>();

    // Property to get the current scene map
    public string CurrentSceneMap => m_currentSceneMap;
    public BattleStorageData BattleStorageData => m_battleStorageData;
    public IReadOnlyList<EnemyStorageData> EnemyStorageDatas => m_enemyStorageDatas;

    // API to set the current scene map
    public void SetCurrentSceneMap(string sceneMapKey)
    {
        m_currentSceneMap = sceneMapKey;
    }

    // API for FlagStorageData
    public List<FlagStorageData> GetFlagStorageDatas()
    {
        return new List<FlagStorageData>(m_flagStorageDatas);
    }

    public int GetFlagStorageValue(string key)
    {
        foreach (var data in m_flagStorageDatas)
        {
            if (data.FlagKey == key)
            {
                return data.Value;
            }
        }
        return 0; // Default value if not found
    }

    public void AddFlagStorageValue(string flagKey)
    {
        TutorialFlowDiagnostics.Log($"請求增加旗標：key={flagKey}");

        foreach (var data in m_flagStorageDatas)
        {
            if (data.FlagKey == flagKey)
            {
                int previousValue = data.Value;
                data.SetValue(data.Value + 1);
                TutorialFlowDiagnostics.Log(
                    $"旗標已存在，僅更新數值且不派送首次解鎖事件：key={flagKey}, previous={previousValue}, current={data.Value}");
                return;
            }
        }
        if (Database<FlagData>.Exists(flagKey) == false)
        {
            TutorialFlowDiagnostics.LogWarning($"旗標資料不存在，取消新增與事件派送：key={flagKey}");
            eLog.Log("FlagData not found: " + flagKey);
            return;
        }
        // If not found, add a new entry
        var newData = new FlagStorageData(flagKey, 1);
        m_flagStorageDatas.Add(newData);

        TutorialFlowDiagnostics.Log($"首次建立旗標並準備派送事件：key={flagKey}, current=1");
        EventManager.instance.Notify(new FlagUpdateEvent(flagKey));
        TutorialFlowDiagnostics.Log($"旗標事件派送完成：key={flagKey}");
    }

    // API for EnemyStorageData
    public List<EnemyStorageData> GetEnemyStorageDatas()
    {
        return new List<EnemyStorageData>(m_enemyStorageDatas);
    }

    public EnemyStorageData GetEnemyStorageData(string enemyKey)
    {
        foreach (var data in m_enemyStorageDatas)
        {
            if (data.EnemyKey == enemyKey)
            {
                return data;
            }
        }
        return null; // Default value if not found
    }

    public void AddEnemySatisfyCount(string enemyKey, BattleModel battleModel = BattleModel.Normal)
    {
        foreach (var data in m_enemyStorageDatas)
        {
            if (data.EnemyKey == enemyKey)
            {
                data.SetSatisfyValue(battleModel);
                return;
            }
        }
        if (Database<RoleData>.Exists(enemyKey) == false)
            return;
        // If not found, add a new entry
        m_enemyStorageDatas.Add(new EnemyStorageData(enemyKey, battleModel));
    }

    public void GetEnemySatisfyCount(string enemyKey, out int satisfyCount)
    {
        foreach (var data in m_enemyStorageDatas)
        {
            if (data.EnemyKey == enemyKey)
            {
                satisfyCount = data.SatisfyValue;
                return;
            }
        }
        satisfyCount = 0; // Default value if not found
    }

    public void ApplyBattleProgressData(string enemyKey, BigNumber accumulatedHealValue)
    {
        m_battleStorageData = new BattleStorageData(enemyKey, accumulatedHealValue);
    }

    internal void EnsureInitialized()
    {
        // 舊版空存檔不含欄位，載入後需補齊容器，避免後續流程發生 NullReferenceException。
        if (m_battleStorageData == null)
            m_battleStorageData = new BattleStorageData(string.Empty, 0);
        if (m_flagStorageDatas == null)
            m_flagStorageDatas = new List<FlagStorageData>();
        if (m_enemyStorageDatas == null)
            m_enemyStorageDatas = new List<EnemyStorageData>();
    }

    internal void AddCardStorageData(string key)
    {
        StorageManager.instance.StorageData.GetEnemyStorageData(key)?.GetCard();
    }
}


[System.Serializable]
public class FlagStorageData
{
    [UnityEngine.SerializeField]
    private string m_flagKey;
    [UnityEngine.SerializeField]
    private int m_value;

    public string FlagKey => m_flagKey;
    public int Value => m_value;
    public FlagStorageData(string flagKey, int value)
    {
        m_flagKey = flagKey;
        m_value = value;
    }
    public void SetValue(int value)
    {
        m_value = value;
    }
}

[System.Serializable]
public class EnemyStorageData
{
    [UnityEngine.SerializeField]
    private string m_enemyKey;
    [UnityEngine.SerializeField]
    private int m_satisfyValue;
    [UnityEngine.SerializeField]
    private bool m_silverClear;
    [UnityEngine.SerializeField]
    private bool m_goldenClear;
    [UnityEngine.SerializeField]
    private bool m_sppedClear = false;
    [UnityEngine.SerializeField]
    private bool m_cardReward = false;

    public string EnemyKey => m_enemyKey;
    public int SatisfyValue => m_satisfyValue;
    public bool silverClear => m_silverClear;
    public bool goldenClear => m_goldenClear;
    public bool speedClear => m_sppedClear;
    public bool CardReward => m_cardReward;
    public EnemyStorageData(string enemyKey, BattleModel battleModel)
    {
        m_enemyKey = enemyKey;
        SetSatisfyValue(battleModel);
    }
    public void SetSatisfyValue(BattleModel battleModel)
    {
        switch (battleModel)
        {
            case BattleModel.Normal:
                m_silverClear = true;
                break;
            case BattleModel.Tomato:
                m_goldenClear = true;
                break;
            case BattleModel.Spell:
                m_sppedClear = true;
                break;
        }
        m_satisfyValue++;
    }
    public void GetCard()
    {
        m_cardReward = true;
    }

    /// <summary>
    /// 移除卡片獎勵（開發者工具專用）
    /// </summary>
    public void RemoveCard()
    {
        m_cardReward = false;
    }
}

[System.Serializable]
public class ToyStorageData
{
    [UnityEngine.SerializeField]
    private string m_toyKey;
    [UnityEngine.SerializeField]
    private int m_useableValue;

    public string ToyKey => m_toyKey;
    public int UseableValue => m_useableValue;
    public ToyStorageData(string toyKey, int useableValue)
    {
        m_toyKey = toyKey;
        m_useableValue = useableValue;
    }
    public void SetUseableValue(int useableValue)
    {
        m_useableValue = useableValue;
    }
}

[System.Serializable]
public class ItemStorageData
{
    [UnityEngine.SerializeField]
    private string m_itemKey;
    [UnityEngine.SerializeField]
    private int m_itemValue;

    public string ItemKey => m_itemKey;
    public int ItemValue => m_itemValue;
    public ItemStorageData(string itemKey, int itemValue)
    {
        m_itemKey = itemKey;
        m_itemValue = itemValue;
    }
    public void SetItemValue(int itemValue)
    {
        m_itemValue = itemValue;
    }
}

[System.Serializable]
public class BattleStorageData
{
    [UnityEngine.SerializeField]
    private string m_enemyKey;
    private BigNumber m_accumulatedHealValue;

    public string EnemyKey => m_enemyKey;
    public BigNumber AccumulatedHealValue => m_accumulatedHealValue;
    public BattleStorageData(string enemyKey, BigNumber accumulatedHealValue)
    {
        m_enemyKey = enemyKey;
        m_accumulatedHealValue = accumulatedHealValue;
    }
    public void SetAccumulatedHealValue(BigNumber accumulatedHealValue)
    {
        m_accumulatedHealValue = accumulatedHealValue;
    }
}
