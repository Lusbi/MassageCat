using System;
using BigMath;
using GameCore;
using GameCore.Database;
using GameCore.UI;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class RoleSpawnHUD : MonoBehaviour, IInitlization
{
    private const int k_healProgressScale = 1_000_000;
    private const int k_percentageFractionScale = 10_000;
    private const int k_percentageFractionDigits = 4;
    private const int k_percentageTextBufferLength = 9;
    private const float k_healProgressScaleReciprocal = 1f / k_healProgressScale;
    private const float k_progressFillDuration = 0.18f;
    private const float k_progressPulseDuration = 0.22f;
    private const float k_rolePulseDuration = 0.35f;
    private const float k_pendingPulseDuration = 0.2f;
    private const float k_completeHoldDuration = 0.4f;
    private const float k_completeExitDuration = 1.2f;

    private static readonly Vector3 s_progressPulseStrength = new Vector3(0.015f, 0.12f, 0f);

    [Header("UI Components")]
    [SerializeField] private Image m_enemyImage;
    [SerializeField]
    [FormerlySerializedAs("m_enemyHealthText")]
    [FormerlySerializedAs("m_enemyFatigueText")]
    private TextMeshProUGUI m_enemyHealProgressText;
    [SerializeField]
    [FormerlySerializedAs("m_enemyHealthBar")]
    [FormerlySerializedAs("m_enemyFatigueBar")]
    private Image m_enemyHealProgressBar;
    [SerializeField] private RectTransform m_healProgressRoot;
    [SerializeField] private Image m_pendingHealPulse;
    [SerializeField] private RectTransform m_enemyRect;
    [SerializeField] private CardHud m_cardHud;
    [SerializeField] private TextMeshProUGUI m_roleTalkText;

    private Action m_dieActionCallBack;
    private BigNumber m_requiredHealValue;
    private BigNumber m_accumulatedHealValue;
    private readonly char[] m_percentageTextBuffer = new char[k_percentageTextBufferLength];
    private int m_scaledHealProgress;
    private bool m_isHealCompleted;
    private bool m_hasCachedVisualState;
    private Vector3 m_healProgressBaseScale;
    private Vector3 m_enemyBaseScale;
    private Vector3 m_pendingPulseBaseScale;
    private Color m_pendingPulseBaseColor;
    private Tween m_progressFillTween;
    private Tween m_progressPulseTween;
    private Sequence m_pendingPulseSequence;
    private Image m_departingImage;
    private RectTransform m_departingRect;
    private Vector2 m_enemyBasePosition;
    private Color m_enemyBaseColor;
    private float m_exitElapsed;
    private float m_enterElapsed;
    private float m_hitElapsed;
    private Vector3 m_hitStartScale;
    private Vector2 m_hitStartPosition;
    private bool m_isExiting;
    private bool m_isEntering;
    private bool m_isBouncing;
    private Action m_exitMidpoint;
    private Action m_exitCompleted;
    private Action m_enterCompleted;

    internal BigNumber AccumulatedHealValue => m_accumulatedHealValue;
    internal TextMeshProUGUI RoleTalkText => m_roleTalkText;

    public void Initlization(Action callBack = null)
    {
        m_requiredHealValue = 0;
        m_accumulatedHealValue = 0;
        m_isHealCompleted = false;
        m_enemyImage.sprite = null;
        m_enemyRect = m_enemyImage.GetComponent<RectTransform>();
        m_enemyBasePosition = m_enemyRect.anchoredPosition;
        m_enemyBaseColor = m_enemyImage.color;
        if (!m_departingImage)
        {
            // 僅建立一次純顯示副本，交接時不複製角色狀態與事件。
            m_departingImage = Instantiate(m_enemyImage, m_enemyRect.parent);
            m_departingImage.name = "DepartingRoleVisual";
            m_departingImage.raycastTarget = false;
            m_departingRect = m_departingImage.rectTransform;
            m_departingImage.gameObject.SetActive(false);
        }
        if (m_roleTalkText)
        {
            // 對話不跟隨進度列縮窄或脈衝縮放，固定在主版型上方的安全區。
            RectTransform talkRect = m_roleTalkText.rectTransform;
            talkRect.SetParent(transform, false);
            talkRect.anchorMin = new Vector2(0.5f, 1f);
            talkRect.anchorMax = new Vector2(0.5f, 1f);
            talkRect.pivot = new Vector2(0.5f, 1f);
            talkRect.anchoredPosition = new Vector2(0f, -8f);
            talkRect.sizeDelta = new Vector2(336f, 54f);
            talkRect.localRotation = Quaternion.identity;
            talkRect.localScale = Vector3.one;
            m_roleTalkText.margin = new Vector4(6f, 2f, 6f, 2f);
            m_roleTalkText.alignment = TextAlignmentOptions.Center;
            m_roleTalkText.enableAutoSizing = true;
            m_roleTalkText.fontSizeMin = 14f;
            m_roleTalkText.fontSizeMax = 20f;
            m_roleTalkText.textWrappingMode = TextWrappingModes.Normal;
            m_roleTalkText.overflowMode = TextOverflowModes.Ellipsis;
            m_roleTalkText.gameObject.SetActive(false);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[Codex驗證] 對話已改為上方橫向安全區；請確認長句換行且未超出遊戲邊界。", this);
#endif
        }

        ConfigureHealProgressLayout();
        CacheVisualState();
        StopFeedbackTweens(true);
        UpdateHealProgressImmediate();
        m_dieActionCallBack = null;

        if (m_cardHud)
            m_cardHud.Initlization();
    }

    private void ConfigureHealProgressLayout()
    {
        if (!m_healProgressRoot)
            return;

        // 配合 480×320 主版型，進度列位於角色下方、模式面板上方。
        m_healProgressRoot.gameObject.SetActive(true);
        m_healProgressRoot.anchorMin = new Vector2(0.5f, 1f);
        m_healProgressRoot.anchorMax = new Vector2(0.5f, 1f);
        m_healProgressRoot.pivot = new Vector2(0.5f, 1f);
        m_healProgressRoot.anchoredPosition = new Vector2(0f, -204f);
        m_healProgressRoot.sizeDelta = new Vector2(260f, 22f);
        m_healProgressRoot.localScale = Vector3.one;
        StretchProgressElement(m_enemyHealProgressBar.rectTransform, new Vector2(-8f, -6f));
        m_enemyHealProgressBar.gameObject.SetActive(true);
        m_enemyHealProgressBar.enabled = true;
        m_enemyHealProgressBar.type = Image.Type.Filled;
        m_enemyHealProgressBar.fillMethod = Image.FillMethod.Horizontal;
        m_enemyHealProgressBar.fillOrigin = 0;
        m_enemyHealProgressBar.color = new Color32(133, 176, 94, 255);
        StretchProgressElement(m_enemyHealProgressText.rectTransform, Vector2.zero);
        m_enemyHealProgressText.fontSize = 14f;
        m_enemyHealProgressText.enableAutoSizing = false;
        m_enemyHealProgressText.margin = Vector4.zero;
        m_enemyHealProgressText.alignment = TextAlignmentOptions.Center;
        m_enemyHealProgressText.color = new Color32(63, 43, 27, 255);
        m_enemyHealProgressText.gameObject.SetActive(true);
        if (m_pendingHealPulse)
            StretchProgressElement(m_pendingHealPulse.rectTransform, Vector2.zero);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[Codex驗證] 療癒進度列已啟用並定位於角色下方；請確認百分比與填充隨療癒增加。", this);
#endif
    }

    private static void StretchProgressElement(RectTransform rect, Vector2 size)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    public void ApplyDieAction(Action dieActionCallBack)
    {
        m_dieActionCallBack = dieActionCallBack;
    }

    public void LoadRoleData(RoleData roleData, BattleModel battleModel = BattleModel.Normal)
    {
        StopFeedbackTweens(true);
        m_enemyImage.sprite = roleData.EnemyIcon;
        m_enemyImage.fillAmount = 1f;
        m_enemyImage.enabled = m_enemyImage.sprite != null;

        m_requiredHealValue = roleData.RequiredHealValue;
        m_accumulatedHealValue = 0;
        m_isHealCompleted = false;
        UpdateHealProgressImmediate();

        float rotationY = roleData.DirectionType == DirectionType.Right ? 180f : 0f;
        m_enemyRect.localEulerAngles = new Vector3(0f, rotationY, 0f);
        if (m_cardHud)
            m_cardHud.Apply(roleData, battleModel);
    }

    internal void RefreshCardStatus(RoleData roleData, BattleModel battleModel, bool playObtainedFeedback)
    {
        if (m_cardHud)
            m_cardHud.Apply(roleData, battleModel, playObtainedFeedback);
    }

    public void ApplyBattleStorageData(BattleStorageData battleStorageData)
    {
        if (Database<RoleData>.TryLoad(battleStorageData.EnemyKey, out RoleData roleData))
        {
            LoadRoleData(roleData);
            ApplyAccumulatedHealValue(battleStorageData.AccumulatedHealValue);
        }
    }

    internal void ApplyAccumulatedHealValue(BigNumber accumulatedHealValue)
    {
        StopFeedbackTweens(true);
        if (m_requiredHealValue <= 0)
        {
            m_accumulatedHealValue = 0;
            m_isHealCompleted = false;
            UpdateHealProgressImmediate();
            return;
        }

        m_accumulatedHealValue = accumulatedHealValue;
        if (m_accumulatedHealValue < 0)
            m_accumulatedHealValue = 0;
        else if (m_accumulatedHealValue > m_requiredHealValue)
            m_accumulatedHealValue = m_requiredHealValue;

        m_isHealCompleted = m_accumulatedHealValue >= m_requiredHealValue;
        UpdateHealProgressImmediate();
    }

    public void ApplyHealValue(BigNumber healValue)
    {
        if (m_isHealCompleted || m_requiredHealValue <= 0 || healValue <= 0)
            return;

        m_accumulatedHealValue += healValue;
        if (m_accumulatedHealValue >= m_requiredHealValue)
        {
            m_accumulatedHealValue = m_requiredHealValue;
            m_isHealCompleted = true;
        }

        RefreshScaledHealProgress();
        UpdateHealProgressText();
        PlayHealProgressFeedback(GetNormalizedHealProgress());

        if (m_isHealCompleted)
            m_dieActionCallBack?.Invoke();
    }

    public void PlayPendingHealFeedback()
    {
        if (m_isHealCompleted || m_pendingHealPulse == null)
            return;

        StopPendingHealFeedback();
        RectTransform pendingRect = m_pendingHealPulse.rectTransform;
        pendingRect.localScale = m_pendingPulseBaseScale * 0.9f;

        Color startColor = m_pendingPulseBaseColor;
        startColor.a = 0.65f;
        Color endColor = startColor;
        endColor.a = 0f;
        m_pendingHealPulse.color = startColor;

        m_pendingPulseSequence = Sequence.Create()
            .Chain(Tween.Scale(
                pendingRect,
                pendingRect.localScale,
                new Vector3(m_pendingPulseBaseScale.x * 1.08f, m_pendingPulseBaseScale.y * 1.8f, m_pendingPulseBaseScale.z),
                k_pendingPulseDuration,
                Ease.OutSine))
            .Group(Tween.Color(
                m_pendingHealPulse,
                startColor,
                endColor,
                k_pendingPulseDuration,
                Ease.OutSine))
            .OnComplete(this, static hud => hud.ResetPendingHealVisual(), false);
    }

    public void PlayHealCompleteAnimation(Action callBack)
    {
        BeginExit(null, callBack);
    }

    internal void BeginHandoff(Action midpoint)
    {
        BeginExit(midpoint, null);
    }

    private void BeginExit(Action midpoint, Action completed)
    {
        m_isBouncing = false;
        m_isEntering = false;
        m_enterCompleted = null;
        StopPendingHealFeedback();
        m_enemyRect.localScale = m_enemyBaseScale;
        m_enemyRect.anchoredPosition = m_enemyBasePosition;
        m_departingImage.sprite = m_enemyImage.sprite;
        m_departingImage.color = m_enemyBaseColor;
        m_departingImage.fillAmount = 1f;
        m_departingImage.enabled = true;
        m_departingRect.anchorMin = m_enemyRect.anchorMin;
        m_departingRect.anchorMax = m_enemyRect.anchorMax;
        m_departingRect.pivot = m_enemyRect.pivot;
        m_departingRect.sizeDelta = m_enemyRect.sizeDelta;
        m_departingRect.anchoredPosition = m_enemyBasePosition;
        m_departingRect.localScale = m_enemyBaseScale;
        m_departingRect.localRotation = m_enemyRect.localRotation;
        m_departingImage.gameObject.SetActive(true);
        m_enemyImage.enabled = false;
        m_exitElapsed = 0f;
        m_isExiting = true;
        m_exitMidpoint = midpoint;
        m_exitCompleted = completed;
    }

    internal void BeginEntrance(Action completed)
    {
        m_isBouncing = false;
        m_isEntering = true;
        m_enterElapsed = 0f;
        m_enterCompleted = completed;
        m_enemyImage.enabled = m_enemyImage.sprite != null;
        m_enemyRect.localScale = m_enemyBaseScale;
        ApplyMovingVisual(m_enemyImage, m_enemyRect, 1f, 0f);
    }

    internal void PlaySoftHit()
    {
        if (m_isExiting || m_isEntering)
            return;

        m_hitStartScale = m_enemyRect.localScale;
        m_hitStartPosition = m_enemyRect.anchoredPosition;
        m_hitElapsed = 0f;
        m_isBouncing = true;
    }

    private void ApplyMovingVisual(Image image, RectTransform rect, float distance, float alpha)
    {
        // 緩慢向右移動並上下起伏，保持面向與圖像完整，不以填充裁切退場。
        rect.anchoredPosition = m_enemyBasePosition + new Vector2(
            100f * distance, Mathf.Sin(distance * Mathf.PI * 2f) * 8f);
        Color color = m_enemyBaseColor;
        color.a *= alpha;
        image.color = color;
    }

    private void Update()
    {
        float deltaTime = Time.unscaledDeltaTime;
        bool wasEntering = m_isEntering;
        if (m_isExiting)
        {
            m_exitElapsed += deltaTime;
            float progress = Mathf.Clamp01((m_exitElapsed - k_completeHoldDuration) / k_completeExitDuration);
            float eased = Mathf.SmoothStep(0f, 1f, progress);
            ApplyMovingVisual(m_departingImage, m_departingRect, eased, 1f - eased);
            if (progress >= 0.5f && m_exitMidpoint != null)
            {
                Action midpoint = m_exitMidpoint;
                m_exitMidpoint = null;
                midpoint.Invoke();
            }
            if (progress >= 1f)
            {
                m_isExiting = false;
                m_departingImage.gameObject.SetActive(false);
                Action completed = m_exitCompleted;
                m_exitCompleted = null;
                completed?.Invoke();
            }
        }

        if (m_isEntering)
        {
            // 在同一幀由退場回呼開始進場時，不重複計入整幀時間。
            m_enterElapsed += wasEntering ? deltaTime : 0f;
            float progress = Mathf.Clamp01(m_enterElapsed);
            float eased = Mathf.SmoothStep(0f, 1f, progress);
            ApplyMovingVisual(m_enemyImage, m_enemyRect, 1f - eased, eased);
            if (progress >= 1f && !m_isExiting)
            {
                m_isEntering = false;
                Action completed = m_enterCompleted;
                m_enterCompleted = null;
                completed?.Invoke();
            }
        }
        else if (m_isBouncing)
        {
            m_hitElapsed += deltaTime;
            float progress = Mathf.Clamp01(m_hitElapsed / k_rolePulseDuration);
            if (progress < 0.25f)
            {
                float eased = Mathf.SmoothStep(0f, 1f, progress * 4f);
                m_enemyRect.localScale = Vector3.Lerp(m_hitStartScale,
                    Vector3.Scale(m_enemyBaseScale, new Vector3(1.08f, 0.92f, 1f)), eased);
                m_enemyRect.anchoredPosition = Vector2.Lerp(m_hitStartPosition, m_enemyBasePosition, eased);
            }
            else
            {
                float spring = (progress - 0.25f) / 0.75f;
                float wave = Mathf.Cos(spring * Mathf.PI * 2f) * (1f - spring) * (1f - spring);
                m_enemyRect.localScale = Vector3.Scale(m_enemyBaseScale,
                    new Vector3(1f + 0.08f * wave, 1f - 0.08f * wave, 1f));
                m_enemyRect.anchoredPosition = m_enemyBasePosition + Vector2.up * (6f * Mathf.Sin(spring * Mathf.PI));
            }
            if (progress >= 1f)
            {
                m_isBouncing = false;
                m_enemyRect.localScale = m_enemyBaseScale;
                m_enemyRect.anchoredPosition = m_enemyBasePosition;
            }
        }
    }

    private void UpdateHealProgressImmediate()
    {
        RefreshScaledHealProgress();
        UpdateHealProgressText();
        m_enemyHealProgressBar.fillAmount = GetNormalizedHealProgress();
    }

    private void UpdateHealProgressText()
    {
        int writeIndex = k_percentageTextBufferLength;
        m_percentageTextBuffer[--writeIndex] = '%';

        int fraction = m_scaledHealProgress % k_percentageFractionScale;
        for (int i = 0; i < k_percentageFractionDigits; i++)
        {
            m_percentageTextBuffer[--writeIndex] = (char)('0' + fraction % 10);
            fraction /= 10;
        }

        m_percentageTextBuffer[--writeIndex] = '.';
        int wholePercentage = m_scaledHealProgress / k_percentageFractionScale;
        do
        {
            m_percentageTextBuffer[--writeIndex] = (char)('0' + wholePercentage % 10);
            wholePercentage /= 10;
        } while (wholePercentage > 0);

        m_enemyHealProgressText.SetCharArray(
            m_percentageTextBuffer,
            writeIndex,
            k_percentageTextBufferLength - writeIndex);
    }

    private void RefreshScaledHealProgress()
    {
        if (BigNumber.TryGetScaledRatio(
                m_accumulatedHealValue,
                m_requiredHealValue,
                k_healProgressScale,
                out m_scaledHealProgress) == false)
        {
            m_scaledHealProgress = 0;
            return;
        }

        // 尚未達成時不可因顯示精度的四捨五入而提前顯示 100.0000%。
        if (m_accumulatedHealValue < m_requiredHealValue
            && m_scaledHealProgress >= k_healProgressScale)
        {
            m_scaledHealProgress = k_healProgressScale - 1;
        }
    }

    private float GetNormalizedHealProgress()
    {
        return m_scaledHealProgress * k_healProgressScaleReciprocal;
    }

    private void PlayHealProgressFeedback(float targetProgress)
    {
        StopPendingHealFeedback();

        if (m_progressFillTween.isAlive)
            m_progressFillTween.Stop();
        m_progressFillTween = Tween.UIFillAmount(
            m_enemyHealProgressBar,
            m_enemyHealProgressBar.fillAmount,
            targetProgress,
            k_progressFillDuration,
            Ease.OutCubic);

        if (m_healProgressRoot)
        {
            if (m_progressPulseTween.isAlive)
                m_progressPulseTween.Stop();
            m_healProgressRoot.localScale = m_healProgressBaseScale;
            m_progressPulseTween = Tween.PunchScale(
                m_healProgressRoot,
                s_progressPulseStrength,
                k_progressPulseDuration,
                frequency: 3f,
                easeBetweenShakes: Ease.OutSine);
        }

        PlaySoftHit();
    }

    private void CacheVisualState()
    {
        if (m_hasCachedVisualState)
            return;

        m_healProgressBaseScale = m_healProgressRoot ? m_healProgressRoot.localScale : Vector3.one;
        m_enemyBaseScale = m_enemyRect ? m_enemyRect.localScale : Vector3.one;
        if (m_pendingHealPulse)
        {
            m_pendingPulseBaseScale = m_pendingHealPulse.rectTransform.localScale;
            m_pendingPulseBaseColor = m_pendingHealPulse.color;
        }
        else
        {
            m_pendingPulseBaseScale = Vector3.one;
            m_pendingPulseBaseColor = Color.white;
        }

        m_hasCachedVisualState = true;
    }

    private void StopFeedbackTweens(bool restoreVisualState)
    {
        if (m_progressFillTween.isAlive)
            m_progressFillTween.Stop();
        if (m_progressPulseTween.isAlive)
            m_progressPulseTween.Stop();

        StopPendingHealFeedback();


        if (restoreVisualState == false || m_hasCachedVisualState == false)
            return;

        if (m_healProgressRoot)
            m_healProgressRoot.localScale = m_healProgressBaseScale;
        if (m_enemyRect)
            m_enemyRect.localScale = m_enemyBaseScale;
        m_isBouncing = false;
        if (m_enemyRect)
            m_enemyRect.anchoredPosition = m_enemyBasePosition;
        if (m_enemyImage)
        {
            m_enemyImage.fillAmount = 1f;
            m_enemyImage.color = m_enemyBaseColor;
        }
        if (m_enemyHealProgressBar)
            m_enemyHealProgressBar.fillAmount = GetNormalizedHealProgress();
        ResetPendingHealVisual();
    }

    private void StopPendingHealFeedback()
    {
        if (m_pendingPulseSequence.isAlive)
            m_pendingPulseSequence.Stop();
        ResetPendingHealVisual();
    }


    private void ResetPendingHealVisual()
    {
        if (m_pendingHealPulse == null || m_hasCachedVisualState == false)
            return;

        m_pendingHealPulse.rectTransform.localScale = m_pendingPulseBaseScale;
        Color color = m_pendingPulseBaseColor;
        color.a = 0f;
        m_pendingHealPulse.color = color;
    }

    internal void CancelRoleMotion()
    {
        m_isExiting = false;
        m_isEntering = false;
        m_exitMidpoint = null;
        m_exitCompleted = null;
        m_enterCompleted = null;
        if (m_departingImage)
            m_departingImage.gameObject.SetActive(false);
        StopFeedbackTweens(true);
        if (m_enemyImage)
            m_enemyImage.enabled = m_enemyImage.sprite != null;
    }

    private void OnDisable()
    {
        CancelRoleMotion();
    }
}
