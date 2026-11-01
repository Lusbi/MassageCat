using GameCore.UI;
using UnityEngine;

public class PanelBase : UIViewBase
{
    protected GameMainView uiGameMainView;
    public void Apply(GameMainView uiGameMainView)
    {
        this.uiGameMainView = uiGameMainView;
    }

    public override void Active(bool isActive)
    {
        if (isActive)
            ActiveOn();
        else
            ActiveOff();

        base.Active(isActive);

        canvasGroup.blocksRaycasts = false;
    }

    public virtual void ActiveOn()
    {
        
    }

    public virtual void ActiveOff()
    {
        
    }

    public virtual void Tick (float deltaTime)
    {

    }
}
