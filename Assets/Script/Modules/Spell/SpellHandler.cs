using System;
using System.Text;
using GameCore;
using GameCore.Audio;
using GameCore.Database;
using GameCore.Event;
using GameCore.Resources;
using Modules.SystemMessage;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum SpellStopReason
{
    Manual,
    Forced,
    Timeout
}

public class SpellHandler : MonoBehaviour, IInitlization
{
    private const float k_defaultDurationTime = 300f;
    private const string k_addressableKey = "SpellQuestion";
    private const string k_interruptedTitle = "拼字挑戰中斷";
    private const string k_interruptedContent = "拼字挑戰已中斷，請再試一次。";
    private const string k_finishedTitle = "拼字挑戰結束";
    private const string k_timeoutTitle = "拼字挑戰時間到";
    private const int k_completionSlotCount = 2;
    private const int k_sparklesPerSlot = 6;
    private const float k_completionStartScale = 0.82f;
    private const float k_completionPeakScale = 1.12f;
    private const float k_completionPopDuration = 0.12f;
    private const float k_completionSettleDuration = 0.1f;
    private const float k_completionHoldDuration = 0.12f;
    private const float k_completionExitDuration = 0.28f;
    private const float k_completionFeedbackDuration =
        k_completionPopDuration + k_completionSettleDuration + k_completionHoldDuration + k_completionExitDuration;
    private const float k_completionRiseDistance = 24f;
    private const float k_sparkleMoveDuration = 0.42f;
    private const float k_errorShakeDuration = 0.14f;
    private const float k_errorColorDuration = 0.16f;
    private const int k_warningSecondThreshold = 30;
    private const int k_dangerSecondThreshold = 10;
    private const string k_typedColorTag = "<color=#91E6B2>";
    private const string k_nextColorTag = "<color=#FFD36A>";
    private const string k_remainingColorTag = "<color=#FFF4E5>";
    private const string k_colorEndTag = "</color>";

    private static readonly Color s_promptBorderColor = new Color32(143, 165, 217, 153);
    private static readonly Color s_errorColor = new Color32(255, 141, 136, 255);
    private static readonly Color s_timerNormalColor = new Color32(220, 229, 255, 255);
    private static readonly Color s_timerWarningColor = new Color32(255, 211, 106, 255);
    private static readonly Color s_timerDangerColor = new Color32(255, 141, 136, 255);
    private static readonly Vector3 s_timerPulseStrength = new Vector3(0.08f, 0.08f, 0f);
    private static readonly Vector2[] s_sparkleEndOffsets =
    {
        new Vector2(-42f, 24f),
        new Vector2(-30f, 36f),
        new Vector2(-18f, 44f),
        new Vector2(18f, 44f),
        new Vector2(30f, 36f),
        new Vector2(42f, 24f),
    };

    [SerializeField] private TextMeshProUGUI m_questingText;
    [SerializeField] private TextMeshProUGUI m_timeText;
    [SerializeField] private GameObject m_challengeRoot;
    [SerializeField] private TextMeshProUGUI m_translationText;
    [SerializeField] private RectTransform m_promptRoot;
    [SerializeField] private Image m_promptBorder;
    [SerializeField] private RectTransform m_timerRoot;
    [SerializeField] private RectTransform[] m_completionRoots;
    [SerializeField] private CanvasGroup[] m_completionCanvasGroups;
    [SerializeField] private TextMeshProUGUI[] m_completionTexts;
    [SerializeField] private Image[] m_completionSparkles;

    private readonly StringBuilder m_answerBuilder = new StringBuilder(256);
    private readonly char[] m_timeBuffer = new char[5];
    private readonly Sequence[] m_completionSequences = new Sequence[k_completionSlotCount];
    private readonly Vector2[] m_completionBasePositions = new Vector2[k_completionSlotCount];
    private readonly Vector2[] m_sparkleBasePositions = new Vector2[k_completionSlotCount * k_sparklesPerSlot];

    private SpellQuestion m_spellQuestion;
    private LevelQuestionInfo m_levelQuestionInfo;
    private string m_quest;
    private string m_chineseQuest;
    private float m_durationTime;
    private int m_answerLength;
    private int m_correctCharCount;
    private int m_incorrectCharCount;
    private int m_completedQuestCount;
    private int m_lastDisplayedSecond = -1;
    private int m_nextCompletionSlot;
    private Vector2 m_promptBasePosition;
    private Vector3 m_timerBaseScale = Vector3.one;
    private Tween m_promptShakeTween;
    private Tween m_promptColorTween;
    private Tween m_timerPulseTween;
    private Tween m_nextQuestTween;
    private bool m_visualStateCached;

