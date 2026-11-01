using System;
using GameCore;
using GameCore.Database;
using GameCore.UI;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardHud : MonoBehaviour , IInitlization
{
    private static readonly Vector3 s_cardPulseStrength = new Vector3(0.16f, 0.16f, 0f);

    
    [SerializeField] private Image m_cardImg;
    [SerializeField] private UIButton m_cardBtn;
    [SerializeField] private TextMeshProUGUI m_cardDescText;
    [SerializeField] private GameObject m_descParent;
    [SerializeField] private CanvasGroup m_cardDescCanvasGroup;


    private bool m_hasCardValue;
    private Tween m_cardPulseTween;
    private Vector3 m_cardBaseScale;

    public void Initlization(Action callBack = null)
    {
        if (m_cardImg == null)
            return;

        m_cardImg.enabled = false;
        m_hasCardValue = false;
        m_cardBaseScale = m_cardImg.rectTransform.localScale;
        if (m_cardDescCanvasGroup)
        {
            m_cardDescCanvasGroup.alpha = 0f;
            m_cardDescCanvasGroup.blocksRaycasts = false;
        }
        if (m_descParent)
            m_descParent.SetActive(false);
    }

    private void CardHover(UIButton button, bool isEnter)
    {
        if (m_descParent)
            m_descParent.SetActive(isEnter);
    }

    public void Apply(RoleData roleData)
    {
        Apply(roleData, BattleModel.Normal, false);
    }

    public void Apply(RoleData roleData, BattleModel battleModel, bool playObtainedFeedback = false)
    {
        if (roleData == null || m_cardImg == null)
            return;

        string cardFlagKey = SettingsManager.instance.setting.defaultCardFlag.GetKey();
        int flagCount = StorageManager.instance.StorageData.GetFlagStorageValue(cardFlagKey);
        bool isCardSystemUnlocked = flagCount > 0;
        m_cardImg.enabled = isCardSystemUnlocked;
        if (m_cardDescCanvasGroup)
        {
            m_cardDescCanvasGroup.alpha = isCardSystemUnlocked ? 1f : 0f;
            m_cardDescCanvasGroup.blocksRaycasts = isCardSystemUnlocked;
        }

        var enemyStorageData = StorageManager.instance.StorageData.GetEnemyStorageData(roleData.key);
        bool hasCard = enemyStorageData != null && enemyStorageData.CardReward;
        if (isCardSystemUnlocked == false)
        {
            m_cardBtn.onHover = null;
            if (m_descParent)
                m_descParent.SetActive(false);
            m_hasCardValue = hasCard;
            return;
        }

        if (m_cardDescText)
        {
            if (hasCard)
            {
                m_cardDescText.SetText(roleData.GetCardLocalization());
            }
            else if (battleModel == BattleModel.Normal)
            {
                float cardRate = Formula.GetCardDropRate(roleData, battleModel);
                m_cardDescText.SetText("尚未取得\n目前獲得機率：{0:0.##}%", cardRate);
            }
            else
            {
                // 卡片僅在一般療癒模式的擊倒結算中抽取，其他模式的實際機率為零。
                m_cardDescText.SetText("尚未取得\n目前模式無卡片掉落");
            }
        }

        m_cardImg.color = hasCard ? Color.white : Color.gray;
        bool shouldPlayFeedback = playObtainedFeedback && m_hasCardValue == false && hasCard;
        m_hasCardValue = hasCard;
        m_cardBtn.onHover = CardHover;
        if (m_descParent)
            m_descParent.SetActive(false);

        if (shouldPlayFeedback)
            PlayObtainedFeedback();
    }

    private void PlayObtainedFeedback()
    {
        if (m_cardPulseTween.isAlive)
            m_cardPulseTween.Stop();

        m_cardImg.rectTransform.localScale = m_cardBaseScale;
        m_cardPulseTween = Tween.PunchScale(
            m_cardImg.rectTransform,
            s_cardPulseStrength,
            0.3f,
            frequency: 3f,
            easeBetweenShakes: Ease.OutSine);
    }

    private void OnDisable()
    {
        if (m_cardPulseTween.isAlive)
            m_cardPulseTween.Stop();
    }
}
