using System;
using GameCore.Database;
using GameCore.Event;
using UnityEngine;
using UnityEngine.UI;
using BigMath;
using System.Collections.Generic;
using Modules.SystemMessage;
using GameCore.Audio;

public enum BattleModel
{
    Normal,
    Tomato,
    Spell,
}

public enum AutoModel
{
    Off,
    Next,
    OnlyCard,
}

public class BattlePanel : PanelBase
{
    private const string k_enemySpawnAnimation = "SpawnEnemyAnim";
    [SerializeField] private RectTransform m_enemyParent;
    [SerializeField] private Image m_bgImage;
    [SerializeField] private RoleSpawnHUD m_enemySpawn;
    [SerializeField] private Animation m_ani;
    [SerializeField] private TomatoButton m_tomatoBtn;
    [SerializeField] private HitPanel m_hitPanel;
    [SerializeField] private TeachPanel m_teachPanel;
    [SerializeField] private AutoNextButton m_autoNextButton;
    [SerializeField] private SpellButton m_spellButton;
    [SerializeField] private SpellHandler m_spellHandler;
    [SerializeField] private SystemMessageView m_systemMessageView;
    [SerializeField] private NormalHit normalHitTemplate;
    [SerializeField] private CriticleHit criticleHitTemplate;
    [SerializeField] private BgmStateButton m_bgmStateButton;
    [SerializeField] private AudioStateButton m_audioStateButton;

    private List<NormalHit> m_normalHitPoolList = new List<NormalHit>();
    private List<CriticleHit> m_criticleHitPoolList = new List<CriticleHit>();

    private List<NormalHit> m_activeNormalHitList = new List<NormalHit>();
    private List<CriticleHit> m_activeCriticleHitList = new List<CriticleHit>();

    private TomatoManager m_tomatoManager;
    private EventListener m_eventListener;
    private RoleData m_curBattleRole;
    private bool m_isAlive = false;
    private int m_tomatoHitStack = 0;
    private BigNumber m_curHealValue = 0;
    private RoleTalkHandler m_roleTalkHandler;

    private BattleModel m_battleModel = BattleModel.Normal;
    private AutoModel m_autoModel = AutoModel.Off;
    private List<RoleData> m_sortedRoleDatas = new List<RoleData>();
    private bool m_desktopPetMode;
    [SerializeField] private bool m_showBackgroundInDesktopPetMode;

    public event Action OnSpellForceStopped;
    public event Action OnSpellTimeoutStopped;

    public override void Initlization(Action callBack = null)
    {
        base.Initlization(callBack);

        if (m_enemySpawn)
        {
            m_enemySpawn.Initlization();
            m_enemySpawn.ApplyDieAction(DieAction);
        }

        if (m_eventListener == null)
        {
            m_eventListener = new EventListener();
            m_eventListener.active = true;
            m_eventListener.AddListener<SetBattleRoleEvent>(OnSetBattleRoleEvent, true);
            m_eventListener.AddListener<TomatoTriggerEvent>(OnTomatoTriggerEvent, true);
            m_eventListener.AddListener<FlagUpdateEvent>(OnFlagUpdateEvent, true);
            m_eventListener.AddListener<SpellEvent>(OnSpellEvent, true);
        }

        if (m_tomatoBtn)
            m_tomatoBtn.Initlization();

        m_tomatoManager ??= TomatoManager.instance;

        if (m_hitPanel)
            m_hitPanel.Initlization();

        m_normalHitPoolList.Clear();
        m_activeNormalHitList.Clear();
        m_criticleHitPoolList.Clear();
        m_activeCriticleHitList.Clear();

        m_roleTalkHandler = new RoleTalkHandler();

        if (m_autoNextButton)
            m_autoNextButton.Initlization();

        if (m_spellButton)
            m_spellButton.Initlization();

        if (m_spellHandler)
        {
            m_spellHandler.Initlization();
            m_spellHandler.SpellStopped -= OnSpellStopped;
            m_spellHandler.SpellStopped += OnSpellStopped;
        }

        if (m_systemMessageView)
        {
            m_systemMessageView.Initlization();
        }

        if (m_bgmStateButton)
            m_bgmStateButton.Initlization();
            
        if (m_audioStateButton)
            m_audioStateButton.Initlization();
    }


