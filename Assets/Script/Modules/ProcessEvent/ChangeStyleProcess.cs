using GameCore.Database;
using UnityEngine;

public class ChangeStyleProcess : IProcessEvent
{
    public ProcessType processType { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
    private string m_roleKey;
    public ChangeStyleProcess(string roleKey)
    {
        m_roleKey = roleKey;
    }
    public bool Execute()
    {
        if (Database<RoleData>.TryLoad(m_roleKey, out var roleData) == false)
        {
            return false;
        }
        // 發送事件，更換敵人演出
        processType = ProcessType.Processing;
        return true;
    }

    public void Inilization()
    {
        m_roleKey = string.Empty;
        processType = ProcessType.None;
    }
}
