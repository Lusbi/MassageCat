#if UNITY_EDITOR
using GameCore.Database.Editor;
using UnityEditor;

public static class RepositorySettingsHelper
{
    private const string KEY_FORMAT_DEFAULT_FILTER = "{0}_{1}_DefaultFilter";
    private const string KEY_FORMAT_SERIAL_NUMBER = "{0}_{1}_SerialNumber";
    private const string KEY_FORMAT_SERIAL_NUMBER_INTERVAL = "{0}_{1}_SerialNumberInterval";

    public static string GetDefaultFilter(string repoName)
    {
        string key = string.Format(KEY_FORMAT_DEFAULT_FILTER, DatabaseUser.GetUserName(), repoName);
        return EditorPrefs.GetString(key);
    }

    public static void SetDefaultFilter(string repoName, string value)
    {
        string key = string.Format(KEY_FORMAT_DEFAULT_FILTER, DatabaseUser.GetUserName(), repoName);
        EditorPrefs.SetString(key, value);
    }

    public static int GetSerialNumber(string repoName)
    {
        string key = string.Format(KEY_FORMAT_SERIAL_NUMBER, DatabaseUser.GetUserName(), repoName);
        return EditorPrefs.GetInt(key, 1);
    }

    public static void SetSerialNumber(string repoName, int value)
    {
        string key = string.Format(KEY_FORMAT_SERIAL_NUMBER, DatabaseUser.GetUserName(), repoName);
        EditorPrefs.SetInt(key, value);
    }

    public static int GetSerialNumberInterval(string repoName)
    {
        string key = string.Format(KEY_FORMAT_SERIAL_NUMBER_INTERVAL, DatabaseUser.GetUserName(), repoName);
        return EditorPrefs.GetInt(key, 1);
    }

    public static void SetSerialNumberInterval(string repoName, int value)
    {
        string key = string.Format(KEY_FORMAT_SERIAL_NUMBER_INTERVAL, DatabaseUser.GetUserName(), repoName);
        EditorPrefs.SetInt(key, value);
    }
}
#endif