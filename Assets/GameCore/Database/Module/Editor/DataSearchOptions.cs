using Sirenix.OdinInspector.Editor;
using System;

namespace GameCore.Database.Editor
{
    /// <summary>
    /// Optional filters for advanced searching. Drawn with <see cref="PropertyTree"/> by default (can be overrided).
    /// </summary>
    public abstract class DataSearchOptions<T> where T : Data
    {
        public event Action<DataSearchOptions<T>> OnChanged;
        public bool Enabled { get; protected set; } = false;
        protected virtual bool DrawsPropertyTree { get; } = true;
        private readonly PropertyTree m_propertyTree;

        public DataSearchOptions()
        {
            if (DrawsPropertyTree)
                m_propertyTree = PropertyTree.Create(this);
        }

        public abstract bool Filter(T data);

        public virtual void OnGUI() => m_propertyTree?.Draw(false);

        public virtual void Reset() => Enabled = false;

        protected void RaiseOptionsChanged() => OnChanged?.Invoke(this);
    }
}