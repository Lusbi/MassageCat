public interface ICommand
{
    CommandType CommandType { get; }
    void Execute();
}
