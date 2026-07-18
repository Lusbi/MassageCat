using System;
using GameCore;
using GameCore.Audio;
using GameCore.Event;
using GameCore.UI;
using UnityEngine;
using UnityEngine.UI;

public class RoleItemHUD : MonoBehaviour, IInitlization
{
    [SerializeField] private Image m_iconImg;
    [SerializeField] private Image m_signImg;
    [SerializeField] private UIButton m_selectBtn;
    [SerializeField] private HorizontalLayoutGroup m_hGroupLayout;
    [SerializeField] private Image m_silverImg;
    [SerializeField] private Image m_goldenImg;
    [SerializeField] private Image m_speedStarImg;
    [SerializeField] private Material m_cardMaterial;

    [SerializeField] private RoleCardLightAnim m_cardLightAnim;
    private bool m_isNew;
    private RoleInfo m_roleInfo;
    private RectTransform m_hGroupLayoutRect;
    private Material m_defaultMaterial;
    private Material m_cardMaterialInstance;
    void Awake()
    {
        m_defaultMaterial = m_iconImg.material;
        m_cardLightAnim.Apply(null);
    }
    public void Apply(RoleInfo roleInfo)
    {
        if (m_hGroupLayoutRect == null)
            m_hGroupLayoutRect = m_hGroupLayout.GetComponent<RectTransform>();
        m_selectBtn.ResetButtonState();
        m_roleInfo = roleInfo;
        bool locked = roleInfo.roleData.ValidateFlagReferenceConditions();
        var storageData = StorageManager.instance.StorageData.GetEnemyStorageData(roleInfo.roleData.key);
        int satisfyValue = storageData != null ? storageData.SatisfyValue : 0;
        bool isGolden = storageData != null ? storageData.goldenClear : false;
        bool isSilver = storageData != null ? storageData.silverClear : false;
        bool isSpeedClear = storageData != null ? storageData.speedClear : false;
        bool hasCard = storageData != null ? storageData.CardReward : false;
        if (m_iconImg)
        {
            m_iconImg.sprite = m_roleInfo.roleData.EnemyIcon;
            m_iconImg.enabled = m_iconImg.sprite != null;
            if (hasCard)
            {
                if (m_cardMaterialInstance == null)
                {
                    m_cardMaterialInstance = Instantiate(m_cardMaterial);
                }
                m_cardMaterialInstance.SetTexture("_MainTex", m_iconImg.sprite.texture);
            }
            m_iconImg.material = hasCard ? m_cardMaterialInstance : m_defaultMaterial;
            m_cardLightAnim.Apply(hasCard ? m_iconImg : null);
        }

        if (m_signImg)
        {
            m_isNew = satisfyValue == 0;
            m_signImg.enabled = m_isNew && locked;
        }

        m_selectBtn.locked = !locked;
        if (locked == false)
        {
            bool isCurSelectRole = StorageManager.instance.StorageData.BattleStorageData.EnemyKey == roleInfo.roleData.key;
            m_selectBtn.selected = isCurSelectRole;
        }


        if (m_silverImg)
        {
            m_silverImg.gameObject.SetActive(isSilver);
        }
        if (m_goldenImg)
        {
            m_goldenImg.gameObject.SetActive(isGolden);
        }
        if (m_speedStarImg)
        {
            m_speedStarImg.gameObject.SetActive(isSpeedClear);
        }

        // Force immediate layout update
        LayoutRebuilder.ForceRebuildLayoutImmediate(m_hGroupLayoutRect);
    }

    public void Initlization(Action callBack = null)
    {
        if (m_iconImg)
        {
            m_iconImg.enabled = false;
        }
        if (m_signImg)
        {
            m_signImg.enabled = false;
        }
        if (m_selectBtn && m_selectBtn.onClicked == null)
        {
            m_selectBtn.onClicked = OnClick;
        }
    }

    private void OnClick(UIButton button)
    {
        PlaySFX(AudioClipType.Button_ClickSFX);
        UnityMainThreadDispatcher.DelaySetInputListenerState(false, 0.1f);
        EventManager.instance.Notify(new SetBattleRoleEvent(m_roleInfo.roleData.key));
    }

    private void PlaySFX(AudioClipType clipType)
    {
        AudioClipManager.instance.PlaySFX(clipType, 0.5f);
    }
}
