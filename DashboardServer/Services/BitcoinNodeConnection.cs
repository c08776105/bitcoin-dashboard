using System.Net.Sockets;
using System.Text;
using DashboardServer.DTOs;
using DashboardServer.Hubs;
using DashboardServer.Utilities;
using Microsoft.AspNetCore.SignalR;

namespace DashboardServer.Services;

/// <summary>
/// Interface for a Bitcoin node connection
/// </summary>
public interface IBitcoinNodeConnection
{
    public Task<string> ConnectToNode(string nodeIp, ushort nodePort);
    public Task ReceiveData();
    public void GetAddresses();
    public void Disconnect();
    public NodeState GetState();
}

/// <summary>
/// Represents a connection to a Bitcoin node. This class provides methods for connecting to a node, sending messages, and receiving responses.
/// </summary>
public class BitcoinNodeConnection : IBitcoinNodeConnection, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly IHubContext<BitcoinNodeHub> _bitcoinNodeHubContext;
    
    private TcpClient? _client;
    private NetworkStream? _stream;
    private Queue<NodeMessage> _writeQueue = new();
    
    private List<MessageReference> _sentMessages = new();
    private List<MessageReference> _receivedMessages = new();

    private string? _protocolVersion;
    private string? _userAgent;
    
    private List<InvVector> _invVectors = new();

    /// <summary>
    /// List of messages that can be sent and received by a Bitcoin node
    /// </summary>
    private static readonly string[] BITCOIN_NODE_MESSAGES =
    [
        "version", "verack", "addr", "inv", "getdata", "notfound", "getblocks", "getheaders", "tx", "block", "headers",
        "getaddr", "mempool", "checkorder", "submitorder", "reply", "ping", "pong", "reject", "sendheaders",
        "feefilter", "sendcmpct", "cmpctblock", "getblocktxn", "blocktxn"
    ];

    private bool _connected = false;

    /// <summary>
    /// Constructor for BitcoinNodeConnection
    /// </summary>
    /// <param name="configuration"></param>
    public BitcoinNodeConnection(IConfiguration configuration, IHubContext<BitcoinNodeHub> bitcoinNodeHubContext)
    {
        Console.WriteLine("Bitcoin node configuration constructor hit");
        _configuration = configuration;
        _bitcoinNodeHubContext = bitcoinNodeHubContext;
    }

    /// <summary>
    /// Connects to a Bitcoin node using the specified IP and port. This method should be called before sending any messages to the node.
    /// </summary>
    /// <param name="nodeIp"></param>
    /// <param name="nodePort"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<string> ConnectToNode(string nodeIp, ushort nodePort)
    {
        if (_connected)
        {
            if (_client is not null && !_client.Connected)
            {
                Console.WriteLine("Client no longer connected, disposing and reconnecting");
                _stream.Dispose();
                _client.Dispose();
            }
            else
            {
                throw new InvalidOperationException("Connection already established");
            }
        }

        Console.WriteLine("Connecting to Bitcoin node...");

        var ret = new StringBuilder();

        // Connect to a Bitcoin node (replace with the actual IP and port)
        _client = new TcpClient(nodeIp, nodePort);
        _stream = _client.GetStream();

        // Create a version payload
        var versionPayloadBuilder = new VersionPayload(nodeIp, nodePort);

        byte[] versionPayload = versionPayloadBuilder.BuildPayload();
        // ... fill in the version payload (similar to the Rust example)

        versionPayloadBuilder.PrintBytes();
        versionPayloadBuilder.PrintBuiltBytes();

        byte[] versionHeader = MessageUtils.GenerateHeader("version", versionPayload);

        byte[] versionMessage = new byte[versionHeader.Length + versionPayload.Length];

        Array.Copy(versionHeader, 0, versionMessage, 0, versionHeader.Length);
        Array.Copy(versionPayload, 0, versionMessage, versionHeader.Length, versionPayload.Length);

        // // Send the version payload
        await _stream.WriteAsync(versionMessage, 0, versionMessage.Length);
        
        _sentMessages.Add(new MessageReference(DateTime.Now, "version"));

        var versionResponse = await ReadNext();

        // Console.WriteLine($"Response message: {Encoding.Default.GetString(versionResponse.header)}");

        // ret = string.Join(" ", versionResponse.header.Select(b => string.Format("{0:X2}", b)));

        // Parse the response (e.g., check if it's a "version" message)
        Console.WriteLine("Hex dump of version message");
        HexUtils.PrintHexDump(versionMessage);

        Console.WriteLine("Hex dump of version response message");
        var versionHexDump = versionResponse.ToString();
        ret.Append(versionHexDump);
        Console.Write(versionHexDump);
        
        // Get the protocol version (first 4 bytes of the payload)
        _protocolVersion = BitConverter.ToString(versionResponse.payload.Take(4).ToArray());
        
        // Get the user agent (next 1 byte of the payload)
        _userAgent = Encoding.Default.GetString(versionResponse.payload.Skip(4).Take(1).ToArray());

        if (versionResponse.message != "version")
        {
            throw new InvalidOperationException(
                $"Expected version message, instead received {versionResponse.message}");
        }

        // Await the verack message 
        var verackResp = await ReadNext();

        var verackHexDump = verackResp.ToString();
        ret.Append(verackHexDump);
        Console.Write(verackHexDump);

        // Send a verack payload (similar occess)
        byte[] verackPayload = new byte[24];
        Array.Copy(verackResp.header, 0, verackPayload, 0, 24);
        await _stream.WriteAsync(verackPayload, 0, verackPayload.Length);
        _sentMessages.Add(new MessageReference(DateTime.Now, "verack"));

        Console.WriteLine(HexUtils.GetHexDumpString(verackPayload));

        // Set connect to true, so the background thread can start receiving inv packets
        _connected = true;
        Task.Run(() => ReceiveData());

        return ret.ToString();
    }

    /// <summary>
    /// Disconnects from the Bitcoin node. This method should be called when the connection is no longer needed.
    /// </summary>
    public void Disconnect()
    {
        if (_connected)
        {
            _connected = false;
            _stream.Dispose();
            _client.Dispose();

            Console.WriteLine("Disconnected from Bitcoin node");
        }
        else
        {
            throw new InvalidOperationException("Client not connected");
        }
    }

    /// <summary>
    /// Receives data from the Bitcoin node. This method should be called in a separate thread to continuously listen for messages from the node.
    /// </summary>
    public async Task ReceiveData()
    {
        while (_connected)
        {
            var dispatchState = false;
            
            // Check if there are any messages queued to write, otherwise continue reading next
            if (_writeQueue.Count > 0)
            {
                var message = _writeQueue.Dequeue();
                Console.WriteLine($"\nSending {message.message} message");
                await _stream.WriteAsync(message.payload, 0, message.payload.Length);
                _sentMessages.Add(new MessageReference(DateTime.Now, message.message));
                dispatchState = true;
            }

            var resp = await ReadNext();

            if (resp.message is not null && BITCOIN_NODE_MESSAGES.Contains(resp.message))
            {
                Console.WriteLine($"\nReceived {resp.message} message");
                _receivedMessages.Add(new MessageReference(DateTime.Now, resp.message));
                // Console.Write(resp.ToString());

                if (resp.message == "addr")
                {
                    HandleGetAddr(resp);
                }

                if (resp.message == "inv")
                {
                    HandleInv(resp);
                }

                dispatchState = true;
            }

            if (dispatchState)
            {
                await _bitcoinNodeHubContext.Clients.All.SendAsync("State", GetState());
            }
        }
    }

    /// <summary>
    /// Sends a "getaddr" message to the Bitcoin node to request a list of known addresses
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public void GetAddresses()
    {
        if (!_connected)
        {
            throw new InvalidOperationException("Connection not established");
        }

        if (_client.Connected == false)
        {
            throw new InvalidOperationException("TCP connection down");
        }

        // Create a getaddr payload
        var getaddrPayload = new byte[0];

        byte[] getaddrHeader = MessageUtils.GenerateHeader("getaddr", getaddrPayload);

        byte[] getaddrMessage = new byte[getaddrHeader.Length + getaddrPayload.Length];

        Array.Copy(getaddrHeader, 0, getaddrMessage, 0, getaddrHeader.Length);
        Array.Copy(getaddrPayload, 0, getaddrMessage, getaddrHeader.Length, getaddrPayload.Length);

        // Send the getaddr payload
        Console.WriteLine("Enqueueing getaddr message");
        var nodeMessage = new NodeMessage("getaddr", getaddrMessage);
        _writeQueue.Enqueue(nodeMessage);

        // Await the addr message
        // var addrResp = await ReadNext();
    }

    private void HandleGetAddr(NodeResponse addrResp)
    {
        // Parse the response (e.g., check if it's an "addr" message)
        Console.WriteLine("Hex dump of addr message");
        var hexDump = HexUtils.GetHexDumpString(addrResp.header);
        Console.Write(addrResp.ToString());

        // return hexDump;
    }

    /// <summary>
    /// Handles the inv message from the Bitcoin node
    /// </summary>
    /// <param name="invResponse"></param>
    private void HandleInv(NodeResponse invResponse)
    {
        // Parse the bitcoin node inv response payload
        using var stream = new MemoryStream(invResponse.payload);
        using var reader = new BinaryReader(stream);

        ulong itemCount = MessageUtils.ReadVarInt(reader);
        Console.WriteLine($"Item count: {itemCount}");

        for (ulong i = 0; i < itemCount; i++)
        {
            // Read and process each inventory vector...
            var type = reader.ReadUInt32();
            var hash = reader.ReadBytes(32);
            // Console.WriteLine($"Type: {type}, Hash: {BitConverter.ToString(hash)}");
            
            _invVectors.Add(new InvVector
            {
                Type = type,
                Hash = BitConverter.ToString(hash)
            });
        }
    }

    /// <summary>
    /// Reads the next message from the Bitcoin node. This method should be called when expecting a response from the node.
    /// </summary>
    /// <returns>The response object from the Bitcoin node</returns>
    private async Task<NodeResponse> ReadNext()
    {
        var nodeResponse = new NodeResponse();

        // Read 24 bytes (header length) from the Bitcoin node
        var b = await _stream.ReadAsync(nodeResponse.header, 0, 24);

        // Extract the magic bytes and message from the header
        nodeResponse.magicBytes = new ArraySegment<byte>(nodeResponse.header, 0, 4).ToArray();

        var messageBytesSegment = new ArraySegment<byte>(nodeResponse.header, 4, 12);
        nodeResponse.message = Encoding.Default.GetString(messageBytesSegment).Replace("\0", string.Empty);

        // Extract the payload size and checksum from the header
        var payloadSizeSegment = new ArraySegment<byte>(nodeResponse.header, 16, 4);
        var checksumSegment = new ArraySegment<byte>(nodeResponse.header, 20, 4);

        // Read the payload bytes from the Bitcoin node 
        int payloadSize = BitConverter.ToInt32(payloadSizeSegment);
        nodeResponse.payload = new byte[payloadSize];
        var c = await _stream.ReadAsync(nodeResponse.payload, 0, payloadSize);

        return nodeResponse;
    }

    /// <summary>
    /// Disposes of the connection to the Bitcoin node. 
    /// </summary>
    public void Dispose()
    {
        Console.WriteLine("Disposing of connection");
        _connected = false;
        _client.Dispose();
        _stream.Dispose();
    }

    public NodeState GetState()
    {
        return new NodeState
        {
            Connected = _connected,
            SentMessages = _sentMessages,
            ReceivedMessages = _receivedMessages,
            UserAgent = _userAgent,
            ProtocolVersion = _protocolVersion,
            InvVectors = _invVectors
        };
    }
}