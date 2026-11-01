using System;
using System.Collections.Generic;
using GameCore.Database;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class QuestionInfo
{
    public string key;
    public string quesition;
    public string chineseQuesition;
}

[System.Serializable]
public class LevelQuestionInfo
{
    public int level;
    public List<QuestionInfo> questions;
}

[CreateAssetMenu(fileName = "SpellQuestion", menuName = "Scriptable Objects/SpellQuestion")]
public class SpellQuestion : ScriptableObject
{
    public List<LevelQuestionInfo> levelQuestionInfos = new List<LevelQuestionInfo>();

    public LevelQuestionInfo GetLevelQuestionInfo(string sceneMapKey)
    {
        Database<ScenemapData>.TryLoad(sceneMapKey, out var sceneData);
        int levelIndex = sceneData.SceneMapNo;

        foreach (var levelInfo in levelQuestionInfos)
        {
            if (levelInfo.level == levelIndex)
                return levelInfo;
        }
        return null;
    }


#if UNITY_EDITOR
    public GoogleSheetDownloaderReference googleSheetDownloaderReference;

    [Button("Download")]
    private void GoogleSheetDownloadButton()
    {
        googleSheetDownloaderReference?.Setup(
            (progress) =>
            {

                EditorUtility.DisplayProgressBar("SpellQuestion Settings...", "資料下載中…", progress);

            }, OnDownloadFinished);

        googleSheetDownloaderReference.DownLoad();

    }

    private void OnDownloadFinished(string text)
    {
#if UNITY_EDITOR
        EditorUtility.ClearProgressBar();
#endif
        // 解析 CSV 資料
        string[] lines = text.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        levelQuestionInfos.Clear();
        foreach (var line in lines)
        {
            if (line.Contains("//"))
                continue; // 跳過註解行

            // Key	Level	English	Chinese	SFI Rank	SFI	Adjusted Frequency per Million (U)
            string[] columns = line.Split(new[] { '\t', ',' }, StringSplitOptions.None);

            if (columns.Length < 4)
                continue;
            string key = columns[0].Trim();
            int level = int.TryParse(columns[1].Trim(), out int parsedLevel) ? parsedLevel : 0;
            string english = columns[2].Trim();
            string chinese = columns[3].Trim();

            LevelQuestionInfo levelInfo = GetOrCreateLevelQuestionInfo(level);
            QuestionInfo questionInfo = GetQuestionInfo(key, english, chinese);
            levelInfo.questions.Add(questionInfo);
        }

        AssetDatabase.SaveAssets();

    }

    private LevelQuestionInfo GetOrCreateLevelQuestionInfo(int level)
    {
        foreach (var levelInfo in levelQuestionInfos)
        {
            if (levelInfo.level == level)
                return levelInfo;
        }

        LevelQuestionInfo newLevelInfo = new LevelQuestionInfo
        {
            level = level,
            questions = new List<QuestionInfo>()
        };
        levelQuestionInfos.Add(newLevelInfo);
        return newLevelInfo;
    }
    private QuestionInfo GetQuestionInfo(string key, string english, string chinese)
    {
        QuestionInfo questionInfo = new QuestionInfo
        {
            key = key,
            quesition = english,
            chineseQuesition = chinese
        };
        return questionInfo;
    }
#endif
}