    public int CorrectCharCount => m_correctCharCount;

    public int IncorrectCharCount => m_incorrectCharCount;

    public int CompletedQuestCount => m_completedQuestCount;

    public bool isSpelling { get; private set; }

    public event Action<SpellStopReason> SpellStopped;

    public void Initlization(Action callBack = null)
    {
        CacheVisualState();
        ResetVisualFeedback();
        SetChallengeUIVisible(false);

        CoreResoucesService.LoadAssetAsync<SpellQuestion>(k_addressableKey, setting =>
        {
            m_spellQuestion = setting;
            callBack?.Invoke();
        });

        Clear();
        ClearTranslationText();

        // 舊翻譯欄位只保留序列化相容；實際內容改由角色說話位置呈現。
        if (m_translationText && m_translationText.gameObject.activeSelf)
            m_translationText.gameObject.SetActive(false);
    }

    public void SpellStart()
    {
        StopVisualFeedback();
        Clear();
        SetChallengeUIVisible(true);
        m_durationTime = k_defaultDurationTime;
        ApplyQuest();
        UpdateAnswerText();
        UpdateQuestTranslationText();
        m_lastDisplayedSecond = -1;
        UpdateTimeText();
        isSpelling = true;
    }

    public void SpellStop(SpellStopReason reason = SpellStopReason.Manual)
    {
        if (!isSpelling)
            return;

        int completedQuestCount = m_completedQuestCount;
        int correctCharCount = m_correctCharCount;
        int incorrectCharCount = m_incorrectCharCount;

        StopVisualFeedback();
        Clear();

        if (m_questingText)
            m_questingText.SetText(string.Empty);

        ClearTranslationText();
        UpdateTimeText();
        SetChallengeUIVisible(false);
        SpellStopped?.Invoke(reason);
        NotifySpellStopped(reason, completedQuestCount, correctCharCount, incorrectCharCount);
    }

    private void Clear()
    {
        m_quest = string.Empty;
        m_chineseQuest = string.Empty;
        m_answerLength = 0;
        m_correctCharCount = 0;
        m_incorrectCharCount = 0;
        m_completedQuestCount = 0;
        m_levelQuestionInfo = null;
        isSpelling = false;
        m_durationTime = 0f;
        m_lastDisplayedSecond = -1;
    }

    private void ApplyQuest()
    {
        string roleKey = StorageManager.instance.StorageData.BattleStorageData.EnemyKey;
        Database<RoleData>.TryLoad(roleKey, out RoleData roleData);
        string sceneMapKey = roleData.SceneReference.GetKey();
        m_levelQuestionInfo = m_spellQuestion.GetLevelQuestionInfo(sceneMapKey);
        if (m_levelQuestionInfo == null)
            return;

        QuestionInfo questionInfo = RandomQuestion();
        if (questionInfo == null)
            return;

        m_quest = questionInfo.quesition;
        m_chineseQuest = questionInfo.chineseQuesition;
        m_answerLength = 0;
    }

    private QuestionInfo RandomQuestion()
    {
        if (m_levelQuestionInfo == null || m_levelQuestionInfo.questions.Count == 0)
            return null;

        int randomIndex = UnityEngine.Random.Range(0, m_levelQuestionInfo.questions.Count);
        return m_levelQuestionInfo.questions[randomIndex];
    }

    public bool InputChar(string inputChar)
    {
        if (string.IsNullOrEmpty(inputChar) || string.IsNullOrEmpty(m_quest))
            return false;

        char normalizedInput = inputChar[0];
        bool completed = false;
        if (m_answerLength < m_quest.Length
            && char.ToUpperInvariant(m_quest[m_answerLength]) == char.ToUpperInvariant(normalizedInput))
        {
            m_answerLength++;
            m_correctCharCount++;
            completed = m_answerLength == m_quest.Length;
            if (completed)
            {
                PlayAni();
                m_completedQuestCount++;
                PlaySFX(AudioClipType.Spell_SuceessSFX);

                // 完成演出期間先清空題目，避免舊單字特效覆蓋或干擾下一題。
                m_quest = string.Empty;
                m_chineseQuest = string.Empty;
                m_answerLength = 0;
                UpdateAnswerText();
                ClearTranslationText();
                ScheduleNextQuest();
                return true;
            }
        }
        else
        {
            m_incorrectCharCount++;
            PlaySFX(AudioClipType.Spell_FailSFX);
            PlayIncorrectFeedback();
        }

        UpdateAnswerText();
        return completed;
    }

