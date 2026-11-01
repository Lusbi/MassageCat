namespace GameCore.Database
{
    [System.Flags]
    public enum CardFormulaInfoType
    {
        BaseModel = 1 << 0,
        SpellModel = 1 << 1,
        TomatoModel = 1 << 2,
        BaseMob = 1 << 3,
        MiniBossMob = 1 << 4,
        BossMob = 1 << 5,
        BaseMobCardDropRate = 1 << 6,
        MiniBossMobCardDropRate = 1 << 7,
        BossMobCardDropRate = 1 << 8,
    }

    [System.Serializable]
    public class RoleCardInfo
    {
        public CardFormulaInfoType cardFormulaInfoType;
        public float bonusValue;

        public string GetLocalization()
        {
            return string.Format(GetFormatLocalization(), bonusValue);
        }

        private string GetFormatLocalization() => cardFormulaInfoType switch
        {
            CardFormulaInfoType.BaseModel => LocalizationManager.instance.GetLocalization("卡片能力.一般模式加成"),
            CardFormulaInfoType.SpellModel => LocalizationManager.instance.GetLocalization("卡片能力.拼字模式加成"),
            CardFormulaInfoType.TomatoModel => LocalizationManager.instance.GetLocalization("卡片能力.蕃茄鐘模式加成"),
            CardFormulaInfoType.BaseMob => LocalizationManager.instance.GetLocalization("卡片能力.一般敵人加成"),
            CardFormulaInfoType.MiniBossMob => LocalizationManager.instance.GetLocalization("卡片能力.精英敵人加成"),
            CardFormulaInfoType.BossMob => LocalizationManager.instance.GetLocalization("卡片能力.魔王敵人加成"),
            CardFormulaInfoType.BaseMobCardDropRate => LocalizationManager.instance.GetLocalization("卡片能力.一般敵人卡片掉落率加成"),
            CardFormulaInfoType.MiniBossMobCardDropRate => LocalizationManager.instance.GetLocalization("卡片能力.精英敵人卡片掉落率加成"),
            CardFormulaInfoType.BossMobCardDropRate => LocalizationManager.instance.GetLocalization("卡片能力.魔王敵人卡片掉落率加成"),
            _ => string.Empty,
        };
    }
}