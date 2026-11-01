using System;
using GameCore;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Serialization;

public class HealFeedbackPanel : MonoBehaviour, IInitlization
{
    private const int k_maxVisibleTexts = 24;
    private const int k_laneCount = 5;
    [SerializeField]
    [FormerlySerializedAs("m_defaultRect")]
    private RectTransform m_poolRootRect;
    [SerializeField]
    [FormerlySerializedAs("m_parentRect")]
    private RectTransform m_feedbackAnchorRect;
    [SerializeField]
    [FormerlySerializedAs("m_hitHUDTemplate")]
    private HealValueHUD m_healValueHudTemplate;

    private ObjectPool<HealValueHUD> m_textPool;
    private int m_nextLane;

    public void Initlization(Action callBack = null)
    {
        if (m_textPool != null)
            return;

        m_textPool = new ObjectPool<HealValueHUD>(
            OnCreateText,
            OnGetText,
            OnReleaseText,
            OnDestroyText,
            false,
            k_maxVisibleTexts,
            k_maxVisibleTexts);

        // 初始化時建立全部浮字；連續輸入期間只重用，不新增遊戲物件。
        HealValueHUD[] warmupTexts = new HealValueHUD[k_maxVisibleTexts];
        for (int i = 0; i < warmupTexts.Length; i++)
            warmupTexts[i] = m_textPool.Get();
        for (int i = 0; i < warmupTexts.Length; i++)
            m_textPool.Release(warmupTexts[i]);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[Codex驗證] 療癒浮字已預備完成；請比較一般與強療癒的字級、浮升及淡出，並確認連續輸入不遮住操作。", this);
#endif
    }

    public void PlayHealValue(string healValue, bool isStrong = false)
    {
        // 僅限制畫面上的浮字數量，不影響療癒值或遊戲進度。
        if (m_textPool == null || m_textPool.CountInactive == 0)
            return;

        HealValueHUD healValueHud = m_textPool.Get();
        float x = (m_nextLane - 2) * 32f;
        float y = (m_nextLane & 1) * 12f;
        m_nextLane = (m_nextLane + 1) % k_laneCount;
        healValueHud.RectTransform.anchoredPosition = new Vector2(x, y);
        healValueHud.RectTransform.localRotation = Quaternion.identity;
        healValueHud.ApplyPool(m_textPool);
        healValueHud.SetText(healValue);
        healValueHud.PlayHealValue(isStrong);
    }

    private HealValueHUD OnCreateText()
    {
        HealValueHUD healValueHud = Instantiate(m_healValueHudTemplate, m_poolRootRect);
        healValueHud.RectTransform.anchoredPosition = Vector2.zero;
        healValueHud.RectTransform.localScale = Vector3.one;
        healValueHud.gameObject.SetActive(false);
        return healValueHud;
    }

    private void OnDestroyText(HealValueHUD healValueHud)
    {
        if (healValueHud)
            Destroy(healValueHud.gameObject);
    }

    private void OnGetText(HealValueHUD healValueHud)
    {
        healValueHud.RectTransform.SetParent(m_feedbackAnchorRect, false);
        healValueHud.RectTransform.anchoredPosition = Vector2.zero;
        healValueHud.RectTransform.localScale = Vector3.one;
        healValueHud.gameObject.SetActive(true);
    }

    private void OnReleaseText(HealValueHUD healValueHud)
    {
        healValueHud.RectTransform.SetParent(m_poolRootRect, false);
        healValueHud.RectTransform.anchoredPosition = Vector2.zero;
        healValueHud.RectTransform.localScale = Vector3.one;
        healValueHud.Recycle();
        healValueHud.gameObject.SetActive(false);
    }
}
