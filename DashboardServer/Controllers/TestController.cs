using DashboardServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace DashboardServer.Controllers;

public class TestController : Controller
{
    private readonly IBitcoinNodeConnection _nodeConnection;
    
    public TestController(IBitcoinNodeConnection nodeConnection)
    {
        _nodeConnection = nodeConnection;
    }
    
    // GET
    [HttpGet("/connect")]
    public async Task<IActionResult> Connect([FromQuery] string nodeIp = "45.144.112.208", [FromQuery] ushort nodePort = 8333)
    {
        Console.WriteLine($"Received request to connect to {nodeIp}:{nodePort}");

        try
        {
            var res = await _nodeConnection.ConnectToNode(nodeIp, nodePort);
            return Ok(res);
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    [HttpGet("/disconnect")]
    public IActionResult Disconnect()
    {
        _nodeConnection.Disconnect();

        return Ok("Task stopped. Check the console for output.");
    }
    
    [HttpGet("/getAddresses")]
    public async Task<IActionResult> GetAddresses()
    {
        try
        {
            _nodeConnection.GetAddresses();
            return Ok();
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }
}