    private void UpdateAnswerText()
    {
        if (!m_questingText)
            return;

        m_answerBuilder.Clear();
        if (string.IsNullOrEmpty(m_quest) == false)
        {
            if (m_answerLength > 0)
            {
                m_answerBuilder.Append(k_typedColorTag);
                m_answerBuilder.Append(m_quest, 0, m_answerLength);
                m_answerBuilder.Append(k_colorEndTag);
            }

            if (m_answerLength < m_quest.Length)
            {
                m_answerBuilder.Append(k_nextColorTag);
                m_answerBuilder.Append(m_quest[m_answerLength]);
                m_answerBuilder.Append(k_colorEndTag);

                int remainingLength = m_quest.Length - m_answerLength - 1;
                if (remainingLength > 0)
                {
                    m_answerBuilder.Append(k_remainingColorTag);
                    m_answerBuilder.Append(m_quest, m_answerLength + 1, remainingLength);
                    m_answerBuilder.Append(k_colorEndTag);
                }
            }

        }

        m_questingText.SetText(m_answerBuilder);
    }

    private void UpdateQuestTranslationText()
    {
        if (string.IsNullOrEmpty(m_chineseQuest))
            return;

        if (TalkManager.instance)
        {
            // 拼字中文需持續顯示到換題或離開模式，不套用一般對話的自動消失規則。
            TalkManager.instance.Play(m_chineseQuest, float.PositiveInfinity);
        }
    }

    private void ClearTranslationText()
    {
        if (m_translationText)
            m_translationText.SetText(string.Empty);

        if (TalkManager.instance)
            TalkManager.instance.Clear();
    }

    private void ScheduleNextQuest()
    {
        if (m_nextQuestTween.isAlive)
            m_nextQuestTween.Stop();

        m_nextQuestTween = Tween.Delay(
            this,
            k_completionFeedbackDuration,
            static handler => handler.ShowNextQuest(),
            warnIfTargetDestroyed: false);
    }

    private void ShowNextQuest()
    {
        if (!isSpelling)
            return;

        ApplyQuest();
        UpdateAnswerText();
        UpdateQuestTranslationText();
    }

    private void NotifySpellStopped(
        SpellStopReason reason,
        int completedQuestCount,
        int correctCharCount,
        int incorrectCharCount)
    {
        if (reason == SpellStopReason.Forced)
        {
            EventManager.instance.Notify(new SystemMessageEvent(
                k_interruptedTitle,
                k_interruptedContent,
                null));
            return;
        }

        m_answerBuilder.Clear();
        if (reason == SpellStopReason.Timeout)
            m_answerBuilder.Append("時間到，本次結果：\n");
        else
            m_answerBuilder.Append("本次拼字結果：\n");

        m_answerBuilder
            .Append("完成單字：").Append(completedQuestCount)
            .Append("\n正確輸入：").Append(correctCharCount)
            .Append("\n錯誤輸入：").Append(incorrectCharCount);

        EventManager.instance.Notify(new SystemMessageEvent(
            reason == SpellStopReason.Timeout ? k_timeoutTitle : k_finishedTitle,
            m_answerBuilder.ToString(),
            null));
    }

    private void SetChallengeUIVisible(bool isVisible)
    {
        if (m_challengeRoot && m_challengeRoot.activeSelf != isVisible)
            m_challengeRoot.SetActive(isVisible);
    }

    public void Tick(float deltaTime)
    {
        if (!isSpelling)
            return;

        m_durationTime -= deltaTime;
        if (m_durationTime <= 0f)
        {
            m_durationTime = 0f;
            SpellStop(SpellStopReason.Timeout);
            return;
        }

        UpdateTimeText();
    }

    private void UpdateTimeText()
    {
        if (!m_timeText)
            return;

        if (m_durationTime <= 0f)
        {
            m_timeText.SetText(string.Empty);
            m_timeText.color = s_timerNormalColor;
            m_lastDisplayedSecond = 0;
            return;
        }

        int displayedSecond = Mathf.CeilToInt(m_durationTime);
        if (displayedSecond == m_lastDisplayedSecond)
            return;

        m_lastDisplayedSecond = displayedSecond;
        int minutes = displayedSecond / 60;
        int seconds = displayedSecond - minutes * 60;
        m_timeBuffer[0] = (char)('0' + minutes / 10);
        m_timeBuffer[1] = (char)('0' + minutes % 10);
        m_timeBuffer[2] = ':';
        m_timeBuffer[3] = (char)('0' + seconds / 10);
        m_timeBuffer[4] = (char)('0' + seconds % 10);
        m_timeText.SetCharArray(m_timeBuffer, 0, m_timeBuffer.Length);

        if (displayedSecond <= k_dangerSecondThreshold)
        {
            m_timeText.color = s_timerDangerColor;
            PlayTimerPulse();
        }
        else if (displayedSecond <= k_warningSecondThreshold)
        {
            m_timeText.color = s_timerWarningColor;
        }
        else
        {
            m_timeText.color = s_timerNormalColor;
        }
    }

