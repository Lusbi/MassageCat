using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 重用既有圖示與內容，將左右角落的模式按鈕展開成底部面板。
/// </summary>
internal sealed class DesktopPetModeDock : MonoBehaviour
{
    private const float k_collapsedWidth = 64f;
    private const float k_expandedWidth = 480f;
    private const float k_transitionDuration = 0.22f;

    [SerializeField] private RectTransform m_tomatoRoot;
    [SerializeField] private RectTransform m_spellRoot;
    [SerializeField] private GameObject m_tomatoCard;
    [SerializeField] private GameObject m_spellCard;
    [SerializeField] private RectTransform m_tomatoIcon;
    [SerializeField] private RectTransform m_spellIcon;
    [SerializeField] private TextMeshProUGUI m_tomatoTime;
    [SerializeField] private RectTransform m_spellChallenge;
    [SerializeField] private RectTransform m_spellPrompt;
    [SerializeField] private RectTransform m_spellTimer;
    [SerializeField] private TextMeshProUGUI m_questText;
    [SerializeField] private TextMeshProUGUI m_translationText;
    [SerializeField] private TextMeshProUGUI m_spellTime;

    private GameObject m_tomatoDetails;
    private CanvasGroup m_spellDetails;
    private bool m_layoutReady;

    private BattleModel m_model;
    private float m_elapsed;
    private float m_tomatoStartWidth;
    private float m_spellStartWidth;
    private bool m_isAnimating;

    internal void Apply(BattleModel model, bool immediate = false)
    {
        if (!m_tomatoRoot || !m_spellRoot)
            return;

        ConfigureLayout();

        m_model = model;
        m_tomatoStartWidth = m_tomatoRoot.sizeDelta.x;
        m_spellStartWidth = m_spellRoot.sizeDelta.x;
        m_elapsed = 0f;
        m_isAnimating = !immediate;
        m_tomatoRoot.gameObject.SetActive(model != BattleModel.Spell);
        m_spellRoot.gameObject.SetActive(model != BattleModel.Tomato);
        // 底板隨容器收合與展開，另一模式由父物件控制隱藏。
        m_tomatoCard.SetActive(true);
        m_spellCard.SetActive(true);
        // 展開完成再顯示計時內容，避免小尺寸過程中文字相互覆蓋。
        m_tomatoDetails.SetActive(immediate && model == BattleModel.Tomato);
        if (m_spellDetails)
            m_spellDetails.alpha = immediate && model == BattleModel.Spell ? 1f : 0f;
        ApplyWidth(immediate ? 1f : 0f);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[Codex驗證] 底部模式面板狀態已更新；請確認兩個按鈕圖像與收合底板可見，展開後倒數正常。", this);
#endif
    }

