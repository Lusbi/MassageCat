using System;
using GameCore.UI;
using UnityEngine;

public class TeachStepInfo : MonoBehaviour
{
    private const float k_inputGracePeriod = 0.25f;
    private const float k_autoCompleteDelay = 5f;

    [SerializeField] private CanvasGroup m_canvasGroup;
    [SerializeField] private string m_teachKey;
    [SerializeField] private UIButton m_clickBtn;
    [SerializeField] private GameObject m_obj;
    /// <summary>
    /// 判定用 Key
    /// </summary>
    public string TeachKey => m_teachKey;
    private Action m_nextAction;
    private float m_inputEnableTime;
    private float m_autoCompleteTime;

    public void OnEnable()
    {
        TutorialFlowDiagnostics.Log(
            $"教學步驟啟用：stepKey={m_teachKey}, hasClickButton={m_clickBtn != null}, hasTargetObject={m_obj != null}",
            this);
        if (m_clickBtn != null)
            m_clickBtn.onClicked = OnClick;
    }

    public void OnDisable()
    {
        TutorialFlowDiagnostics.Log(
            $"教學步驟停用並清除點擊事件：stepKey={m_teachKey}, activeSelf={gameObject.activeSelf}, activeInHierarchy={gameObject.activeInHierarchy}",
            this);
        if (m_clickBtn != null)
            m_clickBtn.onClicked = null;

        m_nextAction = null;
        m_autoCompleteTime = 0f;
    }

    public void Apply(Action action)
    {
        m_nextAction = action;
        // 避免戰鬥收尾的同一次連點，立即略過剛顯示的教學。
        float currentTime = Time.realtimeSinceStartup;
        m_inputEnableTime = currentTime + k_inputGracePeriod;
        m_autoCompleteTime = currentTime + k_autoCompleteDelay;
        TutorialFlowDiagnostics.Log(
            $"顯示教學步驟：stepKey={m_teachKey}, hasNextAction={m_nextAction != null}, hasTargetObject={m_obj != null}, clickHandlerBound={m_clickBtn != null && m_clickBtn.onClicked != null}, targetActiveSelf={(m_obj != null && m_obj.activeSelf)}, targetActiveInHierarchy={(m_obj != null && m_obj.activeInHierarchy)}",
            this);
        if (m_obj)
        {
            m_obj.SetActive(true);
            CanvasGroupUpdate(true);
        }
    }

    private void OnClick(UIButton button)
    {
        TryComplete();
    }

    internal bool TryComplete()
    {
        if (Time.realtimeSinceStartup < m_inputEnableTime)
            return false;

        Action nextAction = m_nextAction;
        if (nextAction == null)
            return false;

        // 先清除回呼，確保 UI 事件與全畫面點擊備援不會重複完成同一步驟。
        m_nextAction = null;
        TutorialFlowDiagnostics.Log(
            $"完成教學步驟：stepKey={m_teachKey}",
            this);
        if (m_obj)
        {
            m_obj.SetActive(false);
            CanvasGroupUpdate(false);
        }

        nextAction.Invoke();

        return true;
    }

    internal bool TryCompleteAfterTimeout()
    {
        return m_nextAction != null
            && Time.realtimeSinceStartup >= m_autoCompleteTime
            && TryComplete();
    }

    private void CanvasGroupUpdate(bool isActive)
    {
        if (m_canvasGroup == null)
        {
            m_canvasGroup = GetComponent<CanvasGroup>();
        }
        if (m_canvasGroup == null)
        {
            TutorialFlowDiagnostics.LogWarning(
                $"教學步驟找不到 CanvasGroup：stepKey={m_teachKey}, requestedActive={isActive}",
                this);
            return;
        }

        m_canvasGroup.alpha = isActive ? 1 : 0;
        m_canvasGroup.blocksRaycasts = isActive;
        TutorialFlowDiagnostics.Log(
            $"更新教學步驟顯示：stepKey={m_teachKey}, active={isActive}, alpha={m_canvasGroup.alpha}, blocksRaycasts={m_canvasGroup.blocksRaycasts}, activeSelf={gameObject.activeSelf}, activeInHierarchy={gameObject.activeInHierarchy}",
            this);
    }
}
