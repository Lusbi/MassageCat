using GameCore.Event;

/// <summary>
/// 設定戰鬥角色
/// </summary>
public struct SetBattleRoleEvent : IEventBase
{
    public string roleKey;
    public SetBattleRoleEvent(string roleKey) => this.roleKey = roleKey;
}

/// <summary>
/// 蕃茄鐘開始
/// </summary>
public struct TomatoTriggerEvent : IEventBase 
{
    public bool enable;
    public TomatoTriggerEvent(bool enable) => this.enable = enable;
}