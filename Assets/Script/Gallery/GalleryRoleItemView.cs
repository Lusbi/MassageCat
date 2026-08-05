using System;
using GameCore.Database;
using GameCore.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MassageCat.Gallery
{
    public sealed class GalleryRoleItemView : MonoBehaviour
    {
        private static readonly Color s_normalColor = new Color(0.13f, 0.15f, 0.19f, 1f);
        private static readonly Color s_selectedColor = new Color(0.18f, 0.42f, 0.55f, 1f);
        private static readonly Color s_completeColor = new Color(0.26f, 0.68f, 0.42f, 1f);
        private static readonly Color s_incompleteColor = new Color(0.42f, 0.42f, 0.46f, 1f);

        private Image m_background;
        private Image m_roleIcon;
        private TextMeshProUGUI m_roleName;
        private TextMeshProUGUI m_cardBadge;
        private TextMeshProUGUI m_tomatoBadge;
        private Button m_button;
        private GalleryRoleEntry m_entry;
        private Action<GalleryRoleEntry> m_onClicked;
        private bool m_built;

        internal void Bind(
            GalleryRoleEntry entry,
            string selectedRoleKey,
            TMP_FontAsset fontAsset,
            Action<GalleryRoleEntry> onClicked)
        {
            EnsureBuilt(fontAsset);
            m_entry = entry;
            m_onClicked = onClicked;

            RoleData roleData = entry.RoleData;
            m_roleIcon.sprite = roleData.EnemyIcon;
            m_roleIcon.enabled = m_roleIcon.sprite != null;
            m_roleName.SetText(roleData.RoleName);
            m_cardBadge.SetText(entry.CardReward ? "卡✓" : "缺卡");
            m_cardBadge.color = entry.CardReward ? s_completeColor : Color.white;
            m_tomatoBadge.SetText(entry.TomatoClear ? "番✓" : "番—");
            m_tomatoBadge.color = entry.TomatoClear ? s_completeColor : s_incompleteColor;
            ApplyButtonColors(string.Equals(roleData.key, selectedRoleKey, StringComparison.Ordinal));
        }

        private void EnsureBuilt(TMP_FontAsset fontAsset)
        {
            if (m_built)
                return;

            m_built = true;
            RectTransform root = (RectTransform)transform;
            root.sizeDelta = new Vector2(132f, 112f);

            m_background = gameObject.AddComponent<Image>();
            m_background.color = s_normalColor;
            m_button = gameObject.AddComponent<Button>();
            m_button.targetGraphic = m_background;
            m_button.onClick.AddListener(OnClicked);
            gameObject.AddComponent<UIButton>();

            m_roleIcon = CreateImage("RoleIcon", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            RectTransform iconRect = m_roleIcon.rectTransform;
            iconRect.pivot = new Vector2(0.5f, 1f);
            iconRect.anchoredPosition = new Vector2(0f, -6f);
            iconRect.sizeDelta = new Vector2(68f, 68f);
            m_roleIcon.preserveAspect = true;
            m_roleIcon.raycastTarget = false;

            m_roleName = CreateText("RoleName", root, fontAsset, 15f, TextAlignmentOptions.Center);
            RectTransform nameRect = m_roleName.rectTransform;
            nameRect.anchorMin = new Vector2(0f, 0f);
            nameRect.anchorMax = new Vector2(1f, 0f);
            nameRect.pivot = new Vector2(0.5f, 0f);
            nameRect.offsetMin = new Vector2(4f, 22f);
            nameRect.offsetMax = new Vector2(-4f, 42f);

            m_cardBadge = CreateText("CardBadge", root, fontAsset, 12f, TextAlignmentOptions.Center);
            RectTransform cardRect = m_cardBadge.rectTransform;
            cardRect.anchorMin = new Vector2(0f, 0f);
            cardRect.anchorMax = new Vector2(0.5f, 0f);
            cardRect.offsetMin = new Vector2(4f, 3f);
            cardRect.offsetMax = new Vector2(-2f, 21f);

            m_tomatoBadge = CreateText("TomatoBadge", root, fontAsset, 12f, TextAlignmentOptions.Center);
            RectTransform tomatoRect = m_tomatoBadge.rectTransform;
            tomatoRect.anchorMin = new Vector2(0.5f, 0f);
            tomatoRect.anchorMax = new Vector2(1f, 0f);
            tomatoRect.offsetMin = new Vector2(2f, 3f);
            tomatoRect.offsetMax = new Vector2(-4f, 21f);
        }

        private void OnClicked()
        {
            if (m_entry != null)
                m_onClicked?.Invoke(m_entry);
        }

        private void ApplyButtonColors(bool isSelected)
        {
            Color normalColor = isSelected ? s_selectedColor : s_normalColor;
            ColorBlock colors = m_button.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = Color.Lerp(normalColor, Color.white, 0.16f);
            colors.pressedColor = Color.Lerp(normalColor, Color.black, 0.25f);
            colors.selectedColor = s_selectedColor;
            colors.disabledColor = new Color(normalColor.r, normalColor.g, normalColor.b, 0.55f);
            colors.fadeDuration = 0.08f;
            m_button.colors = colors;
            m_background.color = normalColor;
        }

        private static Image CreateImage(string objectName, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject gameObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            RectTransform rectTransform = (RectTransform)gameObject.transform;
            rectTransform.SetParent(parent, false);
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            return gameObject.GetComponent<Image>();
        }

        private static TextMeshProUGUI CreateText(
            string objectName,
            Transform parent,
            TMP_FontAsset fontAsset,
            float fontSize,
            TextAlignmentOptions alignment)
        {
            GameObject gameObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform rectTransform = (RectTransform)gameObject.transform;
            rectTransform.SetParent(parent, false);
            TextMeshProUGUI text = gameObject.GetComponent<TextMeshProUGUI>();
            if (fontAsset != null)
                text.font = fontAsset;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }
    }
}
