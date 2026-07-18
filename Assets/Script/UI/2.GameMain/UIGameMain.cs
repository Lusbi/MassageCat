using GameCore.UI;
using System;

public class UIGameMain : UIBehavior<GameMainView>
{
    protected override string addressableKey =>"GameMain";
    public UIGameMain(System.Action action = null , params Enum[] enums) : base (action , enums)
    {
    }

    public override void OnUpdate(float time)
    {
        
    }
}
