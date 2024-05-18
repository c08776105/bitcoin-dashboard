using System.Text.Json.Serialization;

namespace DashboardServer.DTOs;

public class MessageReference(DateTime sentTime, string message)
{
    [JsonPropertyName("sentTime")]
    public DateTime SentTime { get; set; } = sentTime;

    [JsonPropertyName("message")]
    public string Message { get; set; } = message;
}