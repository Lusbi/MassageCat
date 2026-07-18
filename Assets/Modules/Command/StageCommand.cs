using UnityEngine;

public class StageCommand : CommandBase, ICommand
{
    public override CommandType CommandType => CommandType.Stage;

    public override void Execute()
    {
        Debug.Log("Executing Stage Command");
        // 在這裡添加 Stage 指令的具體邏輯
    }
}
