#if UNITY_EDITOR
using Sirenix.OdinInspector;
using UnityEngine;

namespace GameCore
{
    public class DeveloperToolBase
    {
        public bool HasUrl
        {
            get { return !string.IsNullOrEmpty(url); }
        }

        [PropertySpace(SpaceAfter = 10)][HideLabel , ShowIf(nameof(HasUrl)) , InlineButton("ToURL","Help") , PropertyOrder(-1)] public string teachURL = "";
        public virtual string url { get; }
        public virtual string DeveloperName { get; }
        protected DeveloperEditorWindow developerEditorWindow;
        public DeveloperToolBase()
        {

        }

        public DeveloperToolBase(DeveloperEditorWindow developerEditorWindow)
        {
            this.developerEditorWindow = developerEditorWindow;
        }

        private void ToURL()
        {
            Application.OpenURL(url);
        }

        public virtual void Initlization()
        {

        }
        public virtual void Tick(float time)
        {

        }
    }
}
#endif