using System.Text.Json.Serialization;

namespace DashboardServer.DTOs;

public class NodeState
{
    [JsonPropertyName("connected")]
    public bool Connected { get; set; }
    
    [JsonPropertyName("sentMessages")]
    public List<MessageReference> SentMessages { get; set; }
    
    [JsonPropertyName("receivedMessages")]
    public List<MessageReference> ReceivedMessages { get; set; }
    
    [JsonPropertyName("userAgent")]
    public string UserAgent { get; set; }
    
    [JsonPropertyName("protocolVersion")]
    public string ProtocolVersion { get; set; }
    
    [JsonPropertyName("invVectors")]
    public List<InvVector> InvVectors { get; set; }
}