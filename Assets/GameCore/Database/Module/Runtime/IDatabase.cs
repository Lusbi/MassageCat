
namespace GameCore.Database
{
    public interface IDatabase
    {
        void Load();
        string[] GetKeys();
        Data GetData(string key);
        Data[] GetDatas();
    }
}