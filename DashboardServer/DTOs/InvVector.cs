using System.Text.Json.Serialization;

namespace DashboardServer.DTOs;

public class InvVector
{
    [JsonPropertyName("type")]
    public uint Type { get; set; }
    
    [JsonPropertyName("hash")]
    public string Hash { get; set; }
}