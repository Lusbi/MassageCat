using GameCore.Database;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
[CreateAssetMenu(fileName = "GameDefaultSetting", menuName = "Scriptable Objects/GameDefaultSetting")]
public class GameDefaultSetting : ScriptableObject
{
    [LabelText("預設場景")] public ScenemapReference defaultScenemapReference;
    [LabelText("基礎療癒值")] [FormerlySerializedAs("defaultClickDamage")] public int defaultClickHeal = 1;
    [LabelText("拼字療癒倍率")] [FormerlySerializedAs("defaultSpellDamage")] public int defaultSpellHeal = 50;
    [LabelText("預設敵人")] public RoleReference defaultEnemyReference;
    [LabelText("基礎卡片掉落率（%）")] public float defaultCardDropRate = 0.02f;
    [LabelText("卡片功能解鎖旗標")] public FlagReference defaultCardFlag;
    [LabelText("蕃茄鐘卡片掉落倍率")] public float tomatoModeCardDropRateBonus = 10.0f;
    [LabelText("[Debug] 療癒倍率")] [FormerlySerializedAs("debugDamageRate")] public int debugHealRate = 1;
    [LabelText("音效設定")] public AudioClipSetting audioSettings;
    [LabelText("背景音樂設定")] public BgmClipSetting bgmSettings;
}
