using UnityEngine;

public class BattleCommand : CommandBase, ICommand
{
    public override CommandType CommandType => CommandType.Battle;

    public override void Execute()
    {
        Debug.Log("Executing Battle Command");
        // 在這裡添加 Battle 指令的具體邏輯
    }
}
