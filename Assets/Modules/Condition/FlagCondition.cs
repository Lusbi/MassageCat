using GameCore;
using GameCore.Database;
using System;
using UnityEngine;

[Serializable]
public class FlagCondition : ICondition
{
    [SerializeField] private FlagReference m_flagReference;
    [SerializeField] private OperationType m_operationType;
    [SerializeField] private int flagIndex;
    public bool Valid()
    {
        m_flagReference.TryLoad(out FlagData flagData);
        if (flagData == null)
            return false;

        return true;
    }
}
