using System.Collections.Generic;
using UnityEngine;

public class TeachPanel : MonoBehaviour
{
    [SerializeField] private CanvasGroup m_canvasGroup;
    [SerializeField] private TeachInfo[] m_teachInfos;

    private TeachInfo m_cacheTeachInfo;
    private IReadOnlyList<TeachStepInfo> m_cacheTeachSteps;
    private TeachStepInfo m_activeStep;
    private int m_index;
    private bool m_isTeaching;

    private void Update()
    {
        if (!m_isTeaching || m_activeStep == null)
            return;

        if (m_activeStep.TryCompleteAfterTimeout())
            return;

        // 教學顯示期間允許點擊畫面任意位置完成當前步驟，避免 UI Raycast 遺失後流程卡住。
        if (Input.GetMouseButtonDown(0))
            m_activeStep.TryComplete();
    }

    /// <summary>
    /// 觸發教學旗標
    /// </summary>
    /// <param name="flagKey">要觸發的教學旗標</param>
    public void TeachTrigger(string flagKey)
    {
        if (string.IsNullOrEmpty(flagKey))
        {
            TutorialFlowDiagnostics.LogWarning("收到空白教學旗標，取消觸發。", this);
            return;
        }

        int flagValue = StorageManager.instance.StorageData.GetFlagStorageValue(flagKey);
        int teachInfoCount = m_teachInfos != null ? m_teachInfos.Length : 0;
        TutorialFlowDiagnostics.Log(
            $"開始匹配教學：flagKey={flagKey}, flagValue={flagValue}, teachInfoCount={teachInfoCount}",
            this);

        if (m_teachInfos == null)
        {
            TutorialFlowDiagnostics.LogWarning($"教學設定陣列為 null：flagKey={flagKey}", this);
            return;
        }

        foreach (var teachInfo in m_teachInfos)
        {
            if (teachInfo.Valid(flagKey))
            {
                m_cacheTeachInfo = teachInfo;
                m_cacheTeachSteps = m_cacheTeachInfo.TeachSteps;
                int stepCount = m_cacheTeachSteps != null ? m_cacheTeachSteps.Count : 0;
                TutorialFlowDiagnostics.Log(
                    $"教學匹配成功：flagKey={flagKey}, stepCount={stepCount}",
                    this);
                TriggerTeachInfo();
                return;
            }
        }

        TutorialFlowDiagnostics.LogWarning(
            $"找不到可觸發的教學：flagKey={flagKey}, flagValue={flagValue}",
            this);
    }

    public void TriggerTeachInfo()
    {
        if (m_cacheTeachInfo == null || m_cacheTeachSteps == null)
        {
            TutorialFlowDiagnostics.LogWarning(
                $"教學快取無效，取消開始：hasTeachInfo={m_cacheTeachInfo != null}, hasTeachSteps={m_cacheTeachSteps != null}",
                this);
            return;
        }

        m_index = 0;
        TutorialFlowDiagnostics.Log($"開始教學步驟：stepCount={m_cacheTeachSteps.Count}", this);
        NextTeachStep();
        UpdateTeachState(false);
    }

    private void NextTeachStep()
    {
        if (m_index < 0 || m_index >= m_cacheTeachSteps.Count)
        {
            m_activeStep = null;
            TutorialFlowDiagnostics.Log(
                $"教學步驟完成：index={m_index}, stepCount={m_cacheTeachSteps.Count}",
                this);
            // 教學結束
            UpdateTeachState(true);
            return;
        }

        var stepInfo = m_cacheTeachSteps[m_index];
        if (stepInfo == null)
        {
            TutorialFlowDiagnostics.LogWarning($"略過空白教學步驟：index={m_index}", this);
            m_index++;
            NextTeachStep();
            return;
        }

        m_activeStep = stepInfo;
        TutorialFlowDiagnostics.Log(
            $"套用教學步驟：index={m_index}, stepKey={stepInfo.TeachKey}",
            this);
        stepInfo.Apply(() =>
        {
            TutorialFlowDiagnostics.Log(
                $"教學步驟要求前往下一步：index={m_index}, stepKey={stepInfo.TeachKey}",
                this);
            if (ReferenceEquals(m_activeStep, stepInfo))
                m_activeStep = null;

            m_index++;
            NextTeachStep();
        });
    }

    /// <summary>
    /// 觸發劇情時需要暫停按鍵監聽
    /// </summary>
    /// <param name="teachState"></param>
    public void UpdateTeachState(bool teachState)
    {
        m_isTeaching = !teachState;
        if (teachState)
            m_activeStep = null;

        if (m_canvasGroup)
        {
            m_canvasGroup.blocksRaycasts = !teachState;
            m_canvasGroup.alpha = !teachState ? 1 : 0;
            TutorialFlowDiagnostics.Log(
                $"更新教學顯示狀態：completed={teachState}, alpha={m_canvasGroup.alpha}, blocksRaycasts={m_canvasGroup.blocksRaycasts}, inputEnabled={teachState}, activeSelf={gameObject.activeSelf}, activeInHierarchy={gameObject.activeInHierarchy}",
                this);
        }
        else
        {
            TutorialFlowDiagnostics.LogWarning(
                $"教學 CanvasGroup 未設定：completed={teachState}, inputEnabled={teachState}",
                this);
        }

        UnityMainThreadDispatcher.SetInputListenerState(teachState);
    }
}
