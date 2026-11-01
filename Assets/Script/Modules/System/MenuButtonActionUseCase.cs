using UnityEngine;

public class MenuButtonActionUseCase
{
    public void Execute(MenuActionType menuActionType)
    {
        switch (menuActionType)
        {
            case MenuActionType.Main: 
                Action_Main();
                break;
            case MenuActionType.Status: 
                Action_Status();
                break;
            case MenuActionType.Equipment: 
                Action_Equipment();
                break;
            case MenuActionType.Skill:
                Action_Skill();
                break;
            case MenuActionType.Shop: 
                Action_Shop();
                break;
            case MenuActionType.Mission: 
                Action_Mission();
                break;
            case MenuActionType.Story: 
                Action_Story();
                break;
            case MenuActionType.Options: 
                Action_Options();
                break;
        }
    }

    /// <summary>
    /// 返回主頁面
    /// </summary>
    private void Action_Main()
    {

    }

    /// <summary>
    /// 切換狀態頁
    /// </summary>
    private void Action_Status()
    {

    }

    /// <summary>
    /// 切換裝備頁
    /// </summary>
    private void Action_Equipment()
    {

    }

    /// <summary>
    /// 切換技能頁
    /// </summary>
    private void Action_Skill() 
    {
    }

    /// <summary>
    /// 切換商店頁
    /// </summary>
    private void Action_Shop() 
    {
    }

    /// <summary>
    /// 切換任務頁
    /// </summary>
    private void Action_Mission()
    {

    }

    /// <summary>
    /// 切換劇情頁
    /// </summary>
    private void Action_Story() 
    { 
    }

    /// <summary>
    /// 切換選項頁
    /// </summary>
    private void Action_Options()
    {

    }
}
