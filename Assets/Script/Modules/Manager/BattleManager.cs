using System;
using UnityEngine.EventSystems;

public class BattleManager : GameCore.Singleton<BattleManager>
{
    private Action<string> m_onHitEvent;

    public void Register(Action<string> onHitEvent)
    {
        if (onHitEvent == m_onHitEvent)
        {
            return;
        }

        if (onHitEvent != null)
        {
            WindowsInputHookManager.OnGlobalKeyDown += OnGlobalKeyDown;
            WindowsInputHookManager.OnGlobalMouseDown += OnGlobalMouseDown;
        }
        else
        {
            WindowsInputHookManager.OnGlobalKeyDown -= OnGlobalKeyDown;
            WindowsInputHookManager.OnGlobalMouseDown -= OnGlobalMouseDown;
        }
        m_onHitEvent = onHitEvent;
    }
    private void OnGlobalMouseDown(int obj)
    {
        if (DesktopPetWindowController.IsPointerInRuntimeControlArea())
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Hit();
    }

    private void OnGlobalKeyDown(int virtualKey)
    {
        if (virtualKey is >= 0x41 and <= 0x5A)
        {
            Hit(char.ToLowerInvariant((char)virtualKey).ToString());
            return;
        }

        Hit();
    }

    private void Hit(string key = null)
    {
        m_onHitEvent?.Invoke(key);
    }

}
