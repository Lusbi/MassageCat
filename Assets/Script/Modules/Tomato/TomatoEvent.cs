using GameCore.Event;

public struct FlagUpdateEvent : IEventBase
{
    public string flagKey;
    public FlagUpdateEvent(string flagKey) => this.flagKey = flagKey;
}