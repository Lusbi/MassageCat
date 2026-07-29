using System.Collections.Generic;
using GameCore.Audio;
using GameCore.Database;
using GameCore.Event;
using GameCore.UI;
using UnityEngine;

public class RoleScrollRect : MonoBehaviour
{
    private const float k_idleArrowAlpha = 0.35f;

    [SerializeField] private UIButton m_leftArrowButton;
    [SerializeField] private UIButton m_rightArrowButton;
    [SerializeField] private GameObject m_normalCompletionIcon;
    [SerializeField] private GameObject m_tomatoCompletionIcon;
    [SerializeField] private GameObject m_spellCompletionIcon;

    private List<RoleInfo> m_sortedRoleList;
    private EventListener m_eventListener;
    private CanvasGroup m_leftArrowCanvasGroup;
    private CanvasGroup m_rightArrowCanvasGroup;
    private int m_currentRoleIndex = -1;
    private bool m_isArrowInteractionEnabled = true;
    private bool m_isInitialized;

    private void Awake()
    {
        EnsureInitialized();
    }

    public void Start()
    {
        EnsureInitialized();
    }

    private void OnDestroy()
    {
        if (m_leftArrowButton != null)
            m_leftArrowButton.onHover -= OnLeftArrowHover;

        if (m_rightArrowButton != null)
            m_rightArrowButton.onHover -= OnRightArrowHover;

        m_eventListener?.Destroy();
    }

    private bool EnsureInitialized()
    {
        if (m_isInitialized)
            return true;

        if (m_leftArrowButton == null || m_rightArrowButton == null)
            return false;

        Dictionary<string, RoleData>.ValueCollection roleDatas = Database<RoleData>.GetAll();
        m_sortedRoleList = new List<RoleInfo>(roleDatas.Count);
        foreach (RoleData roleData in roleDatas)
        {
            m_sortedRoleList.Add(new RoleInfo
            {
                roleData = roleData
            });
        }

        m_sortedRoleList.Sort(CompareRoleInfo);
        m_leftArrowButton.onClicked = OnPreviousRoleClicked;
        m_rightArrowButton.onClicked = OnNextRoleClicked;
        m_leftArrowButton.onHover += OnLeftArrowHover;
        m_rightArrowButton.onHover += OnRightArrowHover;
        m_leftArrowCanvasGroup = m_leftArrowButton.GetComponent<CanvasGroup>();
        m_rightArrowCanvasGroup = m_rightArrowButton.GetComponent<CanvasGroup>();

        m_eventListener = new EventListener();
        m_eventListener.AddListener<FlagUpdateEvent>(OnFlagUpdateEvent, true);

        m_isInitialized = true;
        RefreshCompletionIndicators();
        UpdateArrowVisibility();
        return true;
    }

    private static int CompareRoleInfo(RoleInfo left, RoleInfo right)
    {
        return left.roleData.roleSortId.CompareTo(right.roleData.roleSortId);
    }

    /// <summary>
    /// 更新中央角色左右箭頭的顯示狀態。
    /// </summary>
    internal void ListUpdate()
    {
        if (EnsureInitialized() == false)
            return;

        RefreshCompletionIndicators();
        UpdateArrowVisibility();
    }

    /// <summary>
    /// 同步目前中央顯示角色與角色索引。
    /// </summary>
    internal void ListMoveToTop(RoleData data)
    {
        if (EnsureInitialized() == false || data == null)
            return;

        for (int i = 0; i < m_sortedRoleList.Count; i++)
        {
            if (m_sortedRoleList[i].roleData != data)
                continue;

            m_currentRoleIndex = i;
            RefreshCompletionIndicators();
            UpdateArrowVisibility();
            return;
        }
    }

    internal void SetArrowInteractionEnabled(bool isEnabled)
    {
        if (m_isArrowInteractionEnabled == isEnabled)
            return;

        m_isArrowInteractionEnabled = isEnabled;
        UpdateArrowVisibility();
    }

    private void OnPreviousRoleClicked(UIButton button)
    {
        TrySelectAdjacentRole(-1);
    }

    private void OnNextRoleClicked(UIButton button)
    {
        TrySelectAdjacentRole(1);
    }

    private void TrySelectAdjacentRole(int direction)
    {
        if (m_isArrowInteractionEnabled == false)
            return;

        int targetIndex = m_currentRoleIndex + direction;
        if (CanSelectRole(targetIndex) == false)
            return;

        AudioClipManager.instance.PlaySFX(AudioClipType.Button_ClickSFX, 0.5f);
        UnityMainThreadDispatcher.DelaySetInputListenerState(false, 0.1f);
        EventManager.instance.Notify(new SetBattleRoleEvent(m_sortedRoleList[targetIndex].roleData.key));
    }

    private void OnFlagUpdateEvent(FlagUpdateEvent eventData)
    {
        UpdateArrowVisibility();
    }

    private void OnLeftArrowHover(UIButton button, bool isHovered)
    {
        SetArrowAlpha(m_leftArrowCanvasGroup, isHovered);
    }

    private void OnRightArrowHover(UIButton button, bool isHovered)
    {
        SetArrowAlpha(m_rightArrowCanvasGroup, isHovered);
    }

    private void UpdateArrowVisibility()
    {
        if (m_leftArrowButton != null)
        {
            UpdateArrowVisibility(
                m_leftArrowButton,
                m_leftArrowCanvasGroup,
                CanSelectRole(m_currentRoleIndex - 1),
                m_isArrowInteractionEnabled);
        }

        if (m_rightArrowButton != null)
        {
            UpdateArrowVisibility(
                m_rightArrowButton,
                m_rightArrowCanvasGroup,
                CanSelectRole(m_currentRoleIndex + 1),
                m_isArrowInteractionEnabled);
        }
    }

    private static void UpdateArrowVisibility(
        UIButton button,
        CanvasGroup canvasGroup,
        bool isVisible,
        bool isInteractionEnabled)
    {
        if (isVisible == false)
        {
            button.hover = false;
            button.Show(false);
            return;
        }

        button.locked = !isInteractionEnabled;
        SetArrowAlpha(canvasGroup, false);
        button.Show(true);
    }

    private static void SetArrowAlpha(CanvasGroup canvasGroup, bool isHovered)
    {
        if (canvasGroup != null)
            canvasGroup.alpha = isHovered ? 1f : k_idleArrowAlpha;
    }

    private bool CanSelectRole(int roleIndex)
    {
        return m_currentRoleIndex >= 0
            && roleIndex >= 0
            && roleIndex < m_sortedRoleList.Count
            && m_sortedRoleList[roleIndex].roleData.ValidateFlagReferenceConditions();
    }

    private void RefreshCompletionIndicators()
    {
        EnemyStorageData storageData = null;
        if (m_currentRoleIndex >= 0 && m_currentRoleIndex < m_sortedRoleList.Count)
        {
            RoleData roleData = m_sortedRoleList[m_currentRoleIndex].roleData;
            storageData = StorageManager.instance.StorageData.GetEnemyStorageData(roleData.key);
        }

        SetCompletionIndicator(
            m_normalCompletionIcon,
            storageData != null && storageData.silverClear);
        SetCompletionIndicator(
            m_tomatoCompletionIcon,
            storageData != null && storageData.goldenClear);
        SetCompletionIndicator(
            m_spellCompletionIcon,
            storageData != null && storageData.speedClear);
    }

    private static void SetCompletionIndicator(GameObject indicator, bool isVisible)
    {
        if (indicator != null && indicator.activeSelf != isVisible)
            indicator.SetActive(isVisible);
    }
}