    public void UpdateBattleModel(BattleModel model, SpellStopReason spellStopReason = SpellStopReason.Manual)
    {
        if (m_battleModel == model)
            return;

        m_battleModel = model;
        ReupdateBattleState(spellStopReason);
    }

    public void UpdateAutoModel(AutoModel model)
    {
        if (m_autoModel == model)
            return;

        m_autoModel = model;
    }
    private void ReupdateBattleState(SpellStopReason spellStopReason)
    {
        switch (m_battleModel)
        {
            case BattleModel.Normal:

                if (m_spellHandler)
                    m_spellHandler.SpellStop(spellStopReason);
                if (m_spellButton)
                    m_spellButton.SetEnable(false);
                break;
            case BattleModel.Tomato:
                if (m_spellHandler)
                    m_spellHandler.SpellStop(spellStopReason);

                if (m_spellButton)
                    m_spellButton.SetEnable(false);

                m_autoModel = AutoModel.Off;
                m_tomatoManager.Apply(m_curBattleRole);

                CreateDefaultEnemy();
                break;
            case BattleModel.Spell:

                if (m_spellHandler)
                    m_spellHandler.SpellStart();

                CreateDefaultEnemy();
                if (m_autoNextButton)
                    m_autoNextButton.Stop();
                break;
        }
    }

    private void OnSpellStopped(SpellStopReason reason)
    {
        switch (reason)
        {
            case SpellStopReason.Forced:
                OnSpellForceStopped?.Invoke();
                break;
            case SpellStopReason.Timeout:
                OnSpellTimeoutStopped?.Invoke();
                break;
        }
    }

    private void OnSpellEvent(SpellEvent eventData)
    {
        if (m_tomatoManager.isTomatoTime)
        {
            return;
        }

        UpdateBattleModel(eventData.IsEnabled ? BattleModel.Spell : BattleModel.Normal);
    }

    private void OnFlagUpdateEvent(FlagUpdateEvent eventData)
    {
        TutorialFlowDiagnostics.Log(
            $"BattlePanel 收到旗標事件：key={eventData.flagKey}, hasTeachPanel={m_teachPanel != null}",
            this);

        if (m_teachPanel)
            m_teachPanel.TeachTrigger(eventData.flagKey);
    }

    private void HitValueUpdate()
    {
        m_curHealValue = Formula.GetDefaultHealValue();
        m_curHealValue = Formula.GetBonusHealValue(m_curHealValue, m_curBattleRole, m_battleModel);
        // Debug Bouns
        m_curHealValue *= SettingsManager.instance.setting.debugHealRate;
    }

    private void OnTomatoTriggerEvent(TomatoTriggerEvent eventData)
    {
        TutorialFlowDiagnostics.Log(
            $"BattlePanel 收到蕃茄鐘狀態事件：enable={eventData.enable}, currentBattleModel={m_battleModel}",
            this);

        if (m_tomatoBtn)
            m_tomatoBtn.SetTrigger(eventData.enable);


        if (eventData.enable)
        {
            UpdateBattleModel(BattleModel.Tomato);
        }
        else
        {
            m_tomatoManager.Apply(null);

            BigNumber value = 0;
            for (int i = 0; i < m_tomatoHitStack; i++)
            {
                // value += GetRandomHitValue();
                value += m_curHealValue;
            }

            if (m_enemySpawn)
                m_enemySpawn.HitHUDUpdate(value);

            PlayHit(value.ToString(), true);
            
            PlayCriticleHitEffect();

            UpdateBattleModel(BattleModel.Normal);
        }
        m_tomatoHitStack = 0;
    }

