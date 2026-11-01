//Auto Create.

using GameCore.Buff;
using Sirenix.OdinInspector;

public static class OdinBuffConditionTypeEnumList
{
    public static ValueDropdownList<BuffConditionType> valueDropdownList = new ValueDropdownList<BuffConditionType>()
    {
			{"攻擊方屬性",GameCore.Buff.BuffConditionType.attackerAttribute},
			{"目標方屬性",GameCore.Buff.BuffConditionType.targetAttribute},
			{"距離",GameCore.Buff.BuffConditionType.distance},
			{"攻擊方狀態參考綴詞",GameCore.Buff.BuffConditionType.attackerBuffHastags},
			{"目標方狀態參考綴詞",GameCore.Buff.BuffConditionType.targetBuffHastags},

    };
}
