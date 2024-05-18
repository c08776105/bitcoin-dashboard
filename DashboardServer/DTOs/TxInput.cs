namespace DashboardServer.DTOs;

public class TxInput(byte[] txInHash, uint txInIndex, uint scriptLength, byte[] scriptSig, uint sequence)
{
    byte[] TxInHash {get; set;} = txInHash;
    uint TxInIndex {get; set;} = txInIndex;
    uint ScriptLength {get; set;} = scriptLength;
    byte[] ScriptSig {get; set;} = scriptSig;
    uint Sequence {get; set;} = sequence;
}