    public void PlayAni()
    {
        m_answerBuilder.Clear();
        if (string.IsNullOrEmpty(m_quest) == false && m_answerLength > 0)
            m_answerBuilder.Append(m_quest, 0, m_answerLength);

        PlayCompletionFeedback(m_answerBuilder);
    }

    private void PlayCompletionFeedback(StringBuilder completedAnswer)
    {
        int slotCount = GetValidCompletionSlotCount();
        if (slotCount == 0)
            return;

        int slotIndex = m_nextCompletionSlot % slotCount;
        m_nextCompletionSlot = (slotIndex + 1) % slotCount;
        StopCompletionSlot(slotIndex);

        RectTransform root = m_completionRoots[slotIndex];
        CanvasGroup canvasGroup = m_completionCanvasGroups[slotIndex];
        TextMeshProUGUI completionText = m_completionTexts[slotIndex];
        root.anchoredPosition = m_completionBasePositions[slotIndex];
        root.localScale = Vector3.one * k_completionStartScale;
        canvasGroup.alpha = 0f;
        completionText.SetText(completedAnswer);

        Sequence sequence = Sequence.Create()
            .Group(Tween.Alpha(canvasGroup, 0f, 1f, 0.1f, Ease.OutSine))
            .Group(Tween.Scale(
                root,
                k_completionStartScale,
                k_completionPeakScale,
                k_completionPopDuration,
                Ease.OutBack))
            .Chain(Tween.Scale(
                root,
                k_completionPeakScale,
                1f,
                k_completionSettleDuration,
                Ease.OutSine))
            .ChainDelay(k_completionHoldDuration)
            .Chain(Tween.UIAnchoredPosition(
                root,
                m_completionBasePositions[slotIndex],
                m_completionBasePositions[slotIndex] + Vector2.up * k_completionRiseDistance,
                k_completionExitDuration,
                Ease.InSine))
            .Group(Tween.Alpha(canvasGroup, 1f, 0f, k_completionExitDuration, Ease.InSine));

        int sparkleStartIndex = slotIndex * k_sparklesPerSlot;
        int sparkleCount = m_completionSparkles != null ? m_completionSparkles.Length : 0;
        int sparkleEndIndex = Mathf.Min(sparkleStartIndex + k_sparklesPerSlot, sparkleCount);
        for (int sparkleIndex = sparkleStartIndex; sparkleIndex < sparkleEndIndex; sparkleIndex++)
        {
            Image sparkle = m_completionSparkles[sparkleIndex];
            if (!sparkle)
                continue;

            int localSparkleIndex = sparkleIndex - sparkleStartIndex;
            RectTransform sparkleRect = sparkle.rectTransform;
            Vector2 startPosition = m_sparkleBasePositions[sparkleIndex];
            sparkleRect.anchoredPosition = startPosition;
            sparkleRect.localScale = Vector3.one * 0.35f;
            sequence = sequence
                .Insert(0f, Tween.UIAnchoredPosition(
                    sparkleRect,
                    startPosition,
                    startPosition + s_sparkleEndOffsets[localSparkleIndex],
                    k_sparkleMoveDuration,
                    Ease.OutCubic,
                    startDelay: localSparkleIndex * 0.015f))
                .Insert(0f, Tween.Scale(
                    sparkleRect,
                    0.35f,
                    1f,
                    0.18f,
                    Ease.OutBack,
                    startDelay: localSparkleIndex * 0.015f));
        }

        m_completionSequences[slotIndex] = sequence;
    }

    private void PlayIncorrectFeedback()
    {
        if (m_promptShakeTween.isAlive)
            m_promptShakeTween.Stop();
        if (m_promptColorTween.isAlive)
            m_promptColorTween.Stop();

        if (m_promptRoot)
        {
            m_promptRoot.anchoredPosition = m_promptBasePosition;
            m_promptShakeTween = Tween.ShakeLocalPosition(
                m_promptRoot,
                new Vector3(5f, 0f, 0f),
                k_errorShakeDuration,
                frequency: 18f,
                easeBetweenShakes: Ease.OutSine);
        }

        if (m_promptBorder)
        {
            m_promptBorder.color = s_errorColor;
            m_promptColorTween = Tween.Color(
                m_promptBorder,
                s_errorColor,
                s_promptBorderColor,
                k_errorColorDuration,
                Ease.OutSine);
        }
    }