    private void OnSetBattleRoleEvent(SetBattleRoleEvent eventData)
    {
        if (Database<RoleData>.TryLoad(eventData.roleKey, out var data))
        {
            StorageManager.instance.StorageData.ApplyLastBattleStorageData(data.key, data.Fatigue);
            HitValueUpdate();
            m_curBattleRole = data;
            m_enemySpawn.LoadRoleData(data);
            UpdateSceneBackground(data.SceneReference.Load());
            m_roleTalkHandler.Apply(data);
            TomatoTimeHint(data);

            uiGameMainView.ApplyRoleDataToListTop(data);
        }
    }

    private void UpdateSceneBackground(ScenemapData scenemapData)
    {
        if (m_desktopPetMode && m_showBackgroundInDesktopPetMode == false)
            return;

        m_bgImage.SetImage(scenemapData.ImgBgReference).AutoEnable();
    }

    private void OnDestroy()
    {
        if (m_spellHandler)
            m_spellHandler.SpellStopped -= OnSpellStopped;
    }

    public override void ActiveOn()
    {
        bool hasBattleData = string.IsNullOrEmpty(StorageManager.instance.StorageData.BattleStorageData.EnemyKey) == false;
        if (hasBattleData)
        {
            m_enemySpawn.ApplyBattleStorageData(StorageManager.instance.StorageData.BattleStorageData);
        }
        else
        {
            CreateDefaultEnemy();
        }

        BattleManager.instance.Register(Hit);
    }

    public override void ActiveOff()
    {
        BattleManager.instance.Register(null);
    }

    private void CreateDefaultEnemy()
    {
        SettingsManager.instance.setting.defaultEnemyReference.TryLoad(out m_curBattleRole);
        CreateBattleEnemy();
    }

    private void CreateBattleEnemy()
    {
        uiGameMainView.ApplyRoleRect();
        RoleData nextRoleData = null;

        if (m_sortedRoleDatas == null || m_sortedRoleDatas.Count == 0)
        {
            var roleDatas = Database<RoleData>.GetAll();
            m_sortedRoleDatas = new List<RoleData>(roleDatas);
            m_sortedRoleDatas.Sort((a, b) => a.roleSortId.CompareTo(b.roleSortId));
        }

        bool isAutoNext = m_autoNextButton && m_autoNextButton.isAutoNext;
        bool isSpelling = m_spellHandler && m_spellHandler.isSpelling;
        if (isAutoNext || isSpelling)
        {
            foreach (var roleData in m_sortedRoleDatas)
            {
                if (roleData.ValidateFlagReferenceConditions() && roleData.roleSortId > m_curBattleRole.roleSortId)
                {
                    nextRoleData = roleData;
                    break;
                }
            }
        }
        else
        {
            nextRoleData = m_curBattleRole;
        }
        if (m_spellHandler && m_spellHandler.isSpelling && nextRoleData != null && m_curBattleRole != null && nextRoleData.roleSortId > m_curBattleRole.roleSortId)
        {
            var enemyStorageData = StorageManager.instance.StorageData.GetEnemyStorageData(nextRoleData.key);
            bool hasSilverClear = enemyStorageData != null && enemyStorageData.silverClear;
            if (!hasSilverClear)
            {
                UpdateBattleModel(BattleModel.Normal, SpellStopReason.Forced);
            }
        }

        if (nextRoleData == null)
            nextRoleData = m_curBattleRole;

        OnSetBattleRoleEvent(new SetBattleRoleEvent(nextRoleData.key));
        PlayAnimation(k_enemySpawnAnimation, () => m_isAlive = true);
        PlayBgm();

        PlaySFX(AudioClipType.BossAppearSFX);
    }

