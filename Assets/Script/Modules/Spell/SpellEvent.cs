using GameCore.Event;

public class SpellEvent : IEventBase
{
    public bool IsEnabled { get; private set; }
    public SpellEvent(bool isEnabled)
    {
        IsEnabled = isEnabled;
    }
}