    private void ConfigureLayout()
    {
        if (m_layoutReady)
            return;

        SetRect(m_tomatoRoot, new Vector2(1f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(64f, 88f));
        SetRect(m_spellRoot, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(64f, 88f));
        SetRect(m_tomatoIcon, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(32f, 0f), new Vector2(64f, 64f));
        SetRect(m_spellIcon, new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-32f, 0f), new Vector2(64f, 64f));
        StretchIconChildren(m_tomatoIcon);
        StretchIconChildren(m_spellIcon);
        ConfigureCard(m_tomatoCard, m_tomatoRoot);
        ConfigureCard(m_spellCard, m_spellRoot);
        m_tomatoIcon.SetAsLastSibling();
        m_spellIcon.SetAsLastSibling();

        SetRect(m_tomatoTime.rectTransform, Vector2.up, Vector2.up, new Vector2(108f, -16f), new Vector2(150f, 30f));
        ConfigureText(m_tomatoTime, 24f);
        m_tomatoDetails = m_tomatoTime.gameObject;

        // 只在初始化時重用既有文字樣式建立狀態與提示，模式切換時不再建立 UI。
        TextMeshProUGUI status = Instantiate(m_tomatoTime, m_tomatoRoot);
        TextMeshProUGUI hint = Instantiate(m_tomatoTime, m_tomatoRoot);
        status.name = "TomatoStatus";
        hint.name = "TomatoHint";
        status.rectTransform.SetParent(m_tomatoTime.transform, false);
        hint.rectTransform.SetParent(m_tomatoTime.transform, false);
        SetRect(status.rectTransform, Vector2.up, Vector2.up, new Vector2(164f, 0f), new Vector2(126f, 30f));
        SetRect(hint.rectTransform, Vector2.up, Vector2.up, new Vector2(0f, -34f), new Vector2(290f, 22f));
        ConfigureText(status, 18f);
        ConfigureText(hint, 12f);
        status.SetText("專注中");
        hint.SetText("專注期間累積療癒，倒數結束後結算。");

        Stretch(m_spellChallenge, Vector2.zero);
        m_spellDetails = m_spellChallenge.GetComponent<CanvasGroup>();
        if (!m_spellDetails)
            m_spellDetails = m_spellChallenge.gameObject.AddComponent<CanvasGroup>();
        m_spellDetails.blocksRaycasts = false;
        SetRect(m_spellPrompt, Vector2.up, Vector2.up, new Vector2(52f, -16f), new Vector2(290f, 30f));
        SetRect(m_questText.rectTransform, Vector2.up, Vector2.up, Vector2.zero, new Vector2(180f, 30f));
        SetRect(m_translationText.rectTransform, Vector2.up, Vector2.up, new Vector2(190f, 0f), new Vector2(100f, 30f));
        ConfigureText(m_questText, 28f);
        ConfigureText(m_translationText, 22f);
        m_questText.enableAutoSizing = true;
        m_questText.fontSizeMin = 16f;
        m_questText.fontSizeMax = 28f;
        m_translationText.enableAutoSizing = true;
        m_translationText.fontSizeMin = 16f;
        m_translationText.fontSizeMax = 22f;
        SetRect(m_spellTimer, Vector2.zero, Vector2.zero, new Vector2(52f, 16f), new Vector2(290f, 22f));
        m_spellTime.rectTransform.SetParent(m_spellTimer, false);
        Stretch(m_spellTime.rectTransform, Vector2.zero);
        m_spellTime.gameObject.SetActive(true);
        ConfigureText(m_spellTime, 16f);
        m_layoutReady = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[Codex驗證] 新紙張底板已套用貼邊延展；請確認番茄鐘與拼字展開到底部全寬，深色文字清楚可讀。", this);
#endif
    }

    private static void ConfigureText(TextMeshProUGUI text, float size)
    {
        text.fontSize = size;
        text.enableAutoSizing = false;
        text.margin = Vector4.zero;
        text.fontStyle = FontStyles.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.color = new Color32(42, 28, 18, 255);
        text.enableVertexGradient = false;
        text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.MidlineLeft;
    }

    private static void ConfigureCard(GameObject card, RectTransform parent)
    {
        RectTransform rect = (RectTransform)card.transform;
        rect.SetParent(parent, false);
        rect.SetAsFirstSibling();
        Stretch(rect, Vector2.zero);
        // 底板必須填滿文字使用的範圍，避免保留圖片比例後只剩中央一小塊。
        Image background = card.GetComponent<Image>();
        if (background)
        {
            background.preserveAspect = false;
            background.type = Image.Type.Sliced;
            background.pixelsPerUnitMultiplier = 4f;
        }
    }

    private static void StretchIconChildren(RectTransform icon)
    {
        for (int i = 0; i < icon.childCount; i++)
        {
            if (icon.GetChild(i) is RectTransform child)
                Stretch(child, Vector2.zero);
        }
    }

    private static void Stretch(RectTransform rect, Vector2 size)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private void Update()
    {
        if (!m_isAnimating)
            return;

        m_elapsed += Time.unscaledDeltaTime;
        float progress = Mathf.Clamp01(m_elapsed / k_transitionDuration);
        float eased = 1f - (1f - progress) * (1f - progress) * (1f - progress);
        ApplyWidth(eased);
        if (progress >= 1f)
        {
            m_isAnimating = false;
            m_tomatoDetails.SetActive(m_model == BattleModel.Tomato);
            if (m_spellDetails)
                m_spellDetails.alpha = m_model == BattleModel.Spell ? 1f : 0f;
        }
    }

    private void ApplyWidth(float progress)
    {
        float tomatoWidth = m_model == BattleModel.Tomato ? k_expandedWidth : k_collapsedWidth;
        float spellWidth = m_model == BattleModel.Spell ? k_expandedWidth : k_collapsedWidth;
        m_tomatoRoot.sizeDelta = new Vector2(Mathf.Lerp(m_tomatoStartWidth, tomatoWidth, progress), 88f);
        m_spellRoot.sizeDelta = new Vector2(Mathf.Lerp(m_spellStartWidth, spellWidth, progress), 88f);
    }
}
