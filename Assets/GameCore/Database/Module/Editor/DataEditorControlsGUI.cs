using Sirenix.OdinInspector.Editor;
using System;

namespace GameCore.Database.Editor
{
    /// <summary>
    /// Base class for GUI controls of data editor. Drawn with <see cref="PropertyTree"/> by default (can be overrided).
    /// </summary>
    public abstract class DataEditorControlsGUI<T> : IDisposable where T : Data
    {
        protected virtual bool DrawsPropertyTree { get; } = true;
        private readonly PropertyTree m_property;

        public DataEditorControlsGUI()
        {
            if (DrawsPropertyTree)
                m_property = PropertyTree.Create(this);
        }

        public void Dispose() => m_property?.Dispose();

        internal virtual void OnGUI()
        {
            m_property?.Draw(false);
        }
    }
}