using System;
using GameCore.Database;
using GameCore.Event;
using UnityEngine;
using UnityEngine.UI;
using BigMath;
using System.Collections.Generic;
using Modules.SystemMessage;
using GameCore.Audio;
using PrimeTween;
using UnityEngine.Serialization;
using MassageCat.Gallery;

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
    [SerializeField] private RectTransform m_foregroundRoot;
    [SerializeField] private SceneVisualToggleButton m_sceneVisualToggleButton;
    [SerializeField] private RoleSpawnHUD m_enemySpawn;
    [SerializeField] private Animation m_ani;
    [SerializeField] private TomatoButton m_tomatoBtn;
    [SerializeField] private GalleryButton m_galleryButton;
    [FormerlySerializedAs("m_hitPanel")]
    [SerializeField] private HealFeedbackPanel m_healFeedbackPanel;
    [SerializeField] private TeachPanel m_teachPanel;
    [SerializeField] private AutoNextButton m_autoNextButton;
    [SerializeField] private SpellButton m_spellButton;
    [SerializeField] private SpellHandler m_spellHandler;
    [SerializeField] private SystemMessageView m_systemMessageView;
    [FormerlySerializedAs("normalHitTemplate")]
    [SerializeField] private NormalHealEffect m_normalHealEffectTemplate;
    [FormerlySerializedAs("criticleHitTemplate")]
    [SerializeField] private StrongHealEffect m_strongHealEffectTemplate;
    [SerializeField] private BgmStateButton m_bgmStateButton;
    [SerializeField] private AudioStateButton m_audioStateButton;

    private readonly List<NormalHealEffect> m_normalHealEffectPoolList = new List<NormalHealEffect>();
    private readonly List<StrongHealEffect> m_strongHealEffectPoolList = new List<StrongHealEffect>();
    private readonly List<NormalHealEffect> m_activeNormalHealEffectList = new List<NormalHealEffect>();
    private readonly List<StrongHealEffect> m_activeStrongHealEffectList = new List<StrongHealEffect>();

    private TomatoManager m_tomatoManager;
    private EventListener m_eventListener;
    private RoleData m_curBattleRole;
    private bool m_isAlive = false;
    private int m_tomatoHitStack = 0;
    private BigNumber m_curHealValue = 0;
    private RoleTalkHandler m_roleTalkHandler;
    private Action<NormalHealEffect> m_releaseNormalHealEffect;
    private Action<StrongHealEffect> m_releaseStrongHealEffect;

    private BattleModel m_battleModel = BattleModel.Normal;
    private AutoModel m_autoModel = AutoModel.Off;
    private List<RoleData> m_sortedRoleDatas = new List<RoleData>();
    private readonly Dictionary<GameObject, GameObject> m_foregroundInstances = new Dictionary<GameObject, GameObject>(11);
    private bool m_desktopPetMode;
    private bool m_sceneVisualsVisible = true;
    private GameObject m_activeForeground;
    private bool m_isDefeatTransitionActive;

    public event Action OnSpellForceStopped;
    public event Action OnSpellTimeoutStopped;

    internal BattleModel CurrentBattleModel => m_battleModel;

    public override void Initlization(Action callBack = null)
    {
        base.Initlization(callBack);
        EnsureSceneVisualUI();

        if (m_enemySpawn)
        {
            m_enemySpawn.Initlization();
            m_enemySpawn.ApplyDieAction(DieAction);

            if (TalkManager.instance)
                TalkManager.instance.BindRoleTalkText(m_enemySpawn.RoleTalkText);
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

        if (m_healFeedbackPanel)
            m_healFeedbackPanel.Initlization();

        m_normalHealEffectPoolList.Clear();
        m_activeNormalHealEffectList.Clear();
        m_strongHealEffectPoolList.Clear();
        m_activeStrongHealEffectList.Clear();
        m_releaseNormalHealEffect ??= ReleaseNormalHealEffect;
        m_releaseStrongHealEffect ??= ReleaseStrongHealEffect;

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

        if (m_galleryButton)
            m_galleryButton.Initlization();

        if (m_bgmStateButton)
            m_bgmStateButton.Initlization();
            
        if (m_audioStateButton)
            m_audioStateButton.Initlization();

        EnsureGeneralSettingsMenu();

        m_sceneVisualsVisible = true;
        if (m_sceneVisualToggleButton)
            m_sceneVisualToggleButton.Initlization(SetSceneVisualsVisible);
        ApplySceneVisualVisibility();
    }

    private void EnsureGeneralSettingsMenu()
    {
        if (GameProcess.Instance != null && GameProcess.Instance.DesktopPetMode)
            return;

        GameSettingsMenu.CreateRuntime(transform, m_autoNextButton);
        if (m_bgmStateButton)
            m_bgmStateButton.gameObject.SetActive(false);
        if (m_audioStateButton)
            m_audioStateButton.gameObject.SetActive(false);
    }

    private void EnsureSceneVisualUI()
    {
        if (m_foregroundRoot == null)
        {
            GameObject foregroundRootObject = new GameObject("ForegroundRoot", typeof(RectTransform));
            foregroundRootObject.layer = gameObject.layer;
            m_foregroundRoot = foregroundRootObject.GetComponent<RectTransform>();
            m_foregroundRoot.SetParent(transform, false);
            m_foregroundRoot.anchorMin = Vector2.zero;
            m_foregroundRoot.anchorMax = Vector2.one;
            m_foregroundRoot.offsetMin = Vector2.zero;
            m_foregroundRoot.offsetMax = Vector2.zero;
            m_foregroundRoot.pivot = new Vector2(0.5f, 0.5f);

            if (m_bgImage)
                m_bgImage.transform.SetSiblingIndex(0);
            if (m_enemyParent)
                m_enemyParent.SetSiblingIndex(1);
            m_foregroundRoot.SetSiblingIndex(2);
        }

        if (m_sceneVisualToggleButton == null)
            m_sceneVisualToggleButton = SceneVisualToggleButton.CreateRuntime(transform);
    }


    public void UpdateBattleModel(BattleModel model, SpellStopReason spellStopReason = SpellStopReason.Manual)
    {
        if (m_battleModel == model)
            return;

        m_battleModel = model;
        ReupdateBattleState(spellStopReason);
        GalleryStateChangeNotifier.Notify();
    }

    public void UpdateAutoModel(AutoModel model)
    {
        if (m_autoModel == model)
            return;

        m_autoModel = model;
    }
    private void ReupdateBattleState(SpellStopReason spellStopReason)
    {
        UpdateRoleArrowInteraction();

        switch (m_battleModel)
        {
            case BattleModel.Normal:

                if (m_spellHandler && m_spellHandler.isSpelling)
                    m_spellHandler.SpellStop(spellStopReason);
                if (m_spellButton)
                    m_spellButton.SetEnable(false);
                break;
            case BattleModel.Tomato:
                if (m_spellHandler && m_spellHandler.isSpelling)
                    m_spellHandler.SpellStop(spellStopReason);

                if (m_spellButton)
                    m_spellButton.SetEnable(false);

                m_autoModel = AutoModel.Off;
                m_tomatoManager.Apply(m_curBattleRole);
                break;
            case BattleModel.Spell:
                CreateDefaultEnemy();
                // 角色資料套用會清除對話框，拼字題目必須在角色建立後才顯示翻譯。
                if (m_spellHandler)
                    m_spellHandler.SpellStart();

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
                UpdateBattleModel(BattleModel.Normal, SpellStopReason.Timeout);
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

        if (m_desktopPetMode && m_autoNextButton && m_autoNextButton.UsesUnlockFlag(eventData.flagKey))
        {
            TutorialFlowDiagnostics.Log(
                $"Desktop Pet 模式略過自動下一關教學：key={eventData.flagKey}",
                this);
            return;
        }

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

            if (m_enemySpawn && value > 0)
            {
                ApplyHealToEnemy(value);
                PlayHealFeedback(value.ToString(), true);
                PlayStrongHealEffect();
            }

            UpdateBattleModel(BattleModel.Normal);
        }
        m_tomatoHitStack = 0;
    }

    private void OnSetBattleRoleEvent(SetBattleRoleEvent eventData)
    {
        if (Database<RoleData>.TryLoad(eventData.roleKey, out var data))
            ApplyBattleRoleData(data, 0);
    }

    private void ApplyBattleRoleData(RoleData data, BigNumber accumulatedHealValue)
    {
        StorageManager.instance.StorageData.ApplyBattleProgressData(data.key, accumulatedHealValue);
        m_curBattleRole = data;
        HitValueUpdate();
        m_enemySpawn.LoadRoleData(data, m_battleModel);
        m_enemySpawn.ApplyAccumulatedHealValue(accumulatedHealValue);
        UpdateSceneVisuals(data.SceneReference.Load());
        m_roleTalkHandler.Apply(data);
        TomatoTimeHint(data);
        PlayBgm();

        uiGameMainView.ApplyRoleDataToListTop(data);
        GalleryStateChangeNotifier.Notify();
    }

    internal bool TrySelectRoleFromGallery(string roleKey, out string reason)
    {
        if (m_battleModel != BattleModel.Normal)
        {
            reason = "normal_mode_only";
            return false;
        }

        if (string.IsNullOrEmpty(roleKey)
            || Database<RoleData>.TryLoad(roleKey, out RoleData roleData) == false)
        {
            reason = "role_not_found";
            return false;
        }

        if (roleData.ValidateFlagReferenceConditions() == false)
        {
            reason = "role_locked";
            return false;
        }

        OnSetBattleRoleEvent(new SetBattleRoleEvent(roleKey));
        reason = string.Empty;
        return true;
    }

    private void UpdateSceneVisuals(ScenemapData scenemapData)
    {
        if (scenemapData == null)
            return;

        if (m_bgImage)
        {
            m_bgImage.SetImage(scenemapData.ImgBgReference).AutoEnable();
            m_bgImage.raycastTarget = false;
        }

        UpdateSceneForeground(scenemapData.SpawnObjectReference, scenemapData.SceneMapNo);
        ApplySceneVisualVisibility();
    }

    private void UpdateSceneForeground(GameObject foregroundPrefab, int sceneMapNo)
    {
        if (m_activeForeground)
            m_activeForeground.SetActive(false);

        m_activeForeground = null;
        if (foregroundPrefab == null)
        {
            LogMissingForegroundPrefab(sceneMapNo);
            return;
        }

        if (m_foregroundRoot == null)
        {
            LogMissingForegroundRoot(sceneMapNo);
            return;
        }

        if (m_foregroundInstances.TryGetValue(foregroundPrefab, out GameObject foregroundInstance) == false
            || foregroundInstance == null)
        {
            foregroundInstance = Instantiate(foregroundPrefab, m_foregroundRoot, false);
            m_foregroundInstances[foregroundPrefab] = foregroundInstance;

            Image foregroundImage = foregroundInstance.GetComponent<Image>();
            if (foregroundImage == null || foregroundImage.sprite == null)
                LogMissingForegroundSprite(sceneMapNo, foregroundPrefab.name);
        }

        m_activeForeground = foregroundInstance;
        m_activeForeground.SetActive(m_sceneVisualsVisible);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private static void LogMissingForegroundPrefab(int sceneMapNo)
    {
        Debug.LogWarning($"SceneMap_{sceneMapNo:00} 未綁定有效的前景 Prefab。");
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private static void LogMissingForegroundRoot(int sceneMapNo)
    {
        Debug.LogWarning($"SceneMap_{sceneMapNo:00} 無法載入前景：BattlePanel 缺少 ForegroundRoot。");
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private static void LogMissingForegroundSprite(int sceneMapNo, string prefabName)
    {
        Debug.LogWarning($"SceneMap_{sceneMapNo:00} 的前景 Prefab「{prefabName}」缺少 Image Sprite。");
    }

    private void SetSceneVisualsVisible(bool isVisible)
    {
        m_sceneVisualsVisible = isVisible;
        ApplySceneVisualVisibility();
    }

    private void ApplySceneVisualVisibility()
    {
        if (m_bgImage)
            m_bgImage.enabled = m_sceneVisualsVisible;

        if (m_foregroundRoot)
            m_foregroundRoot.gameObject.SetActive(m_sceneVisualsVisible);
    }

    private void OnDestroy()
    {
        if (m_spellHandler)
            m_spellHandler.SpellStopped -= OnSpellStopped;

        if (m_galleryButton && m_galleryButton.transform.IsChildOf(transform) == false)
            Destroy(m_galleryButton.gameObject);

        if (m_sceneVisualToggleButton && m_sceneVisualToggleButton.transform.parent != transform)
            Destroy(m_sceneVisualToggleButton.gameObject);

        m_foregroundInstances.Clear();
    }

    public override void ActiveOn()
    {
        if (m_desktopPetMode && m_galleryButton)
            m_galleryButton.gameObject.SetActive(true);

        if (m_desktopPetMode && m_sceneVisualToggleButton)
            m_sceneVisualToggleButton.gameObject.SetActive(true);

        bool hasBattleData = string.IsNullOrEmpty(StorageManager.instance.StorageData.BattleStorageData.EnemyKey) == false;
        if (hasBattleData)
        {
            BattleStorageData battleStorageData = StorageManager.instance.StorageData.BattleStorageData;
            if (Database<RoleData>.TryLoad(battleStorageData.EnemyKey, out RoleData roleData))
                ApplyBattleRoleData(roleData, battleStorageData.AccumulatedHealValue);
            else
                CreateDefaultEnemy();
        }
        else
        {
            CreateDefaultEnemy();
        }

        BattleManager.instance.Register(Hit);
    }

    public override void ActiveOff()
    {
        if (m_desktopPetMode && m_galleryButton)
            m_galleryButton.gameObject.SetActive(false);

        if (m_desktopPetMode && m_sceneVisualToggleButton)
            m_sceneVisualToggleButton.gameObject.SetActive(false);

        BattleManager.instance.Register(null);
    }

    private void CreateDefaultEnemy()
    {
        SettingsManager.instance.setting.defaultEnemyReference.TryLoad(out m_curBattleRole);
        CreateBattleEnemy();
    }

    private void CreateBattleEnemy()
    {
        CreateBattleEnemy(true);
    }

    private void CreateBattleEnemyAfterDefeat()
    {
        CreateBattleEnemy(true);
    }

    private void CreateBattleEnemy(bool allowNormalAutoAdvance)
    {
        uiGameMainView.ApplyRoleRect();
        RoleData nextRoleData = null;

        EnsureSortedRoleDatas();

        bool isAutoNext = allowNormalAutoAdvance
            && m_autoNextButton
            && m_autoNextButton.isAutoNext;
        bool isSpelling = m_spellHandler && m_spellHandler.isSpelling;
        if (isAutoNext || isSpelling)
        {
            for (int i = 0; i < m_sortedRoleDatas.Count; i++)
            {
                RoleData roleData = m_sortedRoleDatas[i];
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
        PlayAnimation(k_enemySpawnAnimation, () =>
        {
            m_isAlive = true;
            m_isDefeatTransitionActive = false;
            UpdateRoleArrowInteraction();
        });

        PlaySFX(AudioClipType.BossAppearSFX);
    }

    private void EnsureSortedRoleDatas()
    {
        if (m_sortedRoleDatas != null && m_sortedRoleDatas.Count > 0)
            return;

        Dictionary<string, RoleData>.ValueCollection roleDatas = Database<RoleData>.GetAll();
        m_sortedRoleDatas = new List<RoleData>(roleDatas);
        m_sortedRoleDatas.Sort(CompareRoleData);
    }

    private static int CompareRoleData(RoleData left, RoleData right)
    {
        return left.roleSortId.CompareTo(right.roleSortId);
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
        m_isDefeatTransitionActive = true;
        UpdateRoleArrowInteraction();

        TutorialFlowDiagnostics.Log(
            $"敵人擊倒：roleKey={m_curBattleRole?.key}, battleModel={m_battleModel}",
            this);

        PlaySFX(AudioClipType.SatisfySFX);

        StorageManager.instance.StorageData.AddEnemySatisfyCount(m_curBattleRole.key, m_battleModel);
        // 寫入完成狀態後立即刷新，確保自動切換角色前至少繪製一次新徽章。
        uiGameMainView.ApplyRoleRect();
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

                if (m_enemySpawn)
                    m_enemySpawn.RefreshCardStatus(m_curBattleRole, m_battleModel, true);

                PlaySFX(AudioClipType.GetCardSFX);
            }
        }
        GalleryStateChangeNotifier.Notify();
        PlayHealCompleteAnimation(CreateBattleEnemyAfterDefeat);
    }
    public void Hit(string hit)
    {
        if (active == false)
            return;

        switch (m_battleModel)
        {
            case BattleModel.Normal:
                HandleNormalInput(hit);
                break;
            case BattleModel.Tomato:
                HandleTomatoInput(hit);
                break;
            case BattleModel.Spell:
                HandleSpellInput(hit);
                break;
        }
    }

    private void HandleSpellInput(string hit)
    {
        if (m_enemySpawn && m_spellHandler)
        {
            bool spellInputChar = m_spellHandler.InputChar(hit);
            if (m_isAlive == false)
                return;

            if (spellInputChar)
            {
                Tween.Delay(
                    this,
                    0.15f,
                    static panel => panel.ApplySpellHeal(),
                    warnIfTargetDestroyed: false);
            }
        }
    }

    private void ApplySpellHeal()
    {
        if (m_isAlive == false || m_enemySpawn == null)
            return;

        BigNumber value = m_curHealValue * SettingsManager.instance.setting.defaultSpellHeal;
        ApplyHealToEnemy(value);
        PlayHealFeedback(value.ToString());
    }

    private void HandleTomatoInput(string hit)
    {
        if (m_isAlive == false)
            return;

        m_tomatoHitStack++;
        m_enemySpawn.PlayPendingHealFeedback();
    }

    private void HandleNormalInput(string hit)
    {
        if (m_isAlive == false)
            return;

        if (m_enemySpawn)
        {
            ApplyHealToEnemy(m_curHealValue);
            PlayHealFeedback(m_curHealValue.ToString());
            PlayNormalHealEffect();
        }
    }

    private void PlayHealFeedback(string healValue, bool isStrong = false)
    {
        if (m_healFeedbackPanel)
            m_healFeedbackPanel.PlayHealValue(healValue, isStrong);

        if (isStrong)
            PlaySFX(AudioClipType.CriticleSFX);
        else
            PlaySFX(AudioClipType.HealSFX);
    }

    private void ApplyHealToEnemy(BigNumber healValue)
    {
        m_enemySpawn.ApplyHealValue(healValue);
        StorageManager.instance.StorageData.BattleStorageData.SetAccumulatedHealValue(
            m_enemySpawn.AccumulatedHealValue);
    }

    public void TomatoTimeHint(RoleData roleData)
    {
        if (m_tomatoBtn)
            m_tomatoBtn.TomatoTimeHint(roleData);
    }


    private void PlayHealCompleteAnimation(Action callBack = null)
    {
        m_enemySpawn.PlayHealCompleteAnimation(callBack);
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
        if (m_roleTalkHandler != null && m_battleModel != BattleModel.Spell)
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

    private void PlayStrongHealEffect()
    {
        StrongHealEffect strongHealEffect;
        int poolCount = m_strongHealEffectPoolList.Count;
        if (poolCount > 0)
        {
            int lastIndex = poolCount - 1;
            strongHealEffect = m_strongHealEffectPoolList[lastIndex];
            m_strongHealEffectPoolList.RemoveAt(lastIndex);
        }
        else
        {
            strongHealEffect = Instantiate(m_strongHealEffectTemplate, m_healFeedbackPanel.transform);
            strongHealEffect.gameObject.SetActive(false);
        }

        m_activeStrongHealEffectList.Add(strongHealEffect);
        strongHealEffect.transform.localPosition = UnityEngine.Random.insideUnitCircle * 50f;
        strongHealEffect.Play(m_releaseStrongHealEffect);
    }

    private void UpdateRoleArrowInteraction()
    {
        if (uiGameMainView)
            uiGameMainView.SetRoleArrowInteraction(m_battleModel == BattleModel.Normal && m_isDefeatTransitionActive == false);
    }

    private void ReleaseStrongHealEffect(StrongHealEffect strongHealEffect)
    {
        strongHealEffect.gameObject.SetActive(false);
        m_activeStrongHealEffectList.Remove(strongHealEffect);
        m_strongHealEffectPoolList.Add(strongHealEffect);
    }

    private void PlayNormalHealEffect()
    {
        NormalHealEffect normalHealEffect;
        int poolCount = m_normalHealEffectPoolList.Count;
        if (poolCount > 0)
        {
            int lastIndex = poolCount - 1;
            normalHealEffect = m_normalHealEffectPoolList[lastIndex];
            m_normalHealEffectPoolList.RemoveAt(lastIndex);
        }
        else
        {
            normalHealEffect = Instantiate(m_normalHealEffectTemplate, m_healFeedbackPanel.transform);
            normalHealEffect.gameObject.SetActive(false);
        }

        m_activeNormalHealEffectList.Add(normalHealEffect);
        normalHealEffect.transform.localPosition = UnityEngine.Random.insideUnitCircle * 50f;
        normalHealEffect.Play(m_releaseNormalHealEffect);
    }

    private void ReleaseNormalHealEffect(NormalHealEffect normalHealEffect)
    {
        normalHealEffect.gameObject.SetActive(false);
        m_activeNormalHealEffectList.Remove(normalHealEffect);
        m_normalHealEffectPoolList.Add(normalHealEffect);
    }
    public void ApplyDesktopPetMode()
    {
        m_desktopPetMode = true;

        if (m_galleryButton)
            DesktopPetWindowController.RegisterGalleryButton(m_galleryButton);

        if (m_sceneVisualToggleButton)
            DesktopPetWindowController.RegisterSceneVisualToggle(m_sceneVisualToggleButton);

        if (m_autoNextButton)
            DesktopPetWindowController.RegisterAutoNextButton(m_autoNextButton);

        SetDesktopHidden(m_autoNextButton);
        // Desktop Pet 仍保留拼字按鈕與挑戰介面，顯示狀態交由解鎖旗標控制。
        SetDesktopHidden(m_bgmStateButton);
        SetDesktopHidden(m_audioStateButton);
    }

    private static void SetDesktopHidden(Component component)
    {
        if (component)
            component.gameObject.SetActive(false);
    }
}
