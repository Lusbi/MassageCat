using UnityEngine;

/// <summary>
/// 共用 Prefab 的紙繪 Sprite 引用，避免執行時載入貼圖或重複建立 Sprite。
/// </summary>
internal static class DesktopPetChibiPaperArt
{
    internal static Sprite StageFrame => MassageCatUiArt.StageFrame;
    internal static Sprite IconButtonFrame => MassageCatUiArt.IconButtonFrame;
    internal static Sprite SettingsIcon => MassageCatUiArt.Settings;
    internal static Sprite GalleryIcon => MassageCatUiArt.GalleryOpen;
    internal static Sprite SpellCard => MassageCatUiArt.SpellCard;
    internal static Sprite TomatoCard => MassageCatUiArt.TomatoCard;
    internal static Sprite SpellIcon => MassageCatUiArt.SpellIcon;
    internal static Sprite TomatoIcon => MassageCatUiArt.TomatoIcon;
    internal static Sprite SettingsPanel => MassageCatUiArt.SettingsPanel;
}
