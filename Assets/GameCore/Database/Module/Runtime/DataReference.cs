using GameCore.CSV;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using GameCore.Database.Editor;
using System.Linq;
using System.Reflection;
using UnityEditor;
#endif

namespace GameCore.Database
{
    [HideReferenceObjectPicker]
    public abstract class DataReference<T> : SerializedCSV where T : Data
    {
        private const string NULL = "Null";

        [SerializeField, ReadOnly] protected string m_key = NULL;

        public DataReference() { }
        public DataReference(string key)
        {
            SetKey(key);
        }

        public void SetKey(string key)
        {
            m_key = key;
            OnKeyChanged();
        }

        public string GetKey()
        {
            return m_key;
        }

        public bool TryGetKey(out string key)
        {
            key = m_key;
            return m_key != NULL;
        }

        /// <summary>
        /// Key是否有資料(Key不是"Null")
        /// </summary>
        public bool HasValidKey()
        {
            return (m_key != NULL);
        }

        public bool Exists()
        {
            return Database<T>.Exists(m_key);
        }

        public T Load()
        {
            return Database<T>.Load(m_key);
        }

        public bool TryLoad(out T value)
        {
            if (m_key == NULL)
            {
                value = null;
                return false;
            }

            return Database<T>.TryLoad(m_key, out value);
        }

        public void LoadAsync(Action<T> onDataLoaded)
        {
            Database<T>.LoadAsync(m_key, onDataLoaded);
        }

        public override string ToCSV()
        {
            return m_key;
        }

        public override void FromCSV(string text)
        {
            SetKey(text);
        }

#if UNITY_EDITOR
        [LabelText("搜尋")]
        [FormerlySerializedAs("filter")]
        [SerializeField]
        private string m_queryText = string.Empty;

        [InlineButton(nameof(UpdateWindowValue), SdfIconType.ArrowClockwise, "")]
        [InlineButton(nameof(OpenWindow), SdfIconType.PencilSquare, "")]
        [InlineButton(nameof(SettingUseSelfData), "$selfTips")]
        [OnValueChanged(nameof(OnTitleChanged))]
        [ValueDropdown(nameof(GetTitlesForValueDropdown))]
        [HideLabel]
        [SerializeField]
        private string m_selectedTitle = NULL;

        private static readonly Dictionary<string, string> keyCache = new();
        private Predicate<T> m_queryPredicate;
        private string m_currentUserName;
        private bool m_selfUser = false;
        private string selfTips => m_selfUser ? "自" : "全";

        /// <summary>使用<see cref="DatabaseEditorUtils"/>取得資料 (editor-only)</summary>
        public bool EditorTryLoad(out T value)
        {
            if (m_key == NULL)
            {
                value = null;
                return false;
            }

            value = DatabaseEditorUtils.LoadData<T>(m_key);
            return value != null;
        }

        public void AddQuery(Predicate<T> predicate) => m_queryPredicate += predicate;

        public void RemoveQuery(Predicate<T> predicate) => m_queryPredicate -= predicate;

        public void UpdateQueryText(string queryText)
        {
            m_queryText = queryText?.Trim();
        }

        public void SetTitle(string title)
        {
            m_selectedTitle = title;
            OnTitleChanged();
        }

        private IEnumerable<string> EnumerateTitles()
        {
            return DatabaseEditorUtils.EnumerateData<T>().Select(x => x.GetTitle());
        }

        private IEnumerable<string> GetTitlesForValueDropdown()
        {
            if (m_selfUser)
                m_currentUserName = DatabaseUser.GetUserName();
            return DatabaseEditorUtils.EnumerateData<T>().Where(FilterData).Select(x => x.GetTitle()).OrderBy(x => x, NaturalComparer.Default).Prepend(NULL);
        }

        private void OnKeyChanged()
        {
            if (string.IsNullOrEmpty(m_key))
            {
                m_selectedTitle = NULL;
            }
            else
            {
                m_selectedTitle = EnumerateTitles().FirstOrDefault(x => GetKeyFromTitle(x) == m_key);
            }
        }

        private void OnTitleChanged()
        {
            m_key = GetKeyFromTitle(m_selectedTitle);
        }

        private bool IsUserData(T data) => AssetDatabase.GetAssetPath(data).Contains(m_currentUserName);

        private bool FilterData(T data)
        {
            if (string.IsNullOrWhiteSpace(m_queryText) || data.Filter(m_queryText))
                return m_queryPredicate?.GetInvocationList().Cast<Predicate<T>>().All(x => x(data)) ?? true;
            return false;
        }

        private string GetKeyFromTitle(string title)
        {
            if (string.IsNullOrEmpty(title))
                return string.Empty;

            if (keyCache.TryGetValue(title, out var key))
                return key;

            int indexOfColon = title.LastIndexOf(Data.COMMENT_SEPARATOR);
            int indexOfSlash;
            if (indexOfColon == -1)
            {
                indexOfSlash = title.LastIndexOf(Data.PATH_SEPARATOR);
                key = indexOfSlash == -1 ? title : title[(indexOfSlash + 1)..];
            }
            else
            {
                indexOfSlash = title.LastIndexOf(Data.PATH_SEPARATOR, indexOfColon);
                key = title[(indexOfSlash + 1)..indexOfColon];
            }
            keyCache.Add(title, key);
            return key;
        }

        private void OpenWindow()
        {
            string typeName = string.Format("JSLCore.Database.Editor.{0}EditorWindow, Assembly-CSharp-Editor", DatabaseEditorUtils.ConvertDataName<T>(string.Empty));
            Type type = Type.GetType(typeName);
            MethodInfo methodInfo = type.GetMethod("OpenWindowWithKey", BindingFlags.Static | BindingFlags.NonPublic);
            methodInfo.Invoke(null, new object[] { m_key });
        }

        private void UpdateWindowValue()
        {
            DatabaseEditorUtils.UpdateRepositoryCaches<T>(true);
            if (Database<T>.TryLoad(m_key, out T result))
                SetTitle(result.GetTitle());
            else
                SetTitle(string.Empty);
        }

        private void SettingUseSelfData()
        {
            m_selfUser = !m_selfUser;
            if (m_selfUser)
                m_queryPredicate += IsUserData;
            else
                m_queryPredicate -= IsUserData;
        }
#else
        private void OnKeyChanged(){}
#endif
    }
}
