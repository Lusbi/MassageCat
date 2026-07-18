using System;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;

public class HitHUD : MonoBehaviour
{
    private const string k_aniNormal = "Hit_Normal";
    private const string k_aniTomato = "Hit_Tomato";
    private Animation m_ani;
    [SerializeField] private TextMeshProUGUI m_text;
    [SerializeField] private TMP_ColorGradient m_normalColorGradient;
    [SerializeField] private TMP_ColorGradient m_criticalColorGradient;
    private RectTransform m_rectTransform;
    public RectTransform rectTransform => m_rectTransform;

    private RectTransform m_textRect;

    private ObjectPool<HitHUD> m_objectPool;
    void Awake()
    {
        m_ani = GetComponent<Animation>();
        m_rectTransform = GetComponent<RectTransform>();
        m_textRect = m_text.GetComponent<RectTransform>();
    }
    public void Recycle()
    {
        m_text.text = string.Empty;
    }
    public void SetText(string hitValue)
    {
        m_text.text = hitValue;
        m_textRect.anchoredPosition = new Vector2(0, UnityEngine.Random.Range(-50, 50));
    }
    public void PlayHit(bool isTomato = false)
    {
        if (isTomato)
        {
            m_ani.clip = m_ani.GetClip(k_aniTomato);
            m_ani.Play(k_aniTomato);

            m_text.colorGradientPreset = m_criticalColorGradient;
        }
        else
        {
            m_ani.clip = m_ani.GetClip(k_aniNormal);
            m_ani.Play(k_aniNormal);

            m_text.colorGradientPreset = m_normalColorGradient;
        }
    }

    internal void ApplyPool(ObjectPool<HitHUD> m_textPool)
    {
        if (m_objectPool == null)
            m_objectPool = m_textPool;
    }
    /// <summary>
    /// animation event called when animation end
    /// </summary>
    public void ReleaseToPool()
    {
        m_objectPool?.Release(this);
    }
}
