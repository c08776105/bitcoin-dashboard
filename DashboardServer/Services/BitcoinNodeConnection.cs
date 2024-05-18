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
    public void GetData(string hash, uint type);
    public Task Disconnect();
    public NodeState GetState();
    public string GetMessagePayload(string id);
}

/// <summary>
/// Represents a connection to a Bitcoin node. This class provides methods for connecting to a node, sending messages, and receiving responses.
/// </summary>
public class BitcoinNodeConnection : IBitcoinNodeConnection, IDisposable
{
    private readonly IHubContext<BitcoinNodeHub> _bitcoinNodeHubContext;

    private TcpClient? _client;
    private NetworkStream? _stream;
    private readonly Queue<NodeMessage> _writeQueue = new();

    private readonly List<MessageReference> _sentMessages = new();
    private readonly List<MessageReference> _receivedMessages = new();

    private string? _protocolVersion;
    private string? _userAgent;
    private string? _nodeIpPort;

    private readonly List<InvVector> _invVectors = new();
    private readonly Dictionary<string, string> _messagePayloads = new();
    
    private readonly List<TxRecord> _txRecords = new();
    private readonly List<string> _blocks = new();

    /// <summary>
    /// List of messages that can be sent and received by a Bitcoin node
    /// </summary>
    private static readonly string[] BitcoinNodeMessages =
    [
        "version", "verack", "addr", "inv", "getdata", "notfound", "getblocks", "getheaders", "tx", "block", "headers",
        "getaddr", "mempool", "checkorder", "submitorder", "reply", "ping", "pong", "reject", "sendheaders",
        "feefilter", "sendcmpct", "cmpctblock", "getblocktxn", "blocktxn"
    ];

    private bool _connected;

    /// <summary>
    /// Constructor for BitcoinNodeConnection
    /// </summary>
    /// <param name="bitcoinNodeHubContext"></param>
    public BitcoinNodeConnection(IHubContext<BitcoinNodeHub> bitcoinNodeHubContext)
    {
        Console.WriteLine("Bitcoin node configuration constructor hit");
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
                _stream?.Dispose();
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

        var messageId = Guid.NewGuid().ToString();
        _sentMessages.Add(new MessageReference(DateTime.Now, "version", messageId));
        _messagePayloads.Add(messageId, HexUtils.GetHexDumpString(versionMessage));

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
        if (versionResponse.payload != null)
        {
            _protocolVersion = BitConverter.ToString(versionResponse.payload.Take(4).ToArray());

            // Get the user agent (next 1 byte of the payload)
            _userAgent = Encoding.Default.GetString(versionResponse.payload.Skip(4).Take(1).ToArray());
        }
        else
        {
            throw new MissingFieldException("Missing expected payload from Version response");
        }

        if (versionResponse.message != "version")
        {
            throw new InvalidOperationException(
                $"Expected version message, instead received {versionResponse.message}");
        }

        var versionMessageId = Guid.NewGuid().ToString();
        _receivedMessages.Add(new MessageReference(DateTime.Now, versionResponse.message, versionMessageId));
        _messagePayloads.Add(versionMessageId, versionHexDump);

        // Await the verack message 
        var verackResp = await ReadNext();

        var verackHexDump = verackResp.ToString();
        ret.Append(verackHexDump);
        Console.Write(verackHexDump);

        var verackMessageId = Guid.NewGuid().ToString();
        _receivedMessages.Add(new MessageReference(DateTime.Now, verackResp.message, verackMessageId));
        _messagePayloads.Add(verackMessageId, verackHexDump);

        // Send a verack payload (similar occess)
        byte[] verackPayload = new byte[24];
        Array.Copy(verackResp.header, 0, verackPayload, 0, 24);
        await _stream.WriteAsync(verackPayload, 0, verackPayload.Length);

        var messageId2 = Guid.NewGuid().ToString();
        _sentMessages.Add(new MessageReference(DateTime.Now, "verack", messageId2));
        _messagePayloads.Add(messageId2, HexUtils.GetHexDumpString(verackPayload));

        Console.WriteLine(HexUtils.GetHexDumpString(verackPayload));

        // Set connect to true, so the background thread can start receiving inv packets
        _nodeIpPort = $"{nodeIp}:{nodePort}";
        _connected = true;
        
        // Start the background thread to receive data from the Bitcoin node
        _ = Task.Run(ReceiveData);

        return ret.ToString();
    }

