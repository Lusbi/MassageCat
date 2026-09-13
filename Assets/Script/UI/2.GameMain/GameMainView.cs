using System;
using GameCore.Database;
using GameCore.UI;
using UnityEngine;
using UnityEngine.UI;

public class GameMainView : UIViewBase
{
    [SerializeField] private BattlePanel m_battlePanel;
    [SerializeField] private RoleScrollRect m_roleScrollRect;
    [SerializeField] private CanvasScaler m_canvasScaler;

    private PanelBase m_curPanel;
    private bool m_galleryVisible;

    public bool isBattlePanel => m_curPanel is BattlePanel;

#if UNITY_EDITOR
    protected override void CacheUIViews()
    {
        base.CacheUIViews();

        if (m_battlePanel == null)
            m_battlePanel = GetComponentInChildren<BattlePanel>();
        if (m_canvasScaler == null)
            m_canvasScaler = GetComponent<CanvasScaler>();
    }
#endif


    public override void Initlization(Action callBack = null)
    {
        base.Initlization(callBack);

        m_battlePanel.Initlization();
        m_battlePanel.Apply(this);

        if (GameProcess.Instance != null && GameProcess.Instance.DesktopPetMode)
        {
            ApplyDesktopPetMode();
        }
    }
    public override void Active(bool isActive)
    {
        base.Active(isActive);

        if (isActive)
        {
            ActionCommand(CommandType.Battle);

        }
    }

    internal void ActionCommand(CommandType commandType)
    {
        switch (commandType)
        {
            case CommandType.Battle:
                Debug.Log("Massage Command Executed");
                m_curPanel?.Active(false);
                m_battlePanel.Active(true);
                m_curPanel = m_battlePanel;
                break;
            default:
                Debug.Log("Unknown Command Executed");
                break;
        }
    }

    private void Update()
    {
        if (m_battlePanel)
            m_battlePanel.Tick(Time.deltaTime);
    }

    public void ApplyRoleRect()
    {
        m_roleScrollRect.ListUpdate();
    }

    public void ApplyRoleDataToListTop(RoleData data)
    {
        m_roleScrollRect.ListMoveToTop(data);
    }

    internal void SetRoleArrowInteraction(bool isEnabled)
    {
        m_roleScrollRect.SetArrowInteractionEnabled(isEnabled);
    }

    /// <summary>
    /// 切換桌寵圖鑑的覆蓋畫面；圖鑑開啟時，主遊戲介面不繪製也不接收 UI 射線。
    /// </summary>
    internal void SetGalleryVisible(bool isVisible)
    {
        if (m_galleryVisible == isVisible)
            return;

        m_galleryVisible = isVisible;
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            return;

        canvasGroup.alpha = isVisible ? 0f : 1f;
        canvasGroup.interactable = isVisible == false;
        canvasGroup.blocksRaycasts = isVisible == false;
    }

    private void ApplyDesktopPetMode()
    {
        if (m_battlePanel)
            m_battlePanel.ApplyDesktopPetMode();
    }
}