    private void PlayBgm()
    {
        BgmClipType bgmClipType = BgmClipType.MainTheme;
        switch (m_battleModel)
        {
            case BattleModel.Normal:
                if (m_curBattleRole.EnemyType == EnemyType.Boss)
                {
                    bgmClipType = BgmClipType.BossTheme;
                }
                else if (m_curBattleRole.EnemyType == EnemyType.MiniBoss)
                {
                    bgmClipType = BgmClipType.EliteEnemyTheme;
                }
                else
                {
                    bgmClipType = BgmClipType.BaseEnemyTheme;
                }
                break;
            case BattleModel.Tomato:
                bgmClipType = BgmClipType.TomatoModeTheme;
                break;
            case BattleModel.Spell:
                bgmClipType = BgmClipType.SpellTheme;
                break;
        }
        AudioClipManager.instance.PlayBGM(bgmClipType, 2.0f);
    }

    private void DieAction()
    {
        m_isAlive = false;

        TutorialFlowDiagnostics.Log(
            $"敵人擊倒：roleKey={m_curBattleRole?.key}, battleModel={m_battleModel}",
            this);

        PlaySFX(AudioClipType.SatisfySFX);

        StorageManager.instance.StorageData.AddEnemySatisfyCount(m_curBattleRole.key, m_battleModel);
        if (m_curBattleRole.SatisfyToAddFlagReference != null)
        {
            string flagKey = m_curBattleRole.SatisfyToAddFlagReference.GetKey();
            TutorialFlowDiagnostics.Log(
                $"敵人擊倒準備增加旗標：roleKey={m_curBattleRole.key}, flagKey={flagKey}",
                this);
            StorageManager.instance.StorageData.AddFlagStorageValue(flagKey);
        }
        else
        {
            TutorialFlowDiagnostics.LogWarning(
                $"敵人擊倒但未設定增加旗標：roleKey={m_curBattleRole.key}",
                this);
        }

        TalkManager.instance.Clear();
        if (m_battleModel == BattleModel.Normal)
        {
            float cardRate = Formula.GetCardDropRate(m_curBattleRole, m_battleModel);
            float randomValue = UnityEngine.Random.Range(0f, 100f);
            bool isGetCard = randomValue <= cardRate;
            if (isGetCard)
            {
                StorageManager.instance.StorageData.AddCardStorageData(m_curBattleRole.key);
                var flagKey = SettingsManager.instance.setting.defaultCardFlag.GetKey();
                if (StorageManager.instance.StorageData.GetFlagStorageValue(flagKey) == 0)
                {
                    StorageManager.instance.StorageData.AddFlagStorageValue(flagKey);
                }

                PlaySFX(AudioClipType.GetCardSFX);
            }
        }
        EnemyDieAnimation(CreateBattleEnemy);
    }
    public void Hit(string hit)
    {
        if (active == false)
            return;

        switch (m_battleModel)
        {
            case BattleModel.Normal:
                NormalHit(hit);
                break;
            case BattleModel.Tomato:
                TomatoHit(hit);
                break;
            case BattleModel.Spell:
                SpellHit(hit);
                break;
        }
    }

    private void SpellHit(string hit)
    {
        if (m_enemySpawn && m_spellHandler)
        {
            bool spellInputChar = m_spellHandler.InputChar(hit);
            if (m_isAlive == false)
                return;

            if (spellInputChar)
            {
                PrimeTween.Tween.Delay(0.15f, () =>
                {
                    BigNumber value = m_curHealValue * SettingsManager.instance.setting.defaultSpellHeal;
                    m_enemySpawn.HitHUDUpdate(value);
                    PlayHit(value.ToString());
                });

            }
        }
    }

    private void TomatoHit(string hit)
    {
        if (m_isAlive == false)
            return;

        m_tomatoHitStack++;
        m_enemySpawn.PlayBlockHitAnimation();
    }

    private void NormalHit(string hit)
    {
        if (m_isAlive == false)
            return;

        if (m_enemySpawn)
        {
            m_enemySpawn.HitHUDUpdate(m_curHealValue);
            m_enemySpawn.PlayHitAnimation();
            PlayHit(m_curHealValue.ToString());
            
            PlayNormalHitEffect();
        }
    }

    public void PlayHit(string hitValue, bool isTomato = false)
    {
        if (m_hitPanel)
            m_hitPanel.PlayHit(hitValue, isTomato);

        if (isTomato)
            PlaySFX(AudioClipType.CriticleSFX);
        else
            PlaySFX(AudioClipType.HealSFX);
    }