    /// <summary>
    /// Disconnects from the Bitcoin node. This method should be called when the connection is no longer needed.
    /// </summary>
    public async Task Disconnect()
    {
        if (_connected)
        {
            _connected = false;
            _stream?.Dispose();
            _client?.Dispose();

            _writeQueue.Clear();
            _sentMessages.Clear();
            _receivedMessages.Clear();
            _invVectors.Clear();
            _messagePayloads.Clear();
            _protocolVersion = null;
            _userAgent = null;
            _nodeIpPort = null;

            await _bitcoinNodeHubContext.Clients.All.SendAsync("State", GetState());

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
        Console.Write("Receiving data from Bitcoin node");
        while (_connected)
        {
            var dispatchState = false;

            // Check if there are any messages queued to write, otherwise continue reading next
            if (_writeQueue.Count > 0)
            {
                var message = _writeQueue.Dequeue();
                Console.WriteLine($"\nSending {message.message} message");

                var messageId = Guid.NewGuid().ToString();
                await _stream.WriteAsync(message.payload, 0, message.payload.Length);
                _sentMessages.Add(new MessageReference(DateTime.Now, message.message, messageId));
                _messagePayloads.Add(messageId, HexUtils.GetHexDumpString(message.payload));

                if (message.message == "getdata")
                {
                    Console.WriteLine(HexUtils.GetHexDumpString(message.payload));
                }

                dispatchState = true;
            }

            var resp = await ReadNext();

            if (BitcoinNodeMessages.Contains(resp.message))
            {
                Console.WriteLine($"\nReceived {resp.message} message");

                var messageId = Guid.NewGuid().ToString();
                _receivedMessages.Add(new MessageReference(DateTime.Now, resp.message, messageId));
                _messagePayloads.Add(messageId, resp.ToString());
                // Console.Write(resp.ToString());

                if (resp.message == "addr")
                {
                    HandleGetAddr(resp);
                }

                if (resp.message == "inv")
                {
                    HandleInv(resp);
                }

                if (resp.message == "tx")
                {
                    HandleTx(resp);
                }

                if (resp.message == "block")
                {
                    HandleBlock(resp);
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

        if (_client?.Connected == false)
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

    /// <summary>
    /// Enqueues a getdata payload to the write queue.
    /// The payload will be sent to the Bitcoin node in the next iteration of the ReceiveData loop.
    /// The Bitcoin node will respond with either a tx or block message, depending on the type of data requested.
    /// </summary>
    /// <param name="hash">The hash in which to get data for</param>
    /// <param name="type">an integer repsentation of the hash type (1 = tx etc.)</param>
    public void GetData(string hash, uint type)
    {
        if (!_connected)
        {
            throw new InvalidOperationException("Connection not established");
        }

        if (_client?.Connected == false)
        {
            throw new InvalidOperationException("TCP connection down");
        }

        // Create a getdata payload
        var count = 1; // We will always request with a single inventory vector
        var countBytes = BitConverter.GetBytes(count);
        var typeBytes = BitConverter.GetBytes(type);
        var hashBytes = HexUtils.HexStringToByteArray(hash);
        var getdataPayload = new byte[countBytes.Length + typeBytes.Length + hashBytes.Length];

        Array.Copy(countBytes, 0, getdataPayload, 0, countBytes.Length);
        Array.Copy(typeBytes, 0, getdataPayload, countBytes.Length, typeBytes.Length);
        Array.Copy(hashBytes, 0, getdataPayload, countBytes.Length + typeBytes.Length, hashBytes.Length);

        byte[] getdataHeader = MessageUtils.GenerateHeader("getdata", getdataPayload);

        byte[] getdataMessage = new byte[getdataHeader.Length + getdataPayload.Length];

        Array.Copy(getdataHeader, 0, getdataMessage, 0, getdataHeader.Length);
        Array.Copy(getdataPayload, 0, getdataMessage, getdataHeader.Length, getdataPayload.Length);

        // Send the getdata payload
        Console.WriteLine("Enqueueing getdata message");
        var nodeMessage = new NodeMessage("getdata", getdataMessage);
        _writeQueue.Enqueue(nodeMessage);
    }

    private void HandleGetAddr(NodeResponse addrResp)
    {
        // Parse the response (e.g., check if it's an "addr" message)
        Console.WriteLine("Hex dump of addr message");
        Console.Write(addrResp.ToString());
    }

    /// <summary>
    /// Handles the inv message from the Bitcoin node
    /// </summary>
    /// <param name="invResponse"></param>
    private void HandleInv(NodeResponse invResponse)
    {
        // Parse the bitcoin node inv response payload
        if (invResponse.payload != null)
        {
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
    }
    
    /// <summary>
    /// Handles parsing the block message from the Bitcoin node. Adds the block hash to the list of blocks that
    /// are returned as part of the node state.
    /// </summary>
    /// <param name="blockResp"></param>
    private void HandleBlock(NodeResponse blockResp)
    {
        // Parse the bitcoin node block response payload
        if (blockResp.payload != null)
        {
            using var stream = new MemoryStream(blockResp.payload);
            using var reader = new BinaryReader(stream);

            var block = reader.ReadBytes(blockResp.payload.Length);
            _blocks.Add(BitConverter.ToString(block));
        }
    }

    /// <summary>
    /// Handles parsing the tx message from the Bitcoin node. Adds the tx record to the list of tx records.
    /// </summary>
    /// <param name="txResp"></param>
    private void HandleTx(NodeResponse txResp)
    {
        // Parse the bitcoin node tx response payload
        if (txResp.payload != null)
        {
            using var stream = new MemoryStream(txResp.payload);
            using var reader = new BinaryReader(stream);

            var txIn = new List<TxInput>();
            var txOut = new List<TxOutput>();
            var txWitness = new List<string>();

            // Read the values from the tx payload
            var version = reader.ReadInt32();
            var txInCount = (int)MessageUtils.ReadVarInt(reader);
            for (int i = 0; i < txInCount; i++)
            {
                var txInHash = reader.ReadBytes(32);
                var txInIndex = reader.ReadUInt32();
                var scriptLength = (int)MessageUtils.ReadVarInt(reader);
                var scriptSig = reader.ReadBytes(scriptLength);
                var sequence = reader.ReadUInt32();
                txIn.Add(new TxInput(
                        txInHash,
                        txInIndex,
                        (uint) scriptLength,
                        scriptSig,
                        sequence
                    )
                );
            }

            var txOutCount = (int)MessageUtils.ReadVarInt(reader);
            for (int i = 0; i < txOutCount; i++)
            {
                var value = reader.ReadUInt64();
                var scriptLength = (int)MessageUtils.ReadVarInt(reader);
                var scriptPubKey = reader.ReadBytes(scriptLength);
                txOut.Add(new TxOutput(
                        value,
                        (uint)scriptLength,
                        scriptPubKey
                    )
                );
            }

            var witnessCount = (int)MessageUtils.ReadVarInt(reader);
            for (int i = 0; i < witnessCount; i++)
            {
                var witnessLength = (int)MessageUtils.ReadVarInt(reader);
                var witness = reader.ReadBytes(witnessLength);
                txWitness.Add(BitConverter.ToString(witness));
            }

            var lockTime = reader.ReadUInt32();
        
            var txRecord = new TxRecord(
                BitConverter.ToString(txResp.magicBytes),
                (uint)version,
                (uint)txInCount,
                txIn,
                (uint)txOutCount,
                txOut,
                txWitness,
                lockTime
            );
        
            _txRecords.Add(txRecord);
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
        _ = await _stream.ReadAsync(nodeResponse.header, 0, 24);

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
        _ = await _stream.ReadAsync(nodeResponse.payload, 0, payloadSize);

        return nodeResponse;
    }

    /// <summary>
    /// Disposes of the connection to the Bitcoin node. 
    /// </summary>
    public void Dispose()
    {
        Console.WriteLine("Disposing of connection");
        _connected = false;
        _client?.Dispose();
        _stream?.Dispose();
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
            InvVectors = _invVectors,
            TxRecords = _txRecords,
            Blocks = _blocks,
            NodeIpPort = _nodeIpPort
        };
    }

    public string GetMessagePayload(string id)
    {
        return _messagePayloads[id];
    }
}