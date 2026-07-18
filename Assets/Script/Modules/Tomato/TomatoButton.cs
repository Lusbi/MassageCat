using System;
using GameCore.Database;
using GameCore.Event;
using GameCore.UI;
using TMPro;
using UnityEngine;

public class TomatoButton : MonoBehaviour
{
    private const string k_useageLocalization = "Tomato.UseageTip";
    private const string k_lockedLocalization = "Tomato.DontClose";
    [SerializeField] private CanvasGroup m_canvasGroup;
    [SerializeField] private TextMeshProUGUI m_minuteText;
    [SerializeField] private UIButton m_clickBtn;
    [SerializeField] private GameObject m_enableObj;
    [SerializeField] private GameObject m_disableObj;
    [SerializeField] private FlagReference m_flagReference;
    private TomatoManager m_tomatoManager;
    private EventListener m_eventListener;
    public void Initlization()
    {
        m_tomatoManager ??= TomatoManager.instance;
        TutorialFlowDiagnostics.Log(
            $"初始化蕃茄鐘按鈕：configuredFlagKey={m_flagReference.GetKey()}",
            this);
        if (m_clickBtn)
        {
            m_clickBtn.onClicked = TriggerTomatoTime;
        }

        if (m_minuteText)
            m_minuteText.text = string.Empty;

        if (m_eventListener == null)
        {
            m_eventListener = new EventListener();
            m_eventListener.AddListener<FlagUpdateEvent>(OnFlagUpdateEvent, true);
        }

        OnFlagUpdateEvent(new FlagUpdateEvent(m_flagReference.GetKey()));
        SetTrigger(false);
    }

    private void OnFlagUpdateEvent(FlagUpdateEvent eventData)
    {
        string configuredFlagKey = m_flagReference.GetKey();
        int flagValue = StorageManager.instance.StorageData.GetFlagStorageValue(eventData.flagKey);
        bool unlocked = flagValue > 0;
        TutorialFlowDiagnostics.Log(
            $"蕃茄鐘按鈕收到旗標事件：eventFlagKey={eventData.flagKey}, configuredFlagKey={configuredFlagKey}, keyMatched={eventData.flagKey == configuredFlagKey}, flagValue={flagValue}, unlocked={unlocked}",
            this);

        if (unlocked)
        {
            TomatoState(true);
        }
        else
        {
            TomatoState(false);
        }
    }

    public void SetTrigger(bool trigger)
    {
        m_enableObj.SetActive(trigger);
        m_disableObj.SetActive(!trigger);
    }

    public void ApplyTomatoTime()
    {
        m_minuteText.text = m_tomatoManager.curTomatoTimeString;
    }

    private void TriggerTomatoTime(UIButton uIButton)
    {
        if (m_tomatoManager.isTomatoTime)
        {
            TutorialFlowDiagnostics.Log("點擊蕃茄鐘按鈕，但倒數已在執行中。", this);
            return;
        }
        TutorialFlowDiagnostics.Log("玩家點擊蕃茄鐘按鈕，準備開始倒數。", this);
        UnityMainThreadDispatcher.DelaySetInputListenerState(false, 0.1f);
        m_tomatoManager.TomatoTrigger();
    }

    private void TomatoState(bool state)
    {
        m_canvasGroup.alpha = state ? 1 : 0;
        m_canvasGroup.blocksRaycasts = state;
        TutorialFlowDiagnostics.Log(
            $"更新蕃茄鐘按鈕解鎖狀態：unlocked={state}, alpha={m_canvasGroup.alpha}, blocksRaycasts={m_canvasGroup.blocksRaycasts}",
            this);
    }

    internal void TomatoTimeHint(RoleData roleData)
    {
        var curTomatoTime = m_tomatoManager.setting.GetMinute(roleData.key) * 60;
        int minutes = Mathf.FloorToInt(curTomatoTime / 60);
        int sec = Mathf.FloorToInt(curTomatoTime % 60);
        m_minuteText.text = string.Format("{0:00}:{1:00}", minutes, sec);
    }
}
