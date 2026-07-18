using System;
using System.Collections.Generic;
using GameCore.Database;
using Sirenix.OdinInspector;
using UnityEngine;

public enum TalkType
{
    [LabelText("首次遭遇")]
    FirstEncounter,
    [LabelText("按摩期間")]
    DuringMassage,
    [LabelText("疲勞度低於 50")]
    FatigueLessThan50,
    [LabelText("特殊事件")]
    SpecialEvent,
    [LabelText("疲勞度低於 10")]
    FatigueLessThan10,
    [LabelText("推薦")]
    Recommended,
    [LabelText("戰鬥超過 30 秒")]
    BattleOver30Seconds,
    [LabelText("服務完成")]
    ServiceComplete,
    [LabelText("番茄鐘模式專屬")]
    TomatoModeExclusive,
    [LabelText("要求更快按摩")]
    RequestFasterMassage
}

[CreateAssetMenu(fileName = "TalkScriptableObject", menuName = "Scriptable Objects/Talk Scriptable Object")]
public class TalkScriptableObject : ScriptableObject
{
    [SerializeField, LabelText("對話群組")]
    private TalkGroup[] m_groups = Array.Empty<TalkGroup>();

    public IReadOnlyList<TalkGroup> Groups => m_groups;

#if UNITY_EDITOR
    public void Editor_SetGroups(IReadOnlyList<TalkGroup> groups)
    {
        if (groups == null || groups.Count == 0)
        {
            m_groups = Array.Empty<TalkGroup>();
            return;
        }

        var array = new TalkGroup[groups.Count];
        for (int i = 0; i < groups.Count; i++)
        {
            array[i] = groups[i];
        }

        m_groups = array;
    }
#endif
}

[Serializable]
public struct TalkGroup
{
    [SerializeField, LabelText("對話類型")]
    private TalkType m_talkType;

    [SerializeField, LabelText("觸發條件")]
    private TalkCondition m_condition;

    [SerializeField, LabelText("對話列表")]
    private TalkEntry[] m_entries;

    public TalkType TalkType => m_talkType;
    public TalkCondition Condition => m_condition;
    public IReadOnlyList<TalkEntry> Entries => m_entries ?? Array.Empty<TalkEntry>();

    public TalkGroup(TalkType talkType, TalkCondition condition, IReadOnlyList<TalkEntry> entries)
    {
        m_talkType = talkType;
        m_condition = condition;

        if (entries == null || entries.Count == 0)
        {
            m_entries = Array.Empty<TalkEntry>();
        }
        else
        {
            m_entries = new TalkEntry[entries.Count];
            for (int i = 0; i < entries.Count; i++)
            {
                m_entries[i] = entries[i];
            }
        }
    }
}

[Serializable, HideInInspector]
public struct TalkEntry
{
    [SerializeField, TextArea, LabelText("對話內容")]
    private string m_content;

    [SerializeField, Min(0f), LabelText("持續時間(秒)")]
    private float m_duration;

    public string Content => m_content;

    public float Duration => m_duration;

    public TalkEntry(string content, float duration)
    {
        m_content = content;
        m_duration = duration;
    }
}

[Serializable]
public struct TalkCondition
{
    [SerializeField, Min(0), LabelText("剩餘次數條件")]
    private int m_remainingCount;

    [SerializeField, Min(0), LabelText("滿足次數條件")]
    private int m_satisfyCount;

    [SerializeField, LabelText("旗標參考")]
    private FlagReference m_flagReference;

    public int RemainingCount => m_remainingCount;

    public int SatisfyCount => m_satisfyCount;

    public FlagReference FlagReference => m_flagReference;

    public TalkCondition(int remainingCount, int satisfyCount, FlagReference flagReference)
    {
        m_remainingCount = remainingCount;
        m_satisfyCount = satisfyCount;
        m_flagReference = flagReference;
    }
}
