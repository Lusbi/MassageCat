using UnityEngine;

public interface IProcessEvent
{
    ProcessType processType { get; set; }
    void Inilization();
    bool Execute();
}
