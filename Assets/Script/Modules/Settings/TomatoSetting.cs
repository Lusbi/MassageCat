using System;
using GameCore.Database;
using GameCore.Utils;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "TomatoSetting", menuName = "Scriptable Objects/TomatoSetting")]
public class TomatoSetting : ScriptableObject
{
    public GoogleSheetDownloaderReference googleSheetDownloaderReference;
    public TomatoReference[] tomatoReferences = new TomatoReference[0];

    public float GetMinute(string roleKey)
    {
        foreach (TomatoReference tomatoRef in tomatoReferences)
        {
            foreach (var key in tomatoRef.roleKeys)
            {
                if (key == roleKey)
                {
                    return tomatoRef.minute;
                }
            }
        }
        return 0;
    }

#if UNITY_EDITOR
    [Button("下載資料")]
    private void Downloaded()
    {
        googleSheetDownloaderReference?.Setup((p) =>
        {
            EditorUtility.DisplayProgressBar("Tomato Settings...", "資料下載中…", p);

        }, OnDownloadFinish);
        googleSheetDownloaderReference.DownLoad();
    }

    private void OnDownloadFinish(string obj)
    {
        EditorUtility.ClearProgressBar();

        ArrayUtility.Clear(ref tomatoReferences);
        string[] lines = obj.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in lines)
        {
            if (line.Contains("key"))
                continue;

            string[] value = line.Split(new string[] { "\t", "," }, StringSplitOptions.RemoveEmptyEntries);
            if (value.Length < 2)
            {
                continue;
            }
            string key = value[0];
            string minute = value[1];

            var tomatoRef = GetTomatoReference(minute.ToInt());
            ArrayUtility.Add(ref tomatoRef.roleKeys, key);
        }
    }
    public TomatoReference GetTomatoReference(int minute)
    {
        foreach (var tomatoRef in tomatoReferences)
        {
            if (tomatoRef.minute == minute)
                return tomatoRef;
        }

        TomatoReference tomatoReference = new TomatoReference();
        tomatoReference.minute = minute;
        ArrayUtility.Add(ref tomatoReferences, tomatoReference);
        return tomatoReference;
    }
#endif
}

[System.Serializable]
public class TomatoReference
{
    public float minute;
    public string[] roleKeys = new string[0];
}