#if UNITY_EDITOR
using Sirenix.OdinInspector;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace GameCore.Database
{
    [HideMonoScript]
    public abstract partial class Data
    {
        public const char PATH_SEPARATOR = '/';
        public const char COMMENT_SEPARATOR = ':';

        [HideInInspector]
        [SerializeField]
        protected string m_titlePath = string.Empty;

        [HideInInspector]
        [SerializeField]
        protected string m_comment = string.Empty;

        protected const string KEY_COMMENT_FORMAT = "{0}: {1}";

        private const string DEFAULT_LOG_FORMAT = "key = {0}, comment = {1}";
        private const string FIND_REFERENCE = "FindReference";

        public virtual bool Filter(string filter) => string.IsNullOrEmpty(filter) || GetTitle().Contains(filter);

        public virtual string GetTitle([CallerMemberName] string memberName = null)
        {
            string keyAndComment = string.Format(KEY_COMMENT_FORMAT, key, m_comment);
            return string.IsNullOrEmpty(m_titlePath) ? keyAndComment : m_titlePath + PATH_SEPARATOR + keyAndComment;
        }

        public virtual string GetLog() => string.Format(DEFAULT_LOG_FORMAT, key, m_comment);

        public virtual string GetWarningLog() => string.Empty;

        public virtual string GetErrorLog() => string.Empty;


        /// <summary>給程式CI自動化設定使用</summary>
        public void ModifyTitleAndComment(string title, string comment)
        {
            m_titlePath = title;
            m_comment = comment;
        }

        
    }
}
#endif