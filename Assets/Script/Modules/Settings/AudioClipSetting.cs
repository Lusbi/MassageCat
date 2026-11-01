using Sirenix.OdinInspector;
using UnityEngine;

public enum AudioClipType
{
    SatisfySFX,
    HealSFX,
    CriticleSFX,
    BossAppearSFX,
    ColdDownSFX,
    Spell_SuceessSFX,
    Spell_FailSFX,
    Button_ClickSFX,
    GetCardSFX,
}

[System.Serializable]
public struct AudioClipData
{
    [HideLabel , HorizontalGroup]
    public AudioClipType audioClipType;
    [HideLabel , HorizontalGroup]
    public AudioClip audioClip;
}

[CreateAssetMenu(fileName = "AudioClipSetting", menuName = "Scriptable Objects/AudioClipSetting")]
public class AudioClipSetting : ScriptableObject
{
    [SerializeField] private AudioClipData[] audioClipDatas;

    public AudioClip GetAudioClip(AudioClipType audioClipType)
    {
        foreach (var audioClipData in audioClipDatas)
        {
            if (audioClipData.audioClipType == audioClipType)
            {
                return audioClipData.audioClip;
            }
        }
        Debug.LogWarning($"AudioClip of type {audioClipType} not found!");
        return null;
    }
}
