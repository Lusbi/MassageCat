using System.IO;
using UnityEngine;

public class StorageManager : GameCore.Singleton<StorageManager>
{
    private StorageData m_storageData;

    public StorageData StorageData
    {
        get { return m_storageData; }
    }

    public StorageManager()
    {
        m_storageData = new StorageData();
    }

    public void SetStorageData(StorageData storageData)
    {
        m_storageData = storageData;
    }

    public void SaveStorageData()
    {
        string json = JsonUtility.ToJson(m_storageData, true);
        string path = Path.Combine(Application.persistentDataPath, "StorageData.json");
        File.WriteAllText(path, json);
        Debug.Log($"Storage data saved to: {path}");
    }

    public void LoadStorageData()
    {
        string path = Path.Combine(Application.persistentDataPath, "StorageData.json");
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            m_storageData = JsonUtility.FromJson<StorageData>(json);
            Debug.Log("Storage data loaded successfully.");
        }
        else
        {
            Debug.LogWarning("No storage data file found. Initializing new storage data.");
            m_storageData = new StorageData();
        }
    }
}
