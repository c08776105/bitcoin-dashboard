using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace DashboardServer.DTOs;

public class VersionPayload
{
    private uint _version;
    private ulong _services;
    private ulong _timestamp;
    private byte[] _addrLocal;
    private byte[] _addrPeer;
    private ulong _nonce;
    private byte[] _subversion;
    private uint _startHeight;

    private const ushort VERSION_BYTES = 4;
    private const ushort SERVICES_BYTES = 8;
    private const ushort TIMESTAMP_BYTES = 8;
    private const ushort ADDR_LOCAL_BYTES = 26;
    private const ushort ADDR_PEER_BYTES = 26;
    private const ushort NONCE_BYTES = 8;
    // private const ushort SUB_VERSION_BYTES = 16;
    private const ushort START_HEIGHT_BYTES = 4;

    public byte[]? BuiltPayload { get; private set; }

    public VersionPayload(string nodeIp, ushort nodePort, uint version = 60002, ulong services = 1,
        string localIp = "127.0.0.1", ushort localPort = 8333)
    {
        _version = version;
        _services = services;
        _timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        _addrLocal = CreateNetworkAddress(localIp, localPort);
        _addrPeer = CreateNetworkAddress(nodeIp, nodePort);
        _nonce = (ulong)new Random().Next();
        _subversion = CreateSubVersion();
        _startHeight = 0;
    }

    private static byte[] CreateSubVersion()
    {
        string subVersion = "/Satoshi:0.7.2/";
        
        return new byte[] { (byte) subVersion.Length }
            .Concat(Encoding.ASCII.GetBytes(subVersion)).ToArray();
        // return new byte[] { 0x00 };
    }

    /// <summary>
    /// The network address structure contains the following:
    /// time (4 bytes) not applicable for version messages
    /// services (8 bytes) same as service(s) used in version message
    /// ipv6/4 (16 bytes) network byte order address
    /// port (2 bytes) port number, network byte order
    ///
    /// network address is 26 bytes omitting the time bytes
    /// </summary>
    /// <param name="ipAddress"></param>
    /// <param name="port"></param>
    /// <returns></returns>
    private byte[] CreateNetworkAddress(string ipAddress, ushort port)
    {
        var ip = IPAddress.Parse(ipAddress);

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            ip = ip.MapToIPv6();
        }
        
        short portNetworkOrder = IPAddress.HostToNetworkOrder((short)port);


        byte[] networkAddress = new byte[26];
        byte[] servicesBytes = BitConverter.GetBytes(_services);
        byte[] ipBytes = ip.GetAddressBytes();
        byte[] portBytes = BitConverter.GetBytes(portNetworkOrder);
        Array.Copy(servicesBytes, 0, networkAddress, 0, servicesBytes.Length);
        Array.Copy(ipBytes, 0, networkAddress, 7, ipBytes.Length);
        Array.Copy(portBytes, 0, networkAddress, 23, 2);
        return networkAddress;
    }

    public byte[] BuildPayload()
    {
        BuiltPayload = BitConverter.GetBytes(_version) // 60002 (protocol version 60002) 4 bytes
            .Concat(BitConverter.GetBytes(_services)) // 1 (NODE_NETWORK services) 8 bytes
            .Concat(BitConverter.GetBytes(_timestamp)) // Tue Dec 18 10:12:33 PST 2012 8 bytes
            .Concat(_addrPeer) // Recipient address info 26 bytes
            .Concat(_addrLocal) // Sender address info 26 bytes
            .Concat(BitConverter.GetBytes(_nonce)) // Node ID/nonce - 8 bytes
            .Concat(_subversion) // "/Satoshi:0.7.2/" sub-version string - 15 bytes
            .Concat(BitConverter.GetBytes(_startHeight)) // Last block sending node has is block #212672 - 4 bytes
            .ToArray();
        return BuiltPayload;
    }

    private string FormatBytes(byte[] bytesToFormat)
    {
        return $"{string.Join(" ", bytesToFormat.Select(b => string.Format("{0:X2}", b)))}";
    }

    public void PrintBytes()
    {
        StringBuilder bytesString = new();

        bytesString.AppendLine(FormatBytes(BitConverter.GetBytes(_version)));
        bytesString.AppendLine(FormatBytes(BitConverter.GetBytes(_services)));
        bytesString.AppendLine(FormatBytes(BitConverter.GetBytes(_timestamp)));
        bytesString.AppendLine(FormatBytes(_addrPeer));
        bytesString.AppendLine(FormatBytes(_addrLocal));
        bytesString.AppendLine(FormatBytes(BitConverter.GetBytes(_nonce)));
        bytesString.AppendLine(FormatBytes(_subversion));
        bytesString.AppendLine(FormatBytes(BitConverter.GetBytes(_startHeight)));

        Console.WriteLine(bytesString.ToString());
    }

    public void PrintBuiltBytes()
    {
        if (BuiltPayload is not null)
        {
            int i = 0;

            var arrSegment = new ArraySegment<byte>(BuiltPayload, i, VERSION_BYTES);
            arrSegment = new ArraySegment<byte>(BuiltPayload, i, VERSION_BYTES);
            Console.WriteLine($"VERSION_BYTES: {FormatBytes(arrSegment.ToArray())}");
            i += VERSION_BYTES;

            arrSegment = new ArraySegment<byte>(BuiltPayload, i, SERVICES_BYTES);
            Console.WriteLine($"SERVICES_BYTES: {FormatBytes(arrSegment.ToArray())}");
            i += SERVICES_BYTES;

            arrSegment = new ArraySegment<byte>(BuiltPayload, i, TIMESTAMP_BYTES);
            Console.WriteLine($"TIMESTAMP_BYTES: {FormatBytes(arrSegment.ToArray())}");
            i += TIMESTAMP_BYTES;

            arrSegment = new ArraySegment<byte>(BuiltPayload, i, ADDR_PEER_BYTES);
            Console.WriteLine($"ADDR_PEER_BYTES: {FormatBytes(arrSegment.ToArray())}");
            i += ADDR_PEER_BYTES;

            arrSegment = new ArraySegment<byte>(BuiltPayload, i, ADDR_LOCAL_BYTES);
            Console.WriteLine($"ADDR_LOCAL_BYTES: {FormatBytes(arrSegment.ToArray())}");
            i += ADDR_LOCAL_BYTES;

            arrSegment = new ArraySegment<byte>(BuiltPayload, i, NONCE_BYTES);
            Console.WriteLine($"NONCE_BYTES: {FormatBytes(arrSegment.ToArray())}");
            i += NONCE_BYTES;

            arrSegment = new ArraySegment<byte>(BuiltPayload, i, _subversion.Length);
            Console.WriteLine($"USER_AGENT: {FormatBytes(arrSegment.ToArray())}");
            i += _subversion.Length;

            arrSegment = new ArraySegment<byte>(BuiltPayload, i, START_HEIGHT_BYTES);
            Console.WriteLine($"START_HEIGHT_BYTES: {FormatBytes(arrSegment.ToArray())}");
            i += START_HEIGHT_BYTES;


            Console.WriteLine($"i is {i}. Expected bytes: {BuiltPayload.Length}");
        }
        else
        {
            Console.WriteLine("Build the payload first");
        }
    }
}