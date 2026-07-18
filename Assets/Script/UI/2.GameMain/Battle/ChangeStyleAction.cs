using UnityEngine;

public class ChangeStyleAction : IDieAction
{
    public string ChangeModKey { get; private set; }
    public ChangeStyleAction(string changeModKey)
    {
        ChangeModKey = changeModKey;
    }
    public void Execute()
    {
        // ¼Ä¤HÅÜ¨­
    }
}
