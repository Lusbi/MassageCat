using System.Collections.Generic;
using GameCore.Database;

public class RoleTalkHandler
{
    private const float k_initialDelayMin = 3f;
    private const float k_initialDelayMax = 6f;
    private const float k_talkIntervalMin = 7f;
    private const float k_talkIntervalMax = 12f;
    private const float k_noValidTalkRetryInterval = 10f;
    private const float k_minimumTalkDisplayDuration = 8f;

    private RoleData m_roleData;
    private float m_durationTime;
    private string m_lastTalkContent;
    private readonly List<TalkEntry> m_validEntries = new List<TalkEntry>(12);

    public void Apply(RoleData roleData)
    {
        m_roleData = roleData;
        m_durationTime = UnityEngine.Random.Range(k_initialDelayMin, k_initialDelayMax);
        m_lastTalkContent = null;

        TalkManager.instance.Clear();
    }

    public void Tick(float deltaTime)
    {
        if (m_roleData == null)
            return;

        m_durationTime -= deltaTime;
        if (m_durationTime <= 0f)
            TriggerTalk(TalkType.DuringMassage);
    }

    public void TriggerTalk(TalkType talkType)
    {
        if (m_roleData?.TalkScriptableObject == null)
        {
            m_durationTime = float.PositiveInfinity;
            return;
        }

        TalkEntry? talk = GetRandomTalkWithCondition(talkType);
        if (talk.HasValue == false)
        {
            // 沒有符合條件的台詞時延後重試，避免每幀重新掃描資料。
            m_durationTime = k_noValidTalkRetryInterval;
            return;
        }

        TalkEntry talkEntry = talk.Value;
        m_lastTalkContent = talkEntry.Content;
        float displayDuration = GetTalkDisplayDuration(talkEntry.Duration);
        m_durationTime = displayDuration + UnityEngine.Random.Range(k_talkIntervalMin, k_talkIntervalMax);
        string content = LocalizationManager.instance.GetLocalization(talkEntry.Content);
        TalkManager.instance.Clear();
        TalkManager.instance.Play(content, displayDuration);
    }

    private static float GetTalkDisplayDuration(float configuredDuration)
    {
        return UnityEngine.Mathf.Max(configuredDuration, k_minimumTalkDisplayDuration);
    }

    private TalkEntry? GetRandomTalkWithCondition(TalkType talkType)
    {
        if (m_roleData.TalkScriptableObject.Groups == null)
            return null;

        m_validEntries.Clear();

        IReadOnlyList<TalkGroup> groups = m_roleData.TalkScriptableObject.Groups;
        for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
        {
            TalkGroup group = groups[groupIndex];
            if (group.TalkType == talkType && ConditionValid(group.Condition))
            {
                IReadOnlyList<TalkEntry> entries = group.Entries;
                for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
                    m_validEntries.Add(entries[entryIndex]);
            }
        }

        if (m_validEntries.Count == 0)
            return null;

        int index = GetRandomNonRepeatingIndex();
        return m_validEntries[index];
    }

    private int GetRandomNonRepeatingIndex()
    {
        int entryCount = m_validEntries.Count;
        if (entryCount <= 1 || string.IsNullOrEmpty(m_lastTalkContent))
            return UnityEngine.Random.Range(0, entryCount);

        int lastIndex = -1;
        for (int i = 0; i < entryCount; i++)
        {
            if (string.Equals(m_validEntries[i].Content, m_lastTalkContent, System.StringComparison.Ordinal))
            {
                lastIndex = i;
                break;
            }
        }

        if (lastIndex < 0)
            return UnityEngine.Random.Range(0, entryCount);

        int index = UnityEngine.Random.Range(0, entryCount - 1);
        return index >= lastIndex ? index + 1 : index;
    }

    private bool ConditionValid(TalkCondition condition)
    {
        bool result = true;

        if (condition.FlagReference != null && condition.FlagReference.Exists())
        {
            result &= StorageManager.instance.StorageData.GetFlagStorageValue(condition.FlagReference.GetKey()) > 0;
        }

        EnemyStorageData enemyData = StorageManager.instance.StorageData.GetEnemyStorageData(m_roleData.key);
        if (enemyData == null)
        {
            if (condition.SatisfyCount > 0)
            {
                return false;
            }
        }
        else
        {
            result &= enemyData.SatisfyValue >= condition.SatisfyCount;
        }

        return result;
    }
}
