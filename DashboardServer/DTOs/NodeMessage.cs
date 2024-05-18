namespace DashboardServer.DTOs;

/// <summary>
///  This class represents a message from the Bitcoin node.
/// </summary>
/// <param name="message">The message type</param>
/// <param name="payload">The message payload byte array</param>
public class NodeMessage(string message, byte[] payload)
{
    public string message = message;
    public byte[] payload = payload;
}