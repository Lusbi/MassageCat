namespace GameCore.Database.Editor
{
    public interface IDataSelector<T> where T : Data
    {
        void Focus();
        void OnDelete(T data);
        void OnSave(T data, bool removesOldEntry, string oldKey);
        void Refresh();
        void Select(T data);
        bool TryGetData(out T data);
    }
}