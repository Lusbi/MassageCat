using System.Collections.Generic;

namespace GameCore.Buff
{
    public class BuffUtility
    {
        private static Dictionary<BuffActionType, IBuffAction> m_buffActions = new Dictionary<BuffActionType, IBuffAction>();

        public static void Execute(BuffActionType buffActionType , object parameters)
        {
            if (m_buffActions.ContainsKey(buffActionType))
            {
                m_buffActions[buffActionType].Execute(parameters);
            }
        }

        private static Dictionary<BuffConditionType, IBuffCondition> m_buffConditions = new Dictionary<BuffConditionType, IBuffCondition>();

        public static bool Valid(BuffConditionType buffConditionType , object parameters , bool defaultValue = false)
        {
            if (m_buffConditions.ContainsKey(buffConditionType))
            {
                return m_buffConditions[buffConditionType].Valid(parameters);
            }
            return defaultValue;
        }
    }
}