using Sirenix.OdinInspector;
using UnityEngine;

public enum BgmClipType
{
    MainTheme,
    BaseEnemyTheme,
    BossTheme,
    TomatoModeTheme,
    SpellTheme,
    //精英
    EliteEnemyTheme,
}


[System.Serializable]
public struct BgmClipData
{
    [HideLabel , HorizontalGroup]
    public BgmClipType bgmClipType;
    [HideLabel , HorizontalGroup]
    public AudioClip audioClip;
}


[CreateAssetMenu(fileName = "BgmClipSetting", menuName = "Scriptable Objects/BgmClipSetting")]
public class BgmClipSetting : ScriptableObject
{
        [SerializeField] private BgmClipData[] bgmClipDatas;

    public AudioClip GetAudioClip(BgmClipType bgmClipType)
    {
        foreach (var bgmClipData in bgmClipDatas)
        {
            if (bgmClipData.bgmClipType == bgmClipType)
            {
                return bgmClipData.audioClip;
            }
        }
        Debug.LogWarning($"AudioClip of type {bgmClipType} not found!");
        return null;
    }
}
