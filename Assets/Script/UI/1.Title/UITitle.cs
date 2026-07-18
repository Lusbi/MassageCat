using GameCore.UI;
using System;

public class UITitle : UIBehavior<UITitleView>
{
    protected override string addressableKey => "View_Title";

    public UITitle(System.Action action = null , params Enum[] enums) : base (action , enums)
    {
    }

    public override void OnUpdate(float time)
    {
        
    }
}
