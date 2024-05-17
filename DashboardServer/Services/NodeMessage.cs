namespace DashboardServer.Services;

public class NodeMessage(string message, byte[] payload)
{
    public string message = message;
    public byte[] payload = payload;
}