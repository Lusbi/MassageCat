using System;
using GameCore;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Serialization;

public class HealFeedbackPanel : MonoBehaviour, IInitlization
{
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

    public void Initlization(Action callBack = null)
    {
        m_textPool = new ObjectPool<HealValueHUD>(
            OnCreateText,
            OnGetText,
            OnReleaseText,
            null,
            false,
            10);
    }

    public void PlayHealValue(string healValue, bool isStrong = false)
    {
        HealValueHUD healValueHud = m_textPool.Get();
        float x = UnityEngine.Random.Range(-100f, 100f);
        float y = UnityEngine.Random.Range(-50f, 50f);
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
