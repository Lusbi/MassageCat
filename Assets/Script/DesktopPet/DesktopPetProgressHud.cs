using GameCore.Database;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 桌面寵物緊湊介面的總完成度與卡片蒐集徽章。
/// </summary>
internal sealed class DesktopPetProgressHud : MonoBehaviour
{
    private const float k_refreshInterval = 1f;

    private TextMeshProUGUI m_starText;
    private TextMeshProUGUI m_cardText;
    private float m_nextRefreshTime;
    private int m_lastStarCount = -1;
    private int m_lastCardCount = -1;
    private int m_totalRoleCount;

    internal static DesktopPetProgressHud CreateRuntime(Transform parent)
    {
        GameObject rootObject = new GameObject("DesktopPetProgressHud", typeof(RectTransform), typeof(DesktopPetProgressHud));
        rootObject.layer = parent.gameObject.layer;
        RectTransform root = rootObject.GetComponent<RectTransform>();
        root.SetParent(parent, false);
        root.anchorMin = new Vector2(0f, 1f);
        root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.anchoredPosition = new Vector2(16f, -68f);
        root.sizeDelta = new Vector2(80f, 42f);
        root.SetAsLastSibling();

        Image background = rootObject.AddComponent<Image>();
        background.sprite = null;
        background.type = Image.Type.Simple;
        background.color = new Color32(255, 246, 221, 245);
        background.raycastTarget = false;

        DesktopPetProgressHud hud = rootObject.GetComponent<DesktopPetProgressHud>();
        Color textColor = new Color32(63, 67, 109, 255);
        hud.m_starText = CreateBadgeText("StarBadge", root, new Vector2(0f, 21f), textColor);
        hud.m_cardText = CreateBadgeText("CardBadge", root, Vector2.zero, textColor);
        return hud;
    }

    private static TextMeshProUGUI CreateBadgeText(string objectName, RectTransform parent, Vector2 position, Color color)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.layer = parent.gameObject.layer;
        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        rectTransform.anchorMin = new Vector2(0f, 0f);
        rectTransform.anchorMax = new Vector2(0f, 0f);
        rectTransform.pivot = new Vector2(0f, 0f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(80f, 21f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = 11f;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    private void Update()
    {
        if (Time.unscaledTime < m_nextRefreshTime)
            return;

        m_nextRefreshTime = Time.unscaledTime + k_refreshInterval;
        Refresh();
    }

    internal void Refresh()
    {
        if (StorageManager.instance == null || StorageManager.instance.StorageData == null)
            return;

        if (m_totalRoleCount == 0)
        {
            System.Collections.Generic.Dictionary<string, RoleData>.ValueCollection roleDatas = Database<RoleData>.GetAll();
            m_totalRoleCount = roleDatas.Count;
        }

        if (m_totalRoleCount == 0)
            return;

        int starCount = 0;
        int cardCount = 0;
        System.Collections.Generic.IReadOnlyList<EnemyStorageData> enemyStorageDatas = StorageManager.instance.StorageData.EnemyStorageDatas;
        for (int i = 0; i < enemyStorageDatas.Count; i++)
        {
            EnemyStorageData storageData = enemyStorageDatas[i];
            if (storageData.silverClear)
                starCount++;
            if (storageData.CardReward)
                cardCount++;
        }

        if (starCount != m_lastStarCount)
        {
            m_lastStarCount = starCount;
            m_starText.SetText("STAR {0}", starCount);
        }
        if (cardCount != m_lastCardCount)
        {
            m_lastCardCount = cardCount;
            m_cardText.SetText("CARD {0}/{1}", cardCount, m_totalRoleCount);
        }
    }
}
