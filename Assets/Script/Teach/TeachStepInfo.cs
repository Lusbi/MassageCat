using System;
using GameCore.UI;
using UnityEngine;

public class TeachStepInfo : MonoBehaviour
{
    [SerializeField] private CanvasGroup m_canvasGroup;
    [SerializeField] private string m_teachKey;
    [SerializeField] private UIButton m_clickBtn;
    [SerializeField] private GameObject m_obj;
    /// <summary>
    /// 判定用 Key
    /// </summary>
    public string TeachKey => m_teachKey;
    private Action m_nextAction;

    public void OnEnable()
    {
        TutorialFlowDiagnostics.Log(
            $"教學步驟啟用：stepKey={m_teachKey}, hasClickButton={m_clickBtn != null}, hasTargetObject={m_obj != null}",
            this);
        m_clickBtn.onClicked = OnClick;
    }

    public void OnDisable()
    {
        TutorialFlowDiagnostics.Log(
            $"教學步驟停用並清除點擊事件：stepKey={m_teachKey}, activeSelf={gameObject.activeSelf}, activeInHierarchy={gameObject.activeInHierarchy}",
            this);
        m_clickBtn.onClicked = null;
    }

    public void Apply(Action action)
    {
        m_nextAction = action;
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
        TutorialFlowDiagnostics.Log(
            $"點擊教學步驟：stepKey={m_teachKey}, hasNextAction={m_nextAction != null}",
            this);
        m_nextAction?.Invoke();
        if (m_obj)
        {
            m_obj.SetActive(false);
            CanvasGroupUpdate(false);
        }
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
