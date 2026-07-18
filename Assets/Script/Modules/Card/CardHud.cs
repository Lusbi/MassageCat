using System;
using GameCore;
using GameCore.Database;
using GameCore.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardHud : MonoBehaviour , IInitlization
{
    
    [SerializeField] private Image m_cardImg;
    [SerializeField] private UIButton m_cardBtn;
    [SerializeField] private TextMeshProUGUI m_cardDescText;
    [SerializeField] private GameObject m_descParent;
    [SerializeField] private CanvasGroup m_cardDescCanvasGroup;


    private bool m_hasCardValue;
    public void Initlization(Action callBack = null)
    {
        m_cardImg.enabled = false;
        m_hasCardValue = false;
        m_cardDescCanvasGroup.alpha = 0f;
        m_descParent.SetActive(false);
    }

    private void CardHover(UIButton button, bool isEnter)
    {
        m_descParent.gameObject.SetActive(isEnter);
    }

    public void Apply(RoleData roleData)
    {
        string cardFlagKey = SettingsManager.instance.setting.defaultCardFlag.GetKey();
        int flagCount = StorageManager.instance.StorageData.GetFlagStorageValue(cardFlagKey);
        
        m_cardImg.enabled = flagCount > 0;
        m_cardDescCanvasGroup.alpha = m_cardImg.enabled ? 1f : 0f;
        m_cardDescCanvasGroup.blocksRaycasts = m_cardImg.enabled;
        var enemyStorageData = StorageManager.instance.StorageData.GetEnemyStorageData(roleData.key);
        if (enemyStorageData == null)
        {
            m_cardImg.color = Color.gray;
            m_cardBtn.onHover = null;
            return;
        }
        
        m_cardDescText.text = enemyStorageData.CardReward ? roleData.GetCardLocalization() : string.Empty;
        m_cardImg.color = enemyStorageData.CardReward == false ? Color.gray : Color.white;
        m_hasCardValue = enemyStorageData.CardReward;
        m_cardBtn.onHover = CardHover;
        m_descParent.gameObject.SetActive(false);
    }

}
