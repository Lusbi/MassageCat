using System;
using GameCore;
using GameCore.Resources;

public class SettingsManager : Singleton<SettingsManager>, IInitlization
{
    private const string SETTING_ADDRESSABLE_KEY = "GameDefaultSetting";

    public GameDefaultSetting setting { get; private set; }

    public void Initlization(Action callBack = null)
    {
        CoreResoucesService.LoadAssetAsync<GameDefaultSetting>(SETTING_ADDRESSABLE_KEY,
            (setting) =>
            {
                this.setting = setting;
                callBack?.Invoke();
            });
    }
}
