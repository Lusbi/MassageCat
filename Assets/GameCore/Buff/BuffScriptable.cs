using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace GameCore.Buff
{
    [CreateAssetMenu(menuName ="通用/狀態設定檔")]
    public class BuffScriptable : ScriptableObject
    {
        [Title("持續時間類型") , HideLabel , EnumToggleButtons]
        public BuffTimeType buffTimeType;

        [LabelText("持續時間") , ShowIf("buffTimeType" , BuffTimeType.Interval)]
        public float durationTime = 0;
        [LabelText("間隔觸發時間"), ShowIf("buffTimeType", BuffTimeType.Interval)]
        public float intervalTime = 0;
        [LabelText("堆疊次數")]
        public int stack = 1;

        [Space(10)]
        // 前置條件
        [Title("前置條件"), HideLabel, ValueDropdown("@OdinBuffConditionTypeEnumList.valueDropdownList")]
        public BuffConditionType buffConditionType;

        // 執行功能
        [Space(10)]
        [Title("執行功能") , HideLabel , ValueDropdown("@OdinBuffActionTypeEnumList.valueDropdownList")]
        public BuffActionType buffActionType;
    }
}