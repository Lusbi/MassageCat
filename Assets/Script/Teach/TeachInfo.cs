using System.Collections.Generic;
using GameCore.Database;
using UnityEngine;

[System.Serializable]
public class TeachInfo
{
    [SerializeField] private FlagReference m_flagRef;
    [SerializeField] private TeachStepInfo[] m_teachSteps;

    public IReadOnlyList<TeachStepInfo> TeachSteps => m_teachSteps;

    public bool Valid(string teachKey)
    {
        if (m_flagRef != null && m_flagRef.GetKey() == teachKey)
        {
            int value = StorageManager.instance.StorageData.GetFlagStorageValue(m_flagRef.GetKey());

            return value == 1;
        }
        return false;
    }

}
