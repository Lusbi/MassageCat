using UnityEngine;
using UnityEngine.UI;

internal static class MassageCatUiArt
{
    private const string k_resourceRoot = "MassageCatUI/";

    private static Sprite s_galleryPanel;
    private static Sprite s_settingsPanel;
    private static Sprite s_galleryOpen;
    private static Sprite s_galleryClose;
    private static Sprite s_settings;
    private static Sprite s_sceneVisible;
    private static Sprite s_sceneHidden;

    internal static Sprite GalleryPanel => Load(ref s_galleryPanel, "GalleryPanel");
    internal static Sprite SettingsPanel => Load(ref s_settingsPanel, "SettingsPanel");
    internal static Sprite GalleryOpen => Load(ref s_galleryOpen, "GalleryOpen");
    internal static Sprite GalleryClose => Load(ref s_galleryClose, "GalleryClose");
    internal static Sprite Settings => Load(ref s_settings, "Settings");
    internal static Sprite SceneVisible => Load(ref s_sceneVisible, "SceneVisible");
    internal static Sprite SceneHidden => Load(ref s_sceneHidden, "SceneHidden");

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

    private static Sprite Load(ref Sprite cachedSprite, string resourceName)
    {
        if (cachedSprite == null)
            cachedSprite = Resources.Load<Sprite>(k_resourceRoot + resourceName);

        return cachedSprite;
    }
}