    private void PlayTimerPulse()
    {
        if (!m_timerRoot)
            return;

        if (m_timerPulseTween.isAlive)
            m_timerPulseTween.Stop();
        if (m_nextQuestTween.isAlive)
            m_nextQuestTween.Stop();

        m_timerRoot.localScale = m_timerBaseScale;
        m_timerPulseTween = Tween.PunchScale(
            m_timerRoot,
            s_timerPulseStrength,
            0.24f,
            frequency: 2f,
            easeBetweenShakes: Ease.OutSine);
    }

    private void CacheVisualState()
    {
        if (m_visualStateCached)
            return;

        if (m_promptRoot)
            m_promptBasePosition = m_promptRoot.anchoredPosition;
        if (m_timerRoot)
            m_timerBaseScale = m_timerRoot.localScale;

        int slotCount = GetValidCompletionSlotCount();
        for (int i = 0; i < slotCount; i++)
            m_completionBasePositions[i] = m_completionRoots[i].anchoredPosition;

        if (m_completionSparkles != null)
        {
            int sparkleCount = Mathf.Min(m_completionSparkles.Length, m_sparkleBasePositions.Length);
            for (int i = 0; i < sparkleCount; i++)
            {
                if (m_completionSparkles[i])
                    m_sparkleBasePositions[i] = m_completionSparkles[i].rectTransform.anchoredPosition;
            }
        }

        m_visualStateCached = true;
    }

    private int GetValidCompletionSlotCount()
    {
        if (m_completionRoots == null || m_completionCanvasGroups == null || m_completionTexts == null)
            return 0;

        return Mathf.Min(
            k_completionSlotCount,
            Mathf.Min(m_completionRoots.Length, Mathf.Min(m_completionCanvasGroups.Length, m_completionTexts.Length)));
    }

    private void StopVisualFeedback()
    {
        CacheVisualState();

        if (m_promptShakeTween.isAlive)
            m_promptShakeTween.Stop();
        if (m_promptColorTween.isAlive)
            m_promptColorTween.Stop();
        if (m_timerPulseTween.isAlive)
            m_timerPulseTween.Stop();

        for (int i = 0; i < m_completionSequences.Length; i++)
            StopCompletionSlot(i);

        ResetVisualFeedback();
    }

    private void StopCompletionSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= m_completionSequences.Length)
            return;

        if (m_completionSequences[slotIndex].isAlive)
            m_completionSequences[slotIndex].Stop();

        if (m_completionCanvasGroups != null
            && slotIndex < m_completionCanvasGroups.Length
            && m_completionCanvasGroups[slotIndex])
        {
            m_completionCanvasGroups[slotIndex].alpha = 0f;
        }
    }

    private void ResetVisualFeedback()
    {
        if (m_promptRoot)
            m_promptRoot.anchoredPosition = m_promptBasePosition;
        if (m_promptBorder)
            m_promptBorder.color = s_promptBorderColor;
        if (m_timerRoot)
            m_timerRoot.localScale = m_timerBaseScale;

        int slotCount = GetValidCompletionSlotCount();
        for (int i = 0; i < slotCount; i++)
        {
            m_completionRoots[i].anchoredPosition = m_completionBasePositions[i];
            m_completionRoots[i].localScale = Vector3.one;
            m_completionCanvasGroups[i].alpha = 0f;
            m_completionTexts[i].SetText(string.Empty);
        }

        if (m_completionSparkles == null)
            return;

        int sparkleCount = Mathf.Min(m_completionSparkles.Length, m_sparkleBasePositions.Length);
        for (int i = 0; i < sparkleCount; i++)
        {
            if (!m_completionSparkles[i])
                continue;

            RectTransform sparkleRect = m_completionSparkles[i].rectTransform;
            sparkleRect.anchoredPosition = m_sparkleBasePositions[i];
            sparkleRect.localScale = Vector3.one * 0.35f;
        }
    }

    private void PlaySFX(AudioClipType clipType)
    {
        AudioClipManager.instance.PlaySFX(clipType);
    }

    private void OnDisable()
    {
        StopVisualFeedback();
    }
}
