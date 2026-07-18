using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Modules.Audio.Editor
{
    [CustomEditor(typeof(AudioLibrary))]
    public class AudioLibraryEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            AudioLibrary library = (AudioLibrary)target;

            GUILayout.Space(10);
            if (GUILayout.Button("Load All AudioData", GUILayout.Height(30)))
            {
                LoadAllAudioData(library);
            }
        }

        private void LoadAllAudioData(AudioLibrary library)
        {
            // 搜尋專案中所有的 AudioData
            string[] guids = AssetDatabase.FindAssets("t:AudioData");
            int addedCount = 0;

            // 建立現有 Key 的 HashSet 以避免重複
            HashSet<string> existingKeys = new HashSet<string>();
            foreach (var entry in library.entries)
            {
                if (!string.IsNullOrEmpty(entry.key))
                {
                    existingKeys.Add(entry.key);
                }
            }
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AudioData data = AssetDatabase.LoadAssetAtPath<AudioData>(path);

                if (data != null)
                {
                    // 使用 AudioClip 名稱作為 Key (若 clip 為空則用 Asset 名稱)
                    string key = data.clip != null ? data.clip.name : data.name.Replace("Data_", "");
                    
                    // 如果 Key 尚未存在，則加入
                    if (!existingKeys.Contains(key))
                    {
                        library.entries.Add(new AudioLibrary.AudioEntry
                        {
                            key = key,
                            data = data
                        });
                        existingKeys.Add(key);
                        addedCount++;
                    }
                }
            }

            if (addedCount > 0)
            {
                EditorUtility.SetDirty(library);
                Debug.Log($"[AudioLibrary] 已自動載入 {addedCount} 個新的 AudioData。");
            }
            else
            {
                Debug.Log("[AudioLibrary] 沒有發現新的 AudioData。");
            }
        }
    }
}
