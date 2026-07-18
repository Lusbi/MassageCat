using System;
using System.Collections.Generic;
using Febucci.UI.Core;
using GameCore;
using GameCore.Audio;
using GameCore.Database;
using GameCore.Event;
using GameCore.Resources;
using Modules.SystemMessage;
using TMPro;
using UnityEngine;
using System.Text;

public enum SpellStopReason
{
    Manual,
    Forced,
    Timeout
}

public class SpellHandler : MonoBehaviour, IInitlization
{
    private const float k_defaultDurationTime = 300f;
    private const string k_spellAni = "Hit_Spell";
    private const string k_addressableKey = "SpellQuestion";
    private SpellQuestion m_spellQuestion;

    [SerializeField] private TextMeshProUGUI m_questingText;
    [SerializeField] private TextMeshProUGUI m_timeText;
    [SerializeField] private TextMeshProUGUI m_aniText;
    [SerializeField] private Animation m_animation;
    private string m_quest;
    private string m_answer;
    private string m_chineseQuest;

    private float m_durationTime = 0;

    private List<char> m_answerCharList = new List<char>();
    private readonly StringBuilder m_answerBuilder = new StringBuilder(64);

    private int m_correctCharCount = 0;
    private int m_incorrectCharCount = 0;
    private int m_completedQuestCount = 0;

    public int CorrectCharCount => m_correctCharCount;

    public int IncorrectCharCount => m_incorrectCharCount;

    public int CompletedQuestCount => m_completedQuestCount;

    private LevelQuestionInfo levelQuestionInfo;

    public bool isSpelling { get; private set; }

    public event Action<SpellStopReason> SpellStopped;

    public void Initlization(Action callBack = null)
    {
        CoreResoucesService.LoadAssetAsync<SpellQuestion>(k_addressableKey, s =>
        {
            m_spellQuestion = s;

            callBack?.Invoke();
        });

        Clear();
    }

    public void SpellStart()
    {
        Clear();
        ApplyQuest();
        UpdateAnswerText();
        m_durationTime = k_defaultDurationTime;
        isSpelling = true;
    }

    public void SpellStop(SpellStopReason reason = SpellStopReason.Manual)
    {
        Clear();
        m_questingText.text = string.Empty;
        UpdateTimeText();
        SpellStopped?.Invoke(reason);

        if (reason == SpellStopReason.Forced)
        {
            SystemMessageEvent evt = new SystemMessageEvent
            {
                Title = "Spell Interrupted",
                Content = "The spell was interrupted. Please try again.",
                OnConfirm = null
            };
            EventManager.instance.Notify(evt);
        }
    }

    private void Clear()
    {
        m_quest = string.Empty;
        m_answer = string.Empty;
        m_chineseQuest = string.Empty;
        m_answerCharList.Clear();
        m_correctCharCount = 0;
        m_incorrectCharCount = 0;
        m_completedQuestCount = 0;
        isSpelling = false;
        m_durationTime = 0;
    }

    private void ApplyQuest()
    {
        string roleKey = StorageManager.instance.StorageData.BattleStorageData.EnemyKey;
        Database<RoleData>.TryLoad(roleKey, out var roleData);
        string sceneMapKey = roleData.SceneReference.GetKey();
        levelQuestionInfo = m_spellQuestion.GetLevelQuestionInfo(sceneMapKey);
        if (levelQuestionInfo == null)
        {
            return;
        }

        var questionInfo = RandomQuestion();
        if (questionInfo == null)
        {
            return;
        }
        m_quest = questionInfo.quesition;
        m_chineseQuest = questionInfo.chineseQuesition;
        m_answer = string.Empty;
        m_answerCharList.Clear();
    }

    private QuestionInfo RandomQuestion()
    {
        if (levelQuestionInfo == null || levelQuestionInfo.questions.Count == 0)
            return null;

        int randomIndex = UnityEngine.Random.Range(0, levelQuestionInfo.questions.Count);
        var questionInfo = levelQuestionInfo.questions[randomIndex];
        m_chineseQuest = questionInfo.chineseQuesition;
        return questionInfo;
    }

    public bool InputChar(string inputChar)
    {
        if (string.IsNullOrEmpty(inputChar) || string.IsNullOrEmpty(m_quest))
            return false;

        char normalizedInput = inputChar[0];
        int answerIndex = m_answerCharList.Count;

        bool completed = false;
        if (answerIndex < m_quest.Length &&
            char.ToUpperInvariant(m_quest[answerIndex]) == char.ToUpperInvariant(normalizedInput))
        {
            char expectedChar = m_quest[answerIndex];
            m_answerCharList.Add(expectedChar);

            m_correctCharCount++;
            completed = m_answerCharList.Count == m_quest.Length;
            if (completed)
            {
                PlayAni();
                m_completedQuestCount++;
                
                PlaySFX(AudioClipType.Spell_SuceessSFX);
                
                ApplyQuest();
                UpdateAnswerText();
                return true;
            }
        }
        else
        {
            m_incorrectCharCount++;
            PlaySFX(AudioClipType.Spell_FailSFX);
        }

        UpdateAnswerText();
        return completed;
    }

    private void UpdateAnswerText()
    {
        m_answerBuilder.Clear();
        foreach (char character in m_answerCharList)
        {
            m_answerBuilder.Append(character);
        }
        m_answer = m_answerBuilder.ToString();
        m_questingText.text = string.Format("{0}\n<color=#33ff33>{1}</color>\n{2}", m_quest, m_answer, m_chineseQuest);
    }

    public void Tick(float deltaTime)
    {
        if (!isSpelling)
            return;

        if (m_durationTime > 0)
        {
            m_durationTime -= deltaTime;
            UpdateTimeText();
        }
        else
        {
            m_durationTime = 0;
            SpellStop(SpellStopReason.Timeout);
        }
    }

    private void UpdateTimeText()
    {
        if (m_timeText)
        {
            if (m_durationTime > 0)
            {
                int minutes = Mathf.FloorToInt(m_durationTime / 60);
                int sec = Mathf.FloorToInt(m_durationTime % 60);
                m_timeText.text = string.Format("{0:00}:{1:00}", minutes, sec);
            }
            else
            {
                m_timeText.text = "";
            }
        }
    }

    public void PlayAni()
    {
        m_answerBuilder.Clear();
        foreach (char character in m_answerCharList)
        {
            m_answerBuilder.Append(character);
        }
        m_answer = m_answerBuilder.ToString();
        m_aniText.text = string.Format("<color=#33ff33>{0}</color>", m_answer);
        m_aniText.rectTransform.anchoredPosition = new Vector2(0, m_aniText.rectTransform.anchoredPosition.y);

        m_animation.Play(k_spellAni);
        m_animation.Rewind();

        PrimeTween.Tween.Delay(m_animation.GetClip(k_spellAni).length + 0.2f, () =>
        {
            m_aniText.text = string.Empty;
            m_aniText.rectTransform.anchoredPosition = new Vector2(0, m_aniText.rectTransform.anchoredPosition.y);
        });
    }

    private void PlaySFX(AudioClipType clipType)
    {
        AudioClipManager.instance.PlaySFX(clipType);
    }
}