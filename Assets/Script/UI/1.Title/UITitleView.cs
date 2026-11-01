using GameCore.Audio;
using GameCore.UI;
using UnityEngine;

public class UITitleView : UIViewBase
{
    [SerializeField] private UIButton m_btn;

    public override void Initlization(System.Action callBack = null)
    {
        base.Initlization(callBack);
    }

    public override void ViewEnable()
    {
        base.ViewEnable();

        if (m_btn.onClicked is null)
            m_btn.SetOnClick(EnterGame);

        AudioClipManager.instance.PlayBGM(BgmClipType.MainTheme, 1.0f);
    }

    public override void ViewDisable()
    {
        base.ViewDisable();
    }

    private void EnterGame(UIButton obj)
    {
        UIManager.instance.ChangeView(UIEnum.GameMain);

        AudioClipManager.instance.StopBGM(0.2f);
    }
}
