using System.Security.Cryptography;
using System.Text;

namespace DashboardServer.Utilities;

public static class MessageUtils
{
    public static byte[] GenerateHeader(string headerString, byte[] payload)
    {
        byte[] magicBytes = new byte[4]{ 0xF9, 0xBE, 0xB4, 0xD9 };
        byte[] fullHeaderBytes = new byte[12]; // 76 65 72 73 69 6F 6E 00 00 00 00 00

        byte[] headerStringBytes = Encoding.UTF8.GetBytes(headerString);
        Array.Copy(headerStringBytes, 0, fullHeaderBytes, 0, headerStringBytes.Length);

        byte[] payloadLength = new byte[4] { 0x64, 0x00, 0x00, 0x00 };
        byte[] payloadChecksum = CalculateChecksum(payload);

        byte[] header = new byte[24];
        Array.Copy(magicBytes, 0, header, 0, 4);
        Array.Copy(fullHeaderBytes, 0, header, 4, 12);
        
        Array.Copy(payloadLength, 0, header, 16, 4);        Array.Copy(payloadLength, 0, header, 16, 4);
        Array.Copy(payloadChecksum, 0, header, 20, 4);

        return header;
    }
    
    /// <summary>
    /// In Bitcoin, the payload checksum is a 4-byte hash that is used to verify the integrity of the payload.
    /// It’s calculated by performing a double SHA256 hash on the payload and then taking the first 4 bytes of the result.
    /// TODO: Move this into a helper class
    /// </summary>
    /// <param name="payload"></param>
    /// <returns></returns>
    private static byte[] CalculateChecksum(byte[] payload)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hash1 = sha256.ComputeHash(payload);
            byte[] hash2 = sha256.ComputeHash(hash1);
            byte[] checksum = new byte[4];
            Array.Copy(hash2, checksum, 4);
            return checksum;
        }
    }
    
    /// <summary>
    /// Reads a variable length integer from the given BinaryReader.
    /// </summary>
    /// <param name="reader"></param>
    /// <returns></returns>
    public static ulong ReadVarInt(BinaryReader reader)
    {
        byte prefix = reader.ReadByte();

        switch (prefix)
        {
            case 0xfd:
                return reader.ReadUInt16();
            case 0xfe:
                return reader.ReadUInt32();
            case 0xff:
                return reader.ReadUInt64();
            default:
                return prefix;
        }
    }
}