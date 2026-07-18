using GameCore.UI;
using UnityEngine;

public static class ButtonUtils
{
    public static void SetOnClick(this UIButton uIButton , System.Action<UIButton> clickAction)
    {
        if (uIButton.onClicked != null)
        {
            Debug.LogWarning($"{uIButton.gameObject.name} OnClick 事件重複註冊。");
            return;
        }
        uIButton.onClicked = clickAction;
    }
    
    public static void RemoveOnClick(this UIButton uIButton )
    {
        uIButton.onClicked = null;
    }

    public static void SetOnHover(this UIButton uIButton , System.Action<UIButton , bool> hoverAction)
    {
        if (uIButton.onHover != null)
        {
            Debug.LogWarning($"{uIButton.gameObject.name} OnHover 事件重複註冊。");
            return;
        }
        uIButton.onHover = hoverAction;
    }

    public static void RemoveOnHover(this UIButton uIButton)
    {
        uIButton.onHover = null;
    }

    public static void RemoveAllEvent(this UIButton uIButton)
    {
        uIButton.onClickDown = null;
        uIButton.onClicked = null;
        uIButton.onClickUp = null;
        uIButton.onHover = null;
        uIButton.onRightClicked = null;
    }
}
