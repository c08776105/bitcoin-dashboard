namespace DashboardServer.DTOs;

public class TxOutput(ulong value, uint scriptLength, byte[] scriptPubKey)
{
    public ulong Value {get; set;} = value;
    public uint ScriptLength {get; set;} = scriptLength;
    public byte[] ScriptPubKey {get; set;} = scriptPubKey;
}