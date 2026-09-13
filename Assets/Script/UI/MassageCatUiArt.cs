using UnityEngine;
using UnityEngine.UI;

internal sealed class MassageCatUiArt : MonoBehaviour
{
    [SerializeField] private Sprite m_galleryPanel;
    [SerializeField] private Sprite m_settingsPanel;
    [SerializeField] private Sprite m_galleryOpen;
    [SerializeField] private Sprite m_galleryClose;
    [SerializeField] private Sprite m_settings;
    [SerializeField] private Sprite m_sceneVisible;
    [SerializeField] private Sprite m_sceneHidden;
    [SerializeField] private Sprite m_iconButtonFrame;
    [SerializeField] private Sprite m_bgmOn;
    [SerializeField] private Sprite m_bgmOff;
    [SerializeField] private Sprite m_sfxOn;
    [SerializeField] private Sprite m_sfxOff;
    [SerializeField] private Sprite m_minimize;
    [SerializeField] private Sprite m_quit;
    [SerializeField] private Sprite m_autoNextOn;
    [SerializeField] private Sprite m_autoNextOff;
    [SerializeField] private Sprite m_stageFrame;
    [SerializeField] private Sprite m_spellCard;
    [SerializeField] private Sprite m_tomatoCard;
    [SerializeField] private Sprite m_spellIcon;
    [SerializeField] private Sprite m_tomatoIcon;

    private static MassageCatUiArt s_instance;

    internal static Sprite GalleryPanel => GetInstance()?.m_galleryPanel;
    internal static Sprite SettingsPanel => GetInstance()?.m_settingsPanel;
    internal static Sprite GalleryOpen => GetInstance()?.m_galleryOpen;
    internal static Sprite GalleryClose => GetInstance()?.m_galleryClose;
    internal static Sprite Settings => GetInstance()?.m_settings;
    internal static Sprite SceneVisible => GetInstance()?.m_sceneVisible;
    internal static Sprite SceneHidden => GetInstance()?.m_sceneHidden;
    internal static Sprite IconButtonFrame => GetInstance()?.m_iconButtonFrame;
    internal static Sprite BgmOn => GetInstance()?.m_bgmOn;
    internal static Sprite BgmOff => GetInstance()?.m_bgmOff;
    internal static Sprite SfxOn => GetInstance()?.m_sfxOn;
    internal static Sprite SfxOff => GetInstance()?.m_sfxOff;
    internal static Sprite Minimize => GetInstance()?.m_minimize;
    internal static Sprite Quit => GetInstance()?.m_quit;
    internal static Sprite AutoNextOn => GetInstance()?.m_autoNextOn;
    internal static Sprite AutoNextOff => GetInstance()?.m_autoNextOff;
    internal static Sprite StageFrame => GetInstance()?.m_stageFrame;
    internal static Sprite SpellCard => GetInstance()?.m_spellCard;
    internal static Sprite TomatoCard => GetInstance()?.m_tomatoCard;
    internal static Sprite SpellIcon => GetInstance()?.m_spellIcon;
    internal static Sprite TomatoIcon => GetInstance()?.m_tomatoIcon;
    internal static Sprite DesktopPetSettingsIcon => DesktopPetChibiPaperArt.SettingsIcon;
    internal static Sprite DesktopPetGalleryIcon => DesktopPetChibiPaperArt.GalleryIcon;

    private void Awake()
    {
        s_instance = this;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[Codex驗證] 請確認縮小匯入後的設定、圖鑑、拼字、番茄鐘與資訊圖示清晰，並確認進度條與面板維持原有外觀。", this);
        Debug.Log(m_settingsPanel != null && m_galleryPanel != null && m_settings != null && m_galleryOpen != null && m_stageFrame != null && m_spellCard != null && m_tomatoCard != null && m_spellIcon != null && m_tomatoIcon != null ? "[Codex驗證] 紙繪主要面板、圖示與卡片引用已綁定；請確認實際顯示風格。" : "[Codex驗證] 紙繪主要素材引用有缺漏，請檢查 MassageCatUiArt。", this);
#endif
    }

    private void OnDestroy()
    {
        if (s_instance == this)
            s_instance = null;
    }

    private static MassageCatUiArt GetInstance()
    {
        if (s_instance == null)
        {
            // UI 預載物件可能尚未啟用；仍需取得其序列化的包版圖示引用。
            s_instance = FindFirstObjectByType<MassageCatUiArt>(FindObjectsInactive.Include);
        }

        return s_instance;
    }

    internal static void ApplyIconOutline(Graphic graphic)
    {
        if (graphic == null)
            return;

        Outline outline = graphic.GetComponent<Outline>();
        if (outline == null)
            outline = graphic.gameObject.AddComponent<Outline>();

        outline.effectColor = new Color32(61, 31, 16, 235);
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = true;
    }

}
