#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace GameCore.Database.Editor
{
    public static class DatabaseUser
    {
        public static event Action<string> OnUserChanged;
        private const string USER_NAME_KEY = "ClientDatabaseUser_{0}";

        public static bool HasUserName()
        {
            return !string.IsNullOrEmpty(GetUserName());
        }

        public static void SetUserName(string userName)
        {
            EditorPrefs.SetString(string.Format(USER_NAME_KEY, Application.productName), userName);
            OnUserChanged?.Invoke(userName);
        }

        public static string GetUserName()
        {
            return EditorPrefs.GetString(string.Format(USER_NAME_KEY, Application.productName), string.Empty);
        }
    }

    public readonly struct DatabaseUserScope : IDisposable
    {
        private readonly string m_oldUserName;

        public DatabaseUserScope(string name)
        {
            m_oldUserName = DatabaseUser.GetUserName();
            DatabaseUser.SetUserName(name);
        }

        public readonly void Dispose()
        {
            DatabaseUser.SetUserName(m_oldUserName);
        }
    }
}
#endif