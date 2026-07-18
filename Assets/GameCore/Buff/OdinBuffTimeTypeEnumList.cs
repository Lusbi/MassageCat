//Auto Create.

using GameCore.Buff;
using Sirenix.OdinInspector;

public static class OdinBuffTimeTypeEnumList
{
    public static ValueDropdownList<BuffTimeType> valueDropdownList = new ValueDropdownList<BuffTimeType>()
    {
			{"立即性",BuffTimeType.Immediately},
			{"持續性",BuffTimeType.Interval},
			{"常駐型",BuffTimeType.Permanent},
    };
}
