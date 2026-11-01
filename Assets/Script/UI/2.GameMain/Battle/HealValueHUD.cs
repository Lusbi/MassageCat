using TMPro;
using UnityEngine;
using UnityEngine.Pool;

public class HealValueHUD : MonoBehaviour
{
    private const float k_normalDuration = 0.8f;
    private const float k_strongDuration = 1.05f;

    [SerializeField] private TextMeshProUGUI m_text;
    [SerializeField] private TMP_ColorGradient m_normalColorGradient;
    [SerializeField] private TMP_ColorGradient m_criticalColorGradient;

    private Animation m_animation;
    private RectTransform m_rectTransform;
    private RectTransform m_textRect;
    private ObjectPool<HealValueHUD> m_objectPool;
    private CanvasGroup m_canvasGroup;
    private Vector2 m_startPosition;
    private float m_elapsed;
    private float m_duration;
    private float m_riseDistance;
    private float m_peakScale;
    private bool m_isPlaying;

    public RectTransform RectTransform => m_rectTransform;

    private void Awake()
    {
        m_animation = GetComponent<Animation>();
        m_rectTransform = GetComponent<RectTransform>();
        m_textRect = m_text.GetComponent<RectTransform>();
        m_canvasGroup = GetComponent<CanvasGroup>();
        m_text.raycastTarget = false;
    }

    public void Recycle()
    {
        m_isPlaying = false;
        if (m_animation)
            m_animation.Stop();
        m_text.SetText(string.Empty);
        m_textRect.anchoredPosition = Vector2.zero;
        if (m_canvasGroup)
            m_canvasGroup.alpha = 1f;
    }

    public void SetText(string healValue)
    {
        m_text.SetText(healValue);
        m_textRect.anchoredPosition = Vector2.zero;
    }

    public void PlayHealValue(bool isStrong = false)
    {
        // 保留舊動畫資產與公共呼叫介面，演出改以數值更新，避免旋轉與大幅跳動。
        if (m_animation)
            m_animation.Stop();
        m_text.colorGradientPreset = isStrong ? m_criticalColorGradient : m_normalColorGradient;
        m_text.fontSize = isStrong ? 34f : 28f;
        m_startPosition = m_rectTransform.anchoredPosition;
        m_elapsed = 0f;
        m_duration = isStrong ? k_strongDuration : k_normalDuration;
        m_riseDistance = isStrong ? 58f : 42f;
        m_peakScale = isStrong ? 1.18f : 1.06f;
        m_rectTransform.localRotation = Quaternion.identity;
        m_rectTransform.localScale = Vector3.one * 0.85f;
        if (m_canvasGroup)
            m_canvasGroup.alpha = 1f;
        m_isPlaying = true;
    }

    private void Update()
    {
        if (!m_isPlaying)
            return;

        m_elapsed += Time.unscaledDeltaTime;
        float progress = Mathf.Clamp01(m_elapsed / m_duration);
        float rise = 1f - (1f - progress) * (1f - progress);
        m_rectTransform.anchoredPosition = m_startPosition + new Vector2(0f, m_riseDistance * rise);
        float scale = m_elapsed < 0.1f
            ? Mathf.Lerp(0.85f, m_peakScale, m_elapsed / 0.1f)
            : Mathf.Lerp(m_peakScale, 1f, Mathf.Clamp01((m_elapsed - 0.1f) / 0.16f));
        m_rectTransform.localScale = Vector3.one * scale;
        if (m_canvasGroup)
            m_canvasGroup.alpha = 1f - Mathf.Clamp01((progress - 0.65f) / 0.35f);

        if (progress >= 1f)
            ReleaseToPool();
    }

    internal void ApplyPool(ObjectPool<HealValueHUD> textPool)
    {
        m_objectPool ??= textPool;
    }

    /// <summary>
    /// 演出結束時回收；保留舊動畫事件使用的入口，避免重複回收。
    /// </summary>
    public void ReleaseToPool()
    {
        if (!m_isPlaying)
            return;

        m_isPlaying = false;
        m_objectPool?.Release(this);
    }
}
