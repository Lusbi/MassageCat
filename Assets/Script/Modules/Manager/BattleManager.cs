using System;

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
            GameInputGate.OnGameplayInput += OnGameplayInput;
        }
        else
        {
            GameInputGate.OnGameplayInput -= OnGameplayInput;
        }
        m_onHitEvent = onHitEvent;
    }

    private void OnGameplayInput(string input)
    {
        Hit(input);
    }

    private void Hit(string key = null)
    {
        m_onHitEvent?.Invoke(key);
    }

}
