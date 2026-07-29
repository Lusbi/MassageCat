using System.Collections.Generic;
using GameCore.Database;

public class RoleTalkHandler
{
    private const float k_talkInterval = 5f;

    private RoleData m_roleData;
    private float m_durationTime;
    private readonly List<TalkEntry> m_validEntries = new List<TalkEntry>();

    public void Apply(RoleData roleData)
    {
        m_roleData = roleData;
        m_durationTime = 0f;

        TalkManager.instance.Clear();
    }

    public void Tick(float deltaTime)
    {
        if (m_roleData == null)
            return;

        m_durationTime -= deltaTime;
        if (m_durationTime < 0f)
        {
            TriggerTalk(TalkType.DuringMassage);
        }
    }

    public void TriggerTalk(TalkType talkType)
    {
        if (m_roleData?.TalkScriptableObject == null)
            return;

        TalkManager.instance.Clear();

        TalkEntry? talk = GetRandomTalkWithCondition(talkType);

        if (talk.HasValue)
        {
            // 台詞播完後保留空檔，避免角色文字無縫常駐畫面。
            m_durationTime = talk.Value.Duration + k_talkInterval;
            string content = LocalizationManager.instance.GetLocalization(talk.Value.Content);
            TalkManager.instance.Play(content, talk.Value.Duration);
        }
    }

    private TalkEntry? GetRandomTalkWithCondition(TalkType talkType)
    {
        if (m_roleData.TalkScriptableObject.Groups == null)
            return null;

        m_validEntries.Clear();

        foreach (var group in m_roleData.TalkScriptableObject.Groups)
        {
            if (group.TalkType == talkType && ConditionValid(group.Condition))
            {
                if (group.Entries != null)
                {
                    m_validEntries.AddRange(group.Entries);
                }
            }
        }

        if (m_validEntries.Count == 0)
            return null;

        int index = UnityEngine.Random.Range(0, m_validEntries.Count);
        return m_validEntries[index];
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
