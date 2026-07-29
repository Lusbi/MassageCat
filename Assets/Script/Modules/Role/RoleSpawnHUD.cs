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
    private const float k_rolePulseDuration = 0.18f;
    private const float k_pendingPulseDuration = 0.2f;
    private const float k_completeHoldDuration = 0.7f;
    private const float k_completeExitDuration = 0.3f;

    private static readonly Vector3 s_progressPulseStrength = new Vector3(0.015f, 0.12f, 0f);
    private static readonly Vector3 s_rolePulseStrength = new Vector3(0.025f, 0.025f, 0f);
    private static readonly Vector3 s_completePulseStrength = new Vector3(0.05f, 0.05f, 0f);

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
    private Tween m_rolePulseTween;
    private Sequence m_pendingPulseSequence;
    private Sequence m_completeSequence;

    internal BigNumber AccumulatedHealValue => m_accumulatedHealValue;
    internal TextMeshProUGUI RoleTalkText => m_roleTalkText;

    public void Initlization(Action callBack = null)
    {
        m_requiredHealValue = 0;
        m_accumulatedHealValue = 0;
        m_isHealCompleted = false;
        m_enemyImage.sprite = null;
        m_enemyRect = m_enemyImage.GetComponent<RectTransform>();
        if (m_roleTalkText)
            m_roleTalkText.gameObject.SetActive(false);

        CacheVisualState();
        StopFeedbackTweens(true);
        UpdateHealProgressImmediate();
        m_dieActionCallBack = null;

        if (m_cardHud)
            m_cardHud.Initlization();
    }

    public void ApplyDieAction(Action dieActionCallBack)
    {
        m_dieActionCallBack = dieActionCallBack;
    }

    public void LoadRoleData(RoleData roleData)
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
            m_cardHud.Apply(roleData);
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
        StopCompletionFeedback();
        StopPendingHealFeedback();

        if (m_progressFillTween.isAlive == false)
            m_enemyHealProgressBar.fillAmount = 1f;

        m_completeSequence = Sequence.Create()
            .ChainDelay(k_progressFillDuration + k_completeHoldDuration)
            .Chain(Tween.PunchScale(
                m_enemyRect,
                s_completePulseStrength,
                k_completeExitDuration,
                frequency: 3f,
                easeBetweenShakes: Ease.OutSine))
            .Group(Tween.UIFillAmount(
                m_enemyImage,
                m_enemyImage.fillAmount,
                0f,
                k_completeExitDuration,
                Ease.InCubic));

        if (callBack != null)
            m_completeSequence.OnComplete(callBack, static action => action.Invoke(), false);
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

        if (m_rolePulseTween.isAlive)
            m_rolePulseTween.Stop();
        m_enemyRect.localScale = m_enemyBaseScale;
        m_rolePulseTween = Tween.PunchScale(
            m_enemyRect,
            s_rolePulseStrength,
            k_rolePulseDuration,
            frequency: 3f,
            easeBetweenShakes: Ease.OutSine);
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
        if (m_rolePulseTween.isAlive)
            m_rolePulseTween.Stop();

        StopPendingHealFeedback();
        StopCompletionFeedback();

        if (restoreVisualState == false || m_hasCachedVisualState == false)
            return;

        if (m_healProgressRoot)
            m_healProgressRoot.localScale = m_healProgressBaseScale;
        if (m_enemyRect)
            m_enemyRect.localScale = m_enemyBaseScale;
        if (m_enemyImage)
            m_enemyImage.fillAmount = 1f;
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

    private void StopCompletionFeedback()
    {
        if (m_completeSequence.isAlive)
            m_completeSequence.Stop();
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

    private void OnDisable()
    {
        StopFeedbackTweens(true);
    }
}
