public class DramaProcess : IProcessEvent
{
    public ProcessType processType { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
    private string m_dramaKey;

    public DramaProcess(string dramaKey)
    {
        m_dramaKey = dramaKey;
    }
    public bool Execute()
    {
        // 發送事件，演出劇情
        processType = ProcessType.Processing;
        return true;
    }

    public void Inilization()
    {
        m_dramaKey = string.Empty;
        processType = ProcessType.None;
    }
}
