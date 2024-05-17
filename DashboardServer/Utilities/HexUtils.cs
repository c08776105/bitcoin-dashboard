using System.Text;

namespace DashboardServer.Utilities;

public class HexUtils
{
    public static string GetHexDumpString(byte[] bytes)
    {
        var sb = new StringBuilder();
        
        for (int i = 0; i < bytes.Length; i += 16)
        {
            sb.Append($"{(i/16).ToString("d3")}0 ");
            for (int j = i; j < i + 16; ++j)
            {
                if (j < bytes.Length)
                    sb.Append(string.Format("{0:x2} ", bytes[j]));
                else
                    sb.Append("   ");
            }

            for (int j = i; j < i + 16 && j < bytes.Length; ++j)
            {
                if (bytes[j] < 32 || bytes[j] > 127)
                    sb.Append('.');
                else
                    sb.Append((char)bytes[j]);
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
    
    public static void PrintHexDump(byte[] bytes)
    {
        Console.Write(GetHexDumpString(bytes));
    }
}