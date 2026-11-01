namespace GameCore.Database.Editor
{
    public interface IDataEditorWindow
    {
        bool IsNewDocument { get; }
        void Repaint();
    }

    public interface IDataEditorWindow<T> : IDataEditorWindow where T : Data
    {
        bool IsEditing { get; }

        void Copy(ref T destination);
        T NewEditingCopy();
        bool Delete();
        void Duplicate();
        void Paste(T copied);
        void Refresh();
        void Revert();
        void Save();
        void OnDataSelected(T source);
    }
}