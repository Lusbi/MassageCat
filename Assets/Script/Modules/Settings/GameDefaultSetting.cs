using GameCore.Database;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
[CreateAssetMenu(fileName = "GameDefaultSetting", menuName = "Scriptable Objects/GameDefaultSetting")]
public class GameDefaultSetting : ScriptableObject
{
    [LabelText("w]d")] public ScenemapReference defaultScenemapReference;
    [LabelText("療癒值")] [FormerlySerializedAs("defaultClickDamage")] public int defaultClickHeal = 1;
    [LabelText("字彙療癒值")] [FormerlySerializedAs("defaultSpellDamage")] public int defaultSpellHeal = 50;
    [LabelText("w]ĤH")] public RoleReference defaultEnemyReference;
    [LabelText("dv")] public float defaultCardDropRate = 0.02f;
    [LabelText("즸odX")] public FlagReference defaultCardFlag;
    [LabelText("XҦdv[")] public float tomatoModeCardDropRateBonus = 10.0f;
    [LabelText("[Debug]療癒值")] [FormerlySerializedAs("debugDamageRate")] public int debugHealRate = 1;
    [LabelText("ĳ]w")] public AudioClipSetting audioSettings;
    [LabelText("背景音樂設定")] public BgmClipSetting bgmSettings;
}
