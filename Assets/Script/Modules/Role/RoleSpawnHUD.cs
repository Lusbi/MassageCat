using System;
using BigMath;
using GameCore;
using GameCore.Database;
using GameCore.UI;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

public class RoleSpawnHUD : MonoBehaviour, IInitlization
{
    private const string k_hitAni = "Hit_Bar";
    private const string k_hitBlock = "Hit_Block";
    [Header("UI Components")]
    [SerializeField] private Image m_enemyImage; 
    [SerializeField] [FormerlySerializedAs("m_enemyHealthText")] private TextMeshProUGUI m_enemyFatigueText;
    [SerializeField] [FormerlySerializedAs("m_enemyHealthBar")] private Image m_enemyFatigueBar;
    [SerializeField] private RectTransform m_enemyRect;
    [SerializeField] private Animation m_talkAni;
    [SerializeField] private CardHud m_cardHud;
    private Action m_dieActionCallBack;
    private BigNumber m_maxFatigue; 
    private BigNumber m_curFatigue;

    public void Initlization(Action callBack = null)
    {
        m_curFatigue = 0;
        m_enemyImage.sprite = null;
        m_enemyFatigueText.text = "0";
        m_enemyFatigueBar.fillAmount = 1f;
        m_dieActionCallBack = null;
        m_enemyRect = m_enemyImage.GetComponent<RectTransform>();
        m_talkAni = GetComponent<Animation>();

        m_cardHud.Initlization();
    }

    public void ApplyDieAction(Action dieActionCallBack)
    {
        m_dieActionCallBack = dieActionCallBack;
    }

    public void LoadRoleData(RoleData roleData)
    {
        m_enemyImage.sprite = roleData.EnemyIcon;
        m_enemyImage.fillAmount = 1f;
        m_enemyImage.enabled = m_enemyImage.sprite != null;
        // m_enemyNameText.text = roleData.RoleName;

        m_maxFatigue = roleData.Fatigue;
        m_curFatigue = roleData.Fatigue;
        UpdateFatigueBar(roleData.Fatigue, roleData.Fatigue);

        float rotationY = roleData.DirectionType == DirectionType.Right ? 180f : 0f;
        m_enemyRect.localEulerAngles = new UnityEngine.Vector3(0, rotationY, 0); 
        m_cardHud.Apply(roleData);
    }

    public void ApplyBattleStorageData(BattleStorageData battleStorageData)
    {
        if (Database<RoleData>.TryLoad(battleStorageData.EnemyKey, out var roleData))
        {
            LoadRoleData(roleData);

            m_curFatigue = battleStorageData.RemainHit;
            UpdateFatigueBar(m_curFatigue, m_maxFatigue);
        }

    }

    private void UpdateFatigueBar(BigNumber currentFatigue, BigNumber maxFatigue)
    {
        m_enemyFatigueText.text = $"{m_curFatigue}/{m_maxFatigue}";
        m_enemyFatigueBar.fillAmount = (float)((double)currentFatigue.Value / (double)maxFatigue.Value);
    }

    public void HitHUDUpdate(BigNumber healValue)
    {
        m_curFatigue = m_curFatigue - healValue ;
        if (m_curFatigue < 0)
            m_curFatigue = 0;
        UpdateFatigueBar(m_curFatigue, m_maxFatigue);

        if (m_curFatigue <= 0)
        {
            m_dieActionCallBack?.Invoke();
        }
        Shake();
    }

    public void EnemyDieAnimation(Action callBack)
    {
        TweenSettings tweenSettings = new()
        {
            duration = 0.3f,
            cycleMode = CycleMode.Rewind,
            ease = Ease.Default,
        };
        PrimeTween.Tween.Custom(1f, 0f, tweenSettings, (x) =>
        {
            m_enemyImage.fillAmount = x;
            Shake();
        }).OnComplete(() => { callBack?.Invoke(); });
    }

    private void Shake()
    {
        ShakeSettings shakeSettings = new()
        {
            duration = 0.1f,
            strength = UnityEngine.Random.insideUnitSphere * 5,
            cycles = 1,
            easeBetweenShakes = Ease.OutSine,
            frequency = 3,
        };
        PrimeTween.Tween.PunchLocalPosition(m_enemyRect, shakeSettings);
    }

    public void PlayHitAnimation()
    {
        if (m_talkAni)
        {
            m_talkAni.Rewind();
            m_talkAni.Play(k_hitAni);
        }
    }

    public void PlayBlockHitAnimation()
    {
        if (m_talkAni)
        {
            m_talkAni.Rewind();
            m_talkAni.Play(k_hitBlock);
        }
    }

}
