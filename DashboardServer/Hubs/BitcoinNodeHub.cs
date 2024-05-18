using System.Text.Json;
using DashboardServer.DTOs;
using Microsoft.AspNetCore.SignalR;
using DashboardServer.Services;

namespace DashboardServer.Hubs;

/// <summary>
///    This class is a SignalR hub that will handle the communication between the server and the Bitcoin node.
/// </summary>
public class BitcoinNodeHub: Hub
{
    private readonly IBitcoinNodeConnection _nodeConnection;
    public BitcoinNodeHub(IBitcoinNodeConnection nodeConnection)
    {
        _nodeConnection = nodeConnection;
    }

    public async Task Echo(string echoString)
    {
        await Clients.Caller.SendAsync("Echo", echoString);
    }

    public async Task GetState()
    {
        NodeState jsonState = _nodeConnection.GetState();
        await Clients.Caller.SendAsync("State", jsonState);
    }
    
    public async Task ConnectToNode(string nodeIp, ushort nodePort)
    {
        Console.WriteLine("Received request to connect to bitcoin node");
        try
        {
            await _nodeConnection.ConnectToNode(nodeIp, nodePort);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("ConnectError", ex.Message);
            return;
        }
        
        await Clients.Caller.SendAsync("ConnectSuccess");
    }
    
    public async Task DisconnectFromNode()
    {
        try
        {
            _nodeConnection.Disconnect();
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("DisconectError", ex.Message);
            return;
        }
        
        await Clients.Caller.SendAsync("DisconnectSuccess");
    }

    public async Task SendGetAddresses()
    {
        try
        {
            _nodeConnection.GetAddresses();
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("SendGetAddressesError", ex.Message);
            return;
        }
        
        await Clients.Caller.SendAsync("SendGetAddressesSuccess");
    }
    
    // public async Task SendGetBlocks()
    // {
    //     _nodeConnection.GetBlocks();
    // }
    
    // public async Task SendGetHeaders()
    
}