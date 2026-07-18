using System;
using GameCore;
using GameCore.Audio;
using GameCore.Database;
using GameCore.Event;
using GameCore.Resources;
using UnityEngine;

public class TomatoManager : MonoSingleton<TomatoManager>, IInitlization
{
    private const string SETTING_ADDRESSABLE_KEY = "TomatoSetting";

    private const string PP_START_UTC_TICKS = "Tomato.StartUtcTicks";
    private const string PP_DURATION_SEC = "Tomato.DurationSec";

    public bool isTomatoTime { get; private set; }
    public float curTomatoTime { get; private set; } 
    public TomatoSetting setting { get; private set; }

    private DateTime _startUtc;   
    private float _durationSec;   
    private bool _notified;       
    private int _lastSecond = -1; 

    public string curTomatoTimeString
    {
        get
        {
            if (curTomatoTime <= 0) return string.Empty;
            int minutes = Mathf.FloorToInt(curTomatoTime / 60);
            int sec = Mathf.FloorToInt(curTomatoTime % 60);
            return string.Format("{0:00}:{1:00}", minutes, sec);
        }
    }

    public void Initlization(Action callBack = null)
    {
        CoreResoucesService.LoadAssetAsync<TomatoSetting>(SETTING_ADDRESSABLE_KEY, s =>
        {
            this.setting = s;
            TutorialFlowDiagnostics.Log($"蕃茄鐘設定載入完成：hasSetting={setting != null}", this);

            TryRestoreRunningClock();

            callBack?.Invoke();
        });
    }

    public void Apply(RoleData roleData)
    {
        if (roleData)
        {
            float minute = setting.GetMinute(roleData.key);
            _durationSec = minute * 60f;
            curTomatoTime = _durationSec;
            TutorialFlowDiagnostics.Log(
                $"套用蕃茄鐘角色時間：roleKey={roleData.key}, minute={minute}, durationSec={_durationSec}",
                this);
        }
        else
        {
            _durationSec = 0f;
            curTomatoTime = 0f;
            TutorialFlowDiagnostics.Log("清除蕃茄鐘角色時間。", this);
        }
    }

    public void TomatoTrigger()
    {
        if (isTomatoTime)
        {
            TutorialFlowDiagnostics.Log("要求開始蕃茄鐘，但倒數已在執行中。", this);
            return;
        }

        _startUtc = DateTime.UtcNow;
        isTomatoTime = true;
        _notified = false;
        _lastSecond = -1;

        SaveRunningClock();
        CancelInvoke(nameof(Tick));
        InvokeRepeating(nameof(Tick), 0f, 0.25f);

        TutorialFlowDiagnostics.Log(
            $"蕃茄鐘倒數正式開始：startUtc={_startUtc:O}, durationSec={_durationSec}",
            this);
        EventManager.instance.Notify(new TomatoTriggerEvent(true));
    }

    private void Tick()
    {
        var endUtc = _startUtc.AddSeconds(_durationSec);
        var remain = (float)(endUtc - DateTime.UtcNow).TotalSeconds;

        if (remain < 0f) remain = 0f;

        curTomatoTime = remain;

        if (remain > 0f && remain <= 10f)
        {
            int currentSecond = Mathf.FloorToInt(remain);
            if (currentSecond != _lastSecond)
            {
                _lastSecond = currentSecond;
                PlaySFX(AudioClipType.ColdDownSFX);
            }
        }

        if (remain <= 0f && !_notified)
        {
            CompleteTomatoTime();
        }
    }

    private void CompleteTomatoTime()
    {
        _notified = true;
        isTomatoTime = false;
        CancelInvoke(nameof(Tick));
        ClearRunningClock();
        TutorialFlowDiagnostics.Log("蕃茄鐘倒數結束，準備派送停止事件。", this);
        EventManager.instance.Notify(new TomatoTriggerEvent(false));
    }

    private void TryRestoreRunningClock()
    {
        if (!PlayerPrefs.HasKey(PP_START_UTC_TICKS) || !PlayerPrefs.HasKey(PP_DURATION_SEC))
        {
            TutorialFlowDiagnostics.Log("沒有可恢復的蕃茄鐘倒數。", this);
            return;
        }

        var ticksStr = PlayerPrefs.GetString(PP_START_UTC_TICKS);
        if (!long.TryParse(ticksStr, out var ticks))
        {
            TutorialFlowDiagnostics.LogWarning("蕃茄鐘起始時間資料無效，清除保存狀態。", this);
            ClearRunningClock();
            return;
        }

        _startUtc = new DateTime(ticks, DateTimeKind.Utc);
        _durationSec = PlayerPrefs.GetFloat(PP_DURATION_SEC, 0f);

        var endUtc = _startUtc.AddSeconds(_durationSec);
        var remain = (float)(endUtc - DateTime.UtcNow).TotalSeconds;
        if (remain <= 0f)
        {
            curTomatoTime = 0f;
            isTomatoTime = false;
            _notified = true;
            ClearRunningClock();
            TutorialFlowDiagnostics.Log("保存的蕃茄鐘已逾期，不恢復倒數。", this);
        }
        else
        {
            curTomatoTime = remain;
            isTomatoTime = true;
            _notified = false;
            CancelInvoke(nameof(Tick));
            InvokeRepeating(nameof(Tick), 0f, 0.25f);
            TutorialFlowDiagnostics.Log(
                $"恢復進行中的蕃茄鐘：remainSec={remain}, durationSec={_durationSec}",
                this);
        }
    }

    private void SaveRunningClock()
    {
        PlayerPrefs.SetString(PP_START_UTC_TICKS, _startUtc.Ticks.ToString());
        PlayerPrefs.SetFloat(PP_DURATION_SEC, _durationSec);
        PlayerPrefs.Save();
    }

    private void ClearRunningClock()
    {
        PlayerPrefs.DeleteKey(PP_START_UTC_TICKS);
        PlayerPrefs.DeleteKey(PP_DURATION_SEC);
        PlayerPrefs.Save();
    }

    private void OnApplicationFocus(bool focus)
    {
        if (focus && isTomatoTime) Tick();
    }
    private void OnApplicationPause(bool paused)
    {
        if (!paused && isTomatoTime) Tick();
    }

    private void PlaySFX(AudioClipType clipType)
    {
        AudioClipManager.instance.PlaySFX(clipType);
    }
}