    public void TomatoTimeHint(RoleData roleData)
    {
        if (m_tomatoBtn)
            m_tomatoBtn.TomatoTimeHint(roleData);
    }


    private void EnemyDieAnimation(Action callBack = null)
    {
        m_enemySpawn.EnemyDieAnimation(callBack);
    }

    private void PlayAnimation(string ClipName, Action callBack = null)
    {
        AnimationClip clip = m_ani.GetClip(ClipName);
        m_ani.Play(ClipName);
        PrimeTween.Tween.Delay(clip.length, () =>
        {
            callBack?.Invoke();
        });
    }

    public override void Tick(float deltaTime)
    {
        if (m_roleTalkHandler != null)
            m_roleTalkHandler.Tick(deltaTime);

        switch (m_battleModel)
        {
            case BattleModel.Normal:
                break;
            case BattleModel.Tomato:
                if (m_tomatoBtn)
                    m_tomatoBtn.ApplyTomatoTime();
                break;
            case BattleModel.Spell:
                if (m_spellHandler)
                    m_spellHandler.Tick(deltaTime);
                break;
        }
    }

    private void PlaySFX(AudioClipType clipType)
    {
        AudioClipManager.instance.PlaySFX(clipType, 1f);
    }

    private void PlayCriticleHitEffect()
    {
        if (m_criticleHitPoolList.Count > 0)
        {
            int lastIndex = m_criticleHitPoolList.Count - 1;
            var criticleHit = m_criticleHitPoolList[lastIndex];
            m_criticleHitPoolList.RemoveAt(lastIndex);
            m_activeCriticleHitList.Add(criticleHit);
            criticleHit.gameObject.SetActive(true);
            criticleHit.transform.localPosition = UnityEngine.Random.insideUnitCircle * 50f;
            criticleHit.Play(() =>
            {
                m_activeCriticleHitList.Remove(criticleHit);
                m_criticleHitPoolList.Add(criticleHit);
            });
        }
        else
        {
            var criticleHit = Instantiate(criticleHitTemplate, m_hitPanel.transform);
            m_activeCriticleHitList.Add(criticleHit);
            criticleHit.Play(() =>
            {
                criticleHit.gameObject.SetActive(false);
                m_activeCriticleHitList.Remove(criticleHit);
                m_criticleHitPoolList.Add(criticleHit);
            });
        }
    }
    private void PlayNormalHitEffect()
    {
        if (m_normalHitPoolList.Count > 0)
        {
            int lastIndex = m_normalHitPoolList.Count - 1;
            var normalHit = m_normalHitPoolList[lastIndex];
            m_normalHitPoolList.RemoveAt(lastIndex);
            m_activeNormalHitList.Add(normalHit);
            normalHit.transform.localPosition = UnityEngine.Random.insideUnitCircle * 50f;
            normalHit.Play(() =>
            {
                m_activeNormalHitList.Remove(normalHit);
                m_normalHitPoolList.Add(normalHit);
            });
        }
        else
        {
            var normalHit = Instantiate(normalHitTemplate, m_hitPanel.transform);
            m_activeNormalHitList.Add(normalHit);
            normalHit.Play(() =>
            {
                m_activeNormalHitList.Remove(normalHit);
                m_normalHitPoolList.Add(normalHit);
            });
        }
    }
    public void ApplyDesktopPetMode()
    {
        m_desktopPetMode = true;

        if (m_bgImage && m_showBackgroundInDesktopPetMode == false)
            m_bgImage.enabled = false;

        SetDesktopHidden(m_autoNextButton);
        SetDesktopHidden(m_spellButton);
        SetDesktopHidden(m_spellHandler);
        SetDesktopHidden(m_bgmStateButton);
        SetDesktopHidden(m_audioStateButton);
    }

    private static void SetDesktopHidden(Component component)
    {
        if (component)
            component.gameObject.SetActive(false);
    }
}
