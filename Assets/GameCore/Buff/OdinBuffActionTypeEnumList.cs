//Auto Create.

using GameCore.Buff;
using Sirenix.OdinInspector;

public static class OdinBuffActionTypeEnumList
{
    public static ValueDropdownList<BuffActionType> valueDropdownList = new ValueDropdownList<BuffActionType>()
    {
			{"一般屬性",GameCore.Buff.BuffActionType.attributes},

    };
}
