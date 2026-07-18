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
    /// 
    /// </summary>
    private void Action_Main()
    {

    }

    /// <summary>
    /// ち传篈
    /// </summary>
    private void Action_Status()
    {

    }

    /// <summary>
    /// ち传杆称
    /// </summary>
    private void Action_Equipment()
    {

    }

    /// <summary>
    /// ち传м
    /// </summary>
    private void Action_Skill() 
    {
    }

    /// <summary>
    /// ち传坝┍
    /// </summary>
    private void Action_Shop() 
    {
    }

    /// <summary>
    /// ち传ヴ叭
    /// </summary>
    private void Action_Mission()
    {

    }

    /// <summary>
    /// ち传粿薄
    /// </summary>
    private void Action_Story() 
    { 
    }

    /// <summary>
    /// ち传匡兜
    /// </summary>
    private void Action_Options()
    {

    }
}
