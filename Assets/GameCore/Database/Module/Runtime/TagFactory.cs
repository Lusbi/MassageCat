#if UNITY_EDITOR
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GameCore.Database
{
    [CreateAssetMenu(fileName = "TagFactory", menuName = "JSL 通用/TagFactory")]
    public class TagFactory : ScriptableObject
    {
        private const string TEMPLATE_TAG = "Assets/JSLCore/Modules/Database/Editor/Templates/Template_Tag.txt";
        public GoogleSheetDownloaderReference googleSheetDownloaderReference = new GoogleSheetDownloaderReference();
        [FolderPath, LabelText("儲存位置")]
        public string saveFolder;

        [Button("執行")]
        public void Execute()
        {
            googleSheetDownloaderReference.Setup(OnDownloadProgressChanaged, OnDownloadFinish);
            googleSheetDownloaderReference.DownLoad();
        }

        private void OnDownloadProgressChanaged(float obj)
        {
            EditorUtility.DisplayProgressBar("編輯器運作用", "資料下載中…", obj);
        }

        private void OnDownloadFinish(string obj)
        {
            Create(obj);
            EditorUtility.ClearProgressBar();
        }

        private string m_originalTexts = string.Empty;
        private List<string> m_downloadTexts = new List<string>();
        private void Create(string resultTexts)
        {
            LoadTextAsset();
            SplitResults(resultTexts);
            foreach (string texts in m_downloadTexts)
            {
                CreateEnum(texts);
            }
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }

        private void LoadTextAsset()
        {
            TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(TEMPLATE_TAG);
            if (textAsset != null)
            {
                m_originalTexts = textAsset.text;
            }
        }

        private void SplitResults(string resultTexts)
        {
            string[] values = resultTexts.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            m_downloadTexts = new List<string>(values);
        }

        private void CreateEnum(string values)
        {
            // CSV 格式
            string[] fields = values.Split(",");
            string enumName = fields[0];
            string enumValue = string.Empty;
            string allValue = string.Empty;
            for (int i = 1; i < fields.Length; i++)
            {
                if (string.IsNullOrEmpty(fields[i]))
                {
                    continue;
                }
                enumValue += $"\t\t{fields[i]} = 1 << {i},\n";
                allValue += $"{fields[i]}|";
            }
            enumValue += $"\t\tAll ={allValue.Substring(0, allValue.Length - 1)}";

            // 建立
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(m_originalTexts);
            stringBuilder.Replace("$ENUM_NAME", enumName);
            stringBuilder.Replace("$ENUM_TYPES", enumValue);

            StreamWriter streamWriter = new StreamWriter(saveFolder + $"/{enumName}.cs");
            streamWriter.Write(stringBuilder.ToString());
            streamWriter.Close();
        }
    }
}
#endif