using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace GameCore.Buff
{
    public enum BuffConditionType
    {
        [Description("攻擊方屬性")]
        attackerAttribute,
        [Description("目標方屬性")]
        targetAttribute,
        [Description("距離")]
        distance,
        [Description("攻擊方狀態參考綴詞")]
        attackerBuffHastags,
        [Description("目標方狀態參考綴詞")]
        targetBuffHastags,
    }
}