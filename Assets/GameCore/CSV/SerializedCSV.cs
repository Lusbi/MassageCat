
namespace GameCore.CSV
{
    public abstract class SerializedCSV
    {
        public SerializedCSV() { }
        public abstract string ToCSV();
        public abstract void FromCSV(string text);
    }
}