using System.Collections.Generic;
using BigMath;
using CsvHelper;
using GameCore.Database;

public static class Formula
{
    private static GameDefaultSetting gameDefaultSetting;
    private static Dictionary<string, RoleData>.ValueCollection roleDatas ;
    private static void Initlization()
    {
        if (gameDefaultSetting)
            return;

        gameDefaultSetting = SettingsManager.instance.setting;
        roleDatas = Database<RoleData>.GetAll();
    }

    public static BigNumber GetBonusHealValue(BigNumber baseHeal, RoleData roleData, BattleModel battleModel)
    {
        Initlization();

        BigNumber bigNumber = baseHeal;
        int bonusHeal = 0;
        // ?∠????瑕拿
        
        foreach (var role in roleDatas)
        {
            var storageData = StorageManager.instance.StorageData.GetEnemyStorageData(role.key);
            if (storageData == null)
            {
                continue;
            }
            if ( storageData.CardReward == false)
            {
                continue;
            }
            if (Database<RoleData>.TryLoad(storageData.EnemyKey, out var rd) == false)
            {
                continue;
            }
            switch (roleData.EnemyType)
            {
                case EnemyType.Normal:
                    bonusHeal += (int)rd.GetBonus(CardFormulaInfoType.BaseMob) ;
                    break;
                case EnemyType.MiniBoss:
                    bonusHeal += (int)rd.GetBonus(CardFormulaInfoType.MiniBossMob);
                    break;
                case EnemyType.Boss:
                    bonusHeal += (int)rd.GetBonus(CardFormulaInfoType.BossMob);
                    break;
            }
            switch (battleModel)
            {
                case BattleModel.Normal:
                    bonusHeal += (int)rd.GetBonus(CardFormulaInfoType.BaseModel);
                    break;
                case BattleModel.Spell:
                    bonusHeal += (int)rd.GetBonus(CardFormulaInfoType.SpellModel);
                    break;
                case BattleModel.Tomato:
                    bonusHeal += (int)rd.GetBonus(CardFormulaInfoType.TomatoModel);
                    break;
            }
        }
        if (bonusHeal == 0)
            return bigNumber;
            
        return bigNumber * bonusHeal;
    }

    /// <summary>
    /// ?脣?暺?暺??瑕拿??
    /// </summary>
    /// <returns></returns>
    public static BigNumber GetDefaultHealValue()
    {
        Initlization();

        BigNumber bigNumber = new BigNumber();
        bigNumber = gameDefaultSetting.defaultClickHeal;
        BigNumber satisfyBonus = 0;
        BigNumber tomatoBonus = 0;
        IReadOnlyList<EnemyStorageData> enemyStorageDatas = StorageManager.instance.StorageData.EnemyStorageDatas;
        foreach (var data in enemyStorageDatas)
        {
            if (Database<RoleData>.TryLoad(data.EnemyKey, out var roleData) == false)
            {
                continue;
            }
            if (data.silverClear)
            {
                satisfyBonus += roleData.SatisfyBonus;
            }
            if (data.goldenClear)
            {
                tomatoBonus += roleData.TomatoBonus;
            }
        }

        return bigNumber + (satisfyBonus * (1+tomatoBonus));
    }

    public static float GetCardDropRate(RoleData roleData , BattleModel battleModel)
    {
        Initlization();
        // ?其遢閫?∠??????憭???
        float result = gameDefaultSetting.defaultCardDropRate ;
        
        foreach (var role in roleDatas)
        {
            var storageData = StorageManager.instance.StorageData.GetEnemyStorageData(role.key);
            if (storageData == null)
                continue;
            if (storageData.CardReward == false)
                continue;
            
            if (Database<RoleData>.TryLoad(storageData.EnemyKey, out var rd) == false)
            {
                continue;
            }

            switch (roleData.EnemyType)
            {
                case EnemyType.Normal:
                    result += rd.GetBonus(CardFormulaInfoType.BaseMobCardDropRate);
                    break;
                case EnemyType.MiniBoss:
                    result += rd.GetBonus(CardFormulaInfoType.MiniBossMobCardDropRate);
                    break;
                case EnemyType.Boss:
                    result += rd.GetBonus(CardFormulaInfoType.BossMobCardDropRate);
                    break;
            }
        }

        if (battleModel == BattleModel.Tomato)
        {
            result *= gameDefaultSetting.tomatoModeCardDropRateBonus;
        }
        return result;
    }
}
