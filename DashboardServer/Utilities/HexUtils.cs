using System.Text;

namespace DashboardServer.Utilities;

public class HexUtils
{
    /// <summary>
    /// Converts a hex string to a byte array. The hex string must have an even number of characters.
    /// </summary>
    /// <param name="hexString">A hex string with an even number of characters</param>
    /// <returns>A byte array representation of the hex string</returns>
    /// <exception cref="ArgumentException">If the hex string does not have an even number of characters</exception>
    public static byte[] HexStringToByteArray(string hexString)
    {
        if (hexString.Length % 2 != 0)
        {
            throw new ArgumentException("Hex string must have an even number of characters");
        }

        byte[] bytes = new byte[hexString.Length / 2];
        for (int i = 0; i < hexString.Length; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
        }

        return bytes;
    }
    
    /// <summary>
    /// Converts a byte array to a hex dump string.
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
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
    
    /// <summary>
    /// Prints a hex dump of the given byte array to the console.
    /// </summary>
    /// <param name="bytes">the byte array to print</param>
    public static void PrintHexDump(byte[] bytes)
    {
        Console.Write(GetHexDumpString(bytes));
    }
}