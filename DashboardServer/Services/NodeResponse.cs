using System.Text;
using DashboardServer.Utilities;

namespace DashboardServer.Services;

public class NodeResponse
{
    public byte[] header;
    public byte[]? payload;

    public byte[] magicBytes;
    public string message;
    public int payloadSize;
    public byte[] checksum;

    public NodeResponse()
    {
        header = new byte[24];
        magicBytes = new byte[4];
        checksum = new byte[4];
    }

    public new string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Hex dump of {message} header");
        sb.Append(HexUtils.GetHexDumpString(header));

        if (payload is not null && payload.Length > 0)
        {
            sb.AppendLine($"Hex dump of {message} payload");
            sb.Append(HexUtils.GetHexDumpString(payload));
        }

        return sb.ToString();
    }
}