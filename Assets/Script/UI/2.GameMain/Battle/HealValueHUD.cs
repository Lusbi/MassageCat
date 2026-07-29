using TMPro;
using UnityEngine;
using UnityEngine.Pool;

public class HealValueHUD : MonoBehaviour
{
    private const string k_normalAnimation = "HealValue_Normal";
    private const string k_strongAnimation = "HealValue_Strong";

    [SerializeField] private TextMeshProUGUI m_text;
    [SerializeField] private TMP_ColorGradient m_normalColorGradient;
    [SerializeField] private TMP_ColorGradient m_criticalColorGradient;

    private Animation m_animation;
    private RectTransform m_rectTransform;
    private RectTransform m_textRect;
    private ObjectPool<HealValueHUD> m_objectPool;

    public RectTransform RectTransform => m_rectTransform;

    private void Awake()
    {
        m_animation = GetComponent<Animation>();
        m_rectTransform = GetComponent<RectTransform>();
        m_textRect = m_text.GetComponent<RectTransform>();
    }

    public void Recycle()
    {
        m_text.text = string.Empty;
    }

    public void SetText(string healValue)
    {
        m_text.text = healValue;
        m_textRect.anchoredPosition = new Vector2(0f, UnityEngine.Random.Range(-50f, 50f));
    }

    public void PlayHealValue(bool isStrong = false)
    {
        string animationName = isStrong ? k_strongAnimation : k_normalAnimation;
        m_animation.clip = m_animation.GetClip(animationName);
        m_animation.Play(animationName);
        m_text.colorGradientPreset = isStrong ? m_criticalColorGradient : m_normalColorGradient;
    }

    internal void ApplyPool(ObjectPool<HealValueHUD> textPool)
    {
        m_objectPool ??= textPool;
    }

    /// <summary>
    /// 由療癒浮字動畫結束事件呼叫，將物件送回物件池。
    /// </summary>
    public void ReleaseToPool()
    {
        m_objectPool?.Release(this);
    }
}
