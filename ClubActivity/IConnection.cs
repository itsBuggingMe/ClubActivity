namespace ClubActivity;

public interface IConnection
{
    public void Host();
    public void Connect();
    public string? TryGetData();
    public void SendData(string data);
    public bool IsConnected { get; }
}