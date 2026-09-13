using UnityEngine;
using UnityEditor;
using System.IO;

namespace Modules.Audio.Editor
{
    public class AudioEditorTools
    {
        private const string BASE_PATH = "Assets/SFX/Mixer/AudioData";
        private const string BGM_PATH = BASE_PATH + "/BGM";
        private const string SFX_PATH = BASE_PATH + "/SFX";

        [MenuItem("CI/Audio/BGM")]
        public static void CreateBGMData()
        {
            CreateAudioDataForSelection(BGM_PATH, "BGM");
        }

        [MenuItem("CI/Audio/SFX")]
        public static void CreateSFXData()
        {
            CreateAudioDataForSelection(SFX_PATH, "SFX");
        }

        private static void CreateAudioDataForSelection(string targetFolder, string typeName)
        {
            // 確保目錄存在
            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
                AssetDatabase.Refresh();
            }

            int count = 0;
            foreach (Object obj in Selection.objects)
            {
                if (obj is AudioClip clip)
                {
                    string assetName = $"Data_{clip.name}";
                    string fullPath = $"{targetFolder}/{assetName}.asset";

                    // 檢查是否已存在
                    if (File.Exists(fullPath))
                    {
                        Debug.LogWarning($"[AudioEditor] 檔案已存在，跳過: {fullPath}");
                        continue;
                    }

                    // 建立 AudioData
                    AudioData data = ScriptableObject.CreateInstance<AudioData>();
                    data.clip = clip;
                    
                    // 嘗試自動設定 Mixer Group (若有對應名稱的 Group)
                    // 這裡僅作簡單預設，實際 Group 需由使用者手動指定或進階邏輯
                    // data.outputGroup = ... 

                    AssetDatabase.CreateAsset(data, fullPath);
                    count++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[AudioEditor] 已建立 {count} 個 {typeName} AudioData 於 {targetFolder}");
        }

        // 驗證函式：只有選取 AudioClip 時才啟用選單
        [MenuItem("CI/Audio/BGM", true)]
        [MenuItem("CI/Audio/SFX", true)]
        private static bool ValidateCreateAudioData()
        {
            return Selection.activeObject is AudioClip;
        }
    }
